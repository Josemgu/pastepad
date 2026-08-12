# -*- coding: utf-8 -*-
"""Busqueda con puntuacion. Independiente de la interfaz."""

from . import modelo

SIN_TILDES = str.maketrans("áéíóúüñÁÉÍÓÚÜÑ", "aeiouunAEIOUUN")
TOPE_TEXTO = 4000


def normalizar(texto):
    """Minusculas y sin tildes, para que 'informacion' encuentre
    'información' y al reves."""
    return texto[:TOPE_TEXTO].lower().translate(SIN_TILDES)


def puntuar(palabras, titulo, cuerpo):
    """Cuanto se parece, o None si no coincide.

    Cada palabra tiene que estar en algun lado. Pesa mas si aparece en
    el titulo, si empieza una palabra y si esta cerca del principio.
    """
    total = 0
    for palabra in palabras:
        pos_t = titulo.find(palabra)
        pos_c = cuerpo.find(palabra)
        if pos_t < 0 and pos_c < 0:
            return None
        if pos_t >= 0:
            total += 100
            if pos_t == 0 or titulo[pos_t - 1] in " -_.:,/()":
                total += 60
            total += max(0, 25 - pos_t // 2)
        else:
            total += 30
            if pos_c == 0 or cuerpo[pos_c - 1] in " -_.:,/()\n":
                total += 20
            total += max(0, 15 - pos_c // 40)
    if len(palabras) > 1 and " ".join(palabras) in titulo + " " + cuerpo:
        total += 80
    return total


class Indice:
    """Guarda el texto ya normalizado de cada entrada.

    Normalizar 80 textos largos cuesta unos 25 ms; sin esta cache eso
    pasaba cada vez que se copiaba algo.
    """

    def __init__(self, almacen):
        self.almacen = almacen
        self._cache = {}
        self._lista = None

    def invalidar(self):
        self._lista = None

    def _normalizado(self, dato, tipo):
        clave = id(dato)
        marca = dato.get("texto") if tipo == "h" else dato.get("titulo")
        guardado = self._cache.get(clave)
        if guardado is not None and guardado[0] is marca:
            return guardado[1], guardado[2]

        if tipo == "g":
            titulo = normalizar(dato["titulo"])
            cuerpo = normalizar(modelo.texto_de(dato["runs"]))
        elif dato["tipo"] == "imagen":
            titulo, cuerpo = "imagen captura", ""
        else:
            t = dato.get("texto", "")
            titulo = normalizar(modelo.una_linea(t, 80))
            cuerpo = normalizar(t)

        self._cache[clave] = (marca, titulo, cuerpo)
        return titulo, cuerpo

    def entradas(self):
        if self._lista is not None:
            return self._lista
        lista = []
        for g in self.almacen.snippets:
            t, c = self._normalizado(g, "g")
            lista.append((g, "g", t, c))
        for h in self.almacen.hist_ordenado():
            t, c = self._normalizado(h, "h")
            lista.append((h, "h", t, c))

        vivos = {id(x) for x, _, _, _ in lista}
        for clave in [k for k in self._cache if k not in vivos]:
            del self._cache[clave]

        self._lista = lista
        return lista

    def buscar(self, consulta):
        """Devuelve [(dato, tipo)] ordenado por parecido."""
        palabras = normalizar(consulta).split()
        if not palabras:
            return []
        puntuados = []
        for dato, tipo, titulo, cuerpo in self.entradas():
            p = puntuar(palabras, titulo, cuerpo)
            if p is None:
                continue
            if tipo == "g":
                p += 40   # lo guardado a proposito pesa mas que lo copiado
            puntuados.append((p, dato, tipo))
        puntuados.sort(key=lambda x: -x[0])
        return [(d, t) for _, d, t in puntuados]
