# -*- coding: utf-8 -*-
"""Los datos y sus reglas. Sin tkinter: todo lo de aqui se puede probar
sin abrir una ventana."""

import json
import os
import time

from . import config as cfg


# ------------------------------------------------------------ archivos

def _leer(ruta, defecto):
    try:
        with open(ruta, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception:
        return defecto


def _escribir(ruta, datos):
    """Escribe primero en un archivo aparte y luego lo mueve.

    Si el programa se cierra a mitad de la escritura, el archivo bueno
    sigue intacto en vez de quedar cortado.
    """
    temporal = ruta + ".tmp"
    try:
        with open(temporal, "w", encoding="utf-8") as f:
            json.dump(datos, f, ensure_ascii=False, indent=1)
        os.replace(temporal, ruta)
        return True
    except Exception:
        try:
            os.remove(temporal)
        except Exception:
            pass
        return False


def fragmento(texto, fuente=None, tam=None, negrita=0, cursiva=0,
              subrayado=0, color=None):
    """Un trozo de texto con su formato. Un snippet es una lista de estos."""
    return {"t": texto,
            "f": fuente or cfg.FUENTE_DEF,
            "s": tam or cfg.TAM_DEF,
            "b": negrita, "i": cursiva, "u": subrayado,
            "c": color or cfg.COLOR_DEF}


def texto_de(fragmentos):
    return "".join(f["t"] for f in fragmentos)


def una_linea(texto, tope=52):
    """Resumen de una linea. Corta antes de separar en palabras: con
    textos de miles de lineas, hacerlo al reves cuesta casi un segundo."""
    crudo = texto[:tope * 4]
    limpio = " ".join(crudo.split())
    if len(texto) > len(crudo) or len(limpio) > tope:
        return limpio[:tope] + "..."
    return limpio


# ------------------------------------------------------------ enlaces

def es_enlace(texto):
    """True si el texto es una direccion web y nada mas.

    Solo si el texto entero es el enlace: un parrafo que menciona una
    url de pasada no cuenta, porque abrirlo no seria lo que el usuario
    espera al hacer clic.
    """
    if not texto:
        return False
    t = texto.strip()
    if " " in t or "\n" in t or len(t) > 2000:
        return False
    return t.lower().startswith(("http://", "https://", "www."))


def url_de(texto):
    """La direccion lista para abrir en el navegador."""
    t = texto.strip()
    return "https://" + t if t.lower().startswith("www.") else t


def dominio_de(texto):
    """El dominio suelto, para mostrarlo debajo del titulo."""
    t = url_de(texto)
    for prefijo in ("https://", "http://"):
        if t.startswith(prefijo):
            t = t[len(prefijo):]
            break
    if t.startswith("www."):
        t = t[4:]
    return t.split("/")[0][:60]


# ------------------------------------------------------------ plantillas

def campos_de(texto):
    """Los [[campos]] de una plantilla, en orden y sin repetir."""
    campos, resto = [], texto
    while "[[" in resto and "]]" in resto:
        i = resto.index("[[")
        j = resto.index("]]", i)
        nombre = resto[i + 2:j].strip()
        if nombre and nombre not in campos:
            campos.append(nombre)
        resto = resto[j + 2:]
    return campos


def rellenar(fragmentos, valores):
    salida = []
    for f in fragmentos:
        t = f["t"]
        for clave, valor in valores.items():
            t = t.replace("[[%s]]" % clave, valor)
        copia = dict(f)
        copia["t"] = t
        salida.append(copia)
    return salida


# ------------------------------------------------------------ almacen

class Almacen:
    """Todo el estado de la aplicacion en un solo sitio.

    Antes cada parte de la interfaz leia y escribia los archivos por su
    cuenta; ahora pasan por aqui, que es lo unico que toca el disco.
    """

    def __init__(self):
        self.prefs = _leer(cfg.RUTA_PREFS, {})
        self.hist = _leer(cfg.RUTA_HIST, [])
        datos = _leer(cfg.RUTA_DATOS, {})
        self.carpetas = datos.get("categorias", [])
        self.snippets = datos.get("snippets", [])
        for s in self.snippets:
            if "runs" not in s:
                s["runs"] = [fragmento(s.get("texto", ""))]

    # ---- preferencias

    def pref(self, clave, defecto=None):
        return self.prefs.get(clave, defecto)

    def poner_pref(self, clave, valor):
        self.prefs[clave] = valor
        _escribir(cfg.RUTA_PREFS, self.prefs)

    # ---- guardados

    def guardar_datos(self):
        _escribir(cfg.RUTA_DATOS, {"categorias": self.carpetas,
                                   "snippets": self.snippets})

    def guardar_hist(self):
        _escribir(cfg.RUTA_HIST, self.hist)

    # ---- carpetas

    def crear_carpeta(self, nombre):
        if nombre and nombre not in self.carpetas:
            self.carpetas.append(nombre)
            self.guardar_datos()
            return True
        return False

    def renombrar_carpeta(self, viejo, nuevo):
        if not nuevo or nuevo in self.carpetas or viejo not in self.carpetas:
            return False
        self.carpetas[self.carpetas.index(viejo)] = nuevo
        for s in self.snippets:
            if s["categoria"] == viejo:
                s["categoria"] = nuevo
        self.guardar_datos()
        return True

    def contenido_de(self, carpeta):
        return [s for s in self.snippets if s["categoria"] == carpeta]

    def borrar_carpeta(self, carpeta):
        """Se lleva la carpeta y todo lo que tenga dentro."""
        dentro = self.contenido_de(carpeta)
        for s in dentro:
            self.snippets.remove(s)
        if carpeta in self.carpetas:
            self.carpetas.remove(carpeta)
        self.guardar_datos()
        return len(dentro)

    # ---- snippets

    def anadir_snippet(self, snippet):
        self.crear_carpeta(snippet["categoria"])
        self.snippets.append(snippet)
        self.guardar_datos()

    def reemplazar_snippet(self, viejo, nuevo):
        try:
            self.snippets[self.snippets.index(viejo)] = nuevo
        except ValueError:
            return False
        self.crear_carpeta(nuevo["categoria"])
        self.guardar_datos()
        return True

    # ---- historial

    def anotar(self, entrada):
        """Mete algo copiado al principio, debajo de los fijados."""
        for x in self.hist[:4]:
            if entrada["tipo"] == "texto" and \
                    x.get("texto") == entrada.get("texto"):
                return False
        fijados = sum(1 for x in self.hist if x.get("pin"))
        self.hist.insert(fijados, entrada)
        self._recortar()
        self.guardar_hist()
        return True

    def _recortar(self):
        """Deja solo las ultimas MAX_HIST sueltas. Las fijadas no cuentan."""
        libres = [x for x in self.hist if not x.get("pin")]
        for viejo in libres[cfg.MAX_HIST:]:
            self._borrar_imagen(viejo)
            self.hist.remove(viejo)

    @staticmethod
    def _borrar_imagen(entrada):
        if entrada.get("tipo") == "imagen":
            try:
                os.remove(entrada.get("ruta", ""))
            except Exception:
                pass

    def fijar(self, entrada):
        entrada["pin"] = not entrada.get("pin")
        self.guardar_hist()

    def borrar(self, elemento):
        """Sirve para los dos tipos: el historial lleva 'tipo', los
        guardados no."""
        if elemento.get("tipo"):
            self._borrar_imagen(elemento)
            try:
                self.hist.remove(elemento)
            except ValueError:
                return False
            self.guardar_hist()
        else:
            try:
                self.snippets.remove(elemento)
            except ValueError:
                return False
            self.guardar_datos()
        return True

    def borrar_varios(self, elementos):
        return sum(1 for e in elementos if self.borrar(e))

    def vaciar_historial(self):
        """Los fijados sobreviven: para eso estan."""
        for x in self.hist:
            if not x.get("pin"):
                self._borrar_imagen(x)
        self.hist = [x for x in self.hist if x.get("pin")]
        self.guardar_hist()

    def hist_ordenado(self):
        return ([x for x in self.hist if x.get("pin")] +
                [x for x in self.hist if not x.get("pin")])

    def guardar_imagen(self, datos_dib, a_bmp):
        try:
            os.makedirs(cfg.DIR_IMG, exist_ok=True)
            ruta = os.path.join(cfg.DIR_IMG,
                                "img_%d.bmp" % int(time.time() * 1000))
            with open(ruta, "wb") as f:
                f.write(a_bmp(datos_dib))
            return self.anotar({"tipo": "imagen", "ruta": ruta})
        except Exception:
            return False
