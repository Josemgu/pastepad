# -*- coding: utf-8 -*-
"""Las ventanas que se abren encima del panel."""

import tkinter as tk

import customtkinter as ctk
import pyperclip

from . import config as cfg
from . import modelo
from .tema import (C, boton, caja_texto, entrada, etiqueta, fuente,
                   familia_iconos, familia_texto, simbolo)


class Dialogo(ctk.CTkToplevel):
    """Base comun: estilo, icono y atajos de portapapeles.

    Los atajos hay que ponerlos a mano porque la libreria que captura el
    atajo global se queda con las combinaciones que empiezan por Ctrl y
    no las deja llegar a las cajas de texto.
    """

    def __init__(self, padre, titulo, ancho, alto):
        super().__init__(padre, fg_color=C["fondo"])
        self.resultado = None
        self.title(titulo)
        self.geometry("%dx%d" % (ancho, alto))
        self.minsize(ancho, alto)
        self.attributes("-topmost", True)
        icono = cfg.ruta_icono()
        if icono:
            try:
                self.iconbitmap(icono)
            except Exception:
                pass
        self.after(130, self._tomar_foco)
        self.bind("<Escape>", lambda e: self.destroy())

    def _tomar_foco(self):
        try:
            self.grab_set()
        except Exception:
            pass

    def atajos(self, widget):
        for tecla, accion in (("v", self._pegar), ("c", self._copiar),
                              ("x", self._cortar), ("a", self._todo)):
            widget.bind("<Control-%s>" % tecla, accion)
            widget.bind("<Control-%s>" % tecla.upper(), accion)

    def _pegar(self, e):
        try:
            texto = pyperclip.paste()
        except Exception:
            return "break"
        w = e.widget
        try:
            try:
                w.delete("sel.first", "sel.last")
            except Exception:
                pass
            w.insert("insert", texto if isinstance(w, tk.Text)
                     else texto.replace("\n", " "))
        except Exception:
            pass
        return "break"

    def _copiar(self, e):
        try:
            w = e.widget
            pyperclip.copy(w.get("sel.first", "sel.last")
                           if isinstance(w, tk.Text) else w.selection_get())
        except Exception:
            pass
        return "break"

    def _cortar(self, e):
        self._copiar(e)
        try:
            e.widget.delete("sel.first", "sel.last")
        except Exception:
            pass
        return "break"

    def _todo(self, e):
        w = e.widget
        try:
            if isinstance(w, tk.Text):
                w.tag_add("sel", "1.0", "end-1c")
            else:
                w.select_range(0, "end")
        except Exception:
            pass
        return "break"

    def pie(self):
        """Barra de botones anclada abajo.

        Se coloca antes que el contenido para que el area de texto nunca
        la empuje fuera de la ventana.
        """
        marco = ctk.CTkFrame(self, fg_color="transparent", height=56)
        marco.pack(side="bottom", fill="x", padx=cfg.E4, pady=(cfg.E2, cfg.E4))
        marco.pack_propagate(False)
        return marco


class DlgTexto(Dialogo):
    """Crear o editar un texto guardado. Sin campos obligatorios."""

    def __init__(self, padre, carpetas, snippet=None, texto=""):
        super().__init__(padre, "Texto", 580, 460)

        pie = self.pie()
        boton(pie, "Guardar", self.guardar, 108, 36, "acento",
              cfg.T_CUERPO).pack(side="right")
        boton(pie, "Cancelar", self.destroy, 96, 36).pack(side="right",
                                                          padx=cfg.E2)
        etiqueta(pie, "Ctrl+Enter guarda", cfg.T_MINI,
                 C["tenue"]).pack(side="left", pady=cfg.E3)

        cab = ctk.CTkFrame(self, fg_color="transparent")
        cab.pack(fill="x", padx=cfg.E4, pady=(cfg.E4, cfg.E2))
        etiqueta(cab, "Guardar en").pack(side="left")
        opciones = list(carpetas) or ["Mis textos"]
        self.cb = ctk.CTkComboBox(
            cab, values=opciones, width=190, height=34,
            corner_radius=cfg.R_CONTROL, fg_color=C["tarjeta"],
            border_color=C["borde"], button_color=C["tarjeta"],
            button_hover_color=C["hover"], text_color=C["texto"],
            font=fuente())
        self.cb.pack(side="left", padx=cfg.E2)
        self.cb.set(snippet["categoria"] if snippet else opciones[0])
        etiqueta(cab, "[[campo]] se pregunta al pegar", cfg.T_MINI,
                 C["tenue"]).pack(side="left", padx=cfg.E2)

        marco, self.txt = caja_texto(self)
        marco.pack(fill="both", expand=True, padx=cfg.E4)
        self.atajos(self.txt)
        if snippet:
            for f in snippet["runs"]:
                self.txt.insert("end", f["t"])
        elif texto:
            self.txt.insert("1.0", texto)

        self.bind("<Control-Return>", lambda e: self.guardar())
        self.after(220, self.txt.focus_set)

    def guardar(self):
        texto = self.txt.get("1.0", "end-1c")
        if not texto.strip():
            self.destroy()
            return
        primera = texto.strip().splitlines()[0]
        self.resultado = {
            "titulo": modelo.una_linea(primera, 48),
            "categoria": self.cb.get().strip() or "Mis textos",
            "runs": [modelo.fragmento(texto)]}
        self.destroy()


class DlgLinea(Dialogo):
    """Pide una sola linea: el nombre de una carpeta."""

    def __init__(self, padre, titulo, etiqueta_texto):
        super().__init__(padre, titulo, 360, 170)
        pie = self.pie()
        boton(pie, "Crear", self.ok, 92, 34, "acento").pack(side="right")
        boton(pie, "Cancelar", self.destroy, 92, 34).pack(side="right",
                                                          padx=cfg.E2)
        etiqueta(self, etiqueta_texto).pack(anchor="w", padx=cfg.E4,
                                            pady=(cfg.E5, cfg.E2))
        self.e = entrada(self)
        self.e.pack(fill="x", padx=cfg.E4)
        self.atajos(self.e)
        self.bind("<Return>", lambda e: self.ok())
        self.after(220, self.e.focus_set)

    def ok(self):
        self.resultado = self.e.get().strip()
        self.destroy()


class DlgCampos(Dialogo):
    """Rellena los [[campos]] de una plantilla antes de pegar."""

    def __init__(self, padre, campos):
        super().__init__(padre, "Completar", 380, 110 + 62 * len(campos))
        pie = self.pie()
        boton(pie, "Pegar", self.ok, 92, 34, "acento").pack(side="right")
        boton(pie, "Cancelar", self.destroy, 92, 34).pack(side="right",
                                                          padx=cfg.E2)
        etiqueta(self, "Completar antes de pegar", cfg.T_CUERPO,
                 C["texto"]).pack(anchor="w", padx=cfg.E4,
                                  pady=(cfg.E4, cfg.E2))
        self.entradas = {}
        for campo in campos:
            etiqueta(self, campo, cfg.T_MINI).pack(anchor="w", padx=cfg.E4)
            e = entrada(self)
            e.pack(fill="x", padx=cfg.E4, pady=(2, cfg.E2))
            self.atajos(e)
            self.entradas[campo] = e
        self.bind("<Return>", lambda e: self.ok())
        primeros = list(self.entradas.values())
        if primeros:
            self.after(220, primeros[0].focus_set)

    def ok(self):
        self.resultado = {k: e.get() for k, e in self.entradas.items()}
        self.destroy()


class DlgLista(Dialogo):
    """Pega varias cosas: una nota por linea, o todo junto."""

    def __init__(self, padre, carpeta):
        super().__init__(padre, "Agregar a " + carpeta, 560, 500)

        pie = self.pie()
        boton(pie, "Agregar", self.ok, 104, 36, "acento",
              cfg.T_CUERPO).pack(side="right")
        boton(pie, "Cancelar", self.destroy, 96, 36).pack(side="right",
                                                          padx=cfg.E2)
        self.cuenta = etiqueta(pie, "", cfg.T_MENOR)
        self.cuenta.pack(side="left", pady=cfg.E3)

        opciones = ctk.CTkFrame(self, fg_color=C["elevado"],
                                corner_radius=cfg.R_TARJETA)
        opciones.pack(side="bottom", fill="x", padx=cfg.E4, pady=cfg.E2)
        self.modo = ctk.StringVar(value="separado")
        for valor, texto in (("separado", "Una nota por cada linea"),
                             ("junto", "Todo junto en una sola nota")):
            ctk.CTkRadioButton(
                opciones, text=texto, variable=self.modo, value=valor,
                radiobutton_width=17, radiobutton_height=17,
                fg_color=C["acento"], hover_color=C["acento_h"],
                text_color=C["texto"], font=fuente(cfg.T_MENOR),
                command=self._contar).pack(anchor="w", padx=cfg.E3,
                                           pady=(cfg.E3, cfg.E1))
        self.limpiar = ctk.BooleanVar(value=True)
        ctk.CTkCheckBox(
            opciones, text="Quitar numeracion y vinetas",
            variable=self.limpiar, checkbox_width=17, checkbox_height=17,
            corner_radius=5, fg_color=C["acento"], hover_color=C["acento_h"],
            text_color=C["medio"], font=fuente(cfg.T_MENOR),
            command=self._contar).pack(anchor="w", padx=cfg.E3,
                                       pady=(cfg.E1, cfg.E3))

        etiqueta(self, "Pega aqui tu lista", cfg.T_CUERPO,
                 C["texto"]).pack(anchor="w", padx=cfg.E4,
                                  pady=(cfg.E4, cfg.E2))
        marco, self.txt = caja_texto(self)
        marco.pack(fill="both", expand=True, padx=cfg.E4)
        self.atajos(self.txt)
        self.txt.bind("<KeyRelease>", lambda e: self._contar())
        self.after(220, self.txt.focus_set)
        self._contar()

    @staticmethod
    def _sin_vineta(t):
        for marca in ("- ", "* ", "\u2022 ", "\u2013 "):
            if t.startswith(marca):
                return t[len(marca):].strip()
        i = 0
        while i < len(t) and t[i].isdigit():
            i += 1
        if i and i < len(t) and t[i] in ".)-":
            return t[i + 1:].strip()
        return t

    def lineas(self):
        salida = []
        for linea in self.txt.get("1.0", "end-1c").splitlines():
            t = linea.strip()
            if not t:
                continue
            if self.limpiar.get():
                t = self._sin_vineta(t)
            if t:
                salida.append(t)
        return salida

    def _contar(self):
        n = len(self.lineas())
        if self.modo.get() == "junto":
            self.cuenta.configure(text="1 nota con %d lineas" % n)
        else:
            self.cuenta.configure(
                text="%d nota%s" % (n, "" if n == 1 else "s"))

    def ok(self):
        lineas = self.lineas()
        if lineas:
            self.resultado = (["\n".join(lineas)]
                              if self.modo.get() == "junto" else lineas)
        self.destroy()


class DlgApariencia(Dialogo):
    """Color, tamano del panel y atajo, todo en un sitio."""

    def __init__(self, padre, acento, tamano, atajo, carpetas):
        super().__init__(padre, "Apariencia", 380, 470)
        self.acento, self.tamano = acento, tamano
        self.carpetas = carpetas

        pie = self.pie()
        boton(pie, "Aplicar", self.ok, 96, 34, "acento").pack(side="right")
        boton(pie, "Cancelar", self.destroy, 92, 34).pack(side="right",
                                                          padx=cfg.E2)

        etiqueta(self, "Color").pack(anchor="w", padx=cfg.E4,
                                     pady=(cfg.E4, cfg.E2))
        fila = ctk.CTkFrame(self, fg_color="transparent")
        fila.pack(padx=cfg.E4, anchor="w")
        self.bolas = {}
        for nombre, (color, _) in cfg.ACENTOS.items():
            b = ctk.CTkButton(
                fila, text=simbolo("check") if nombre == acento else "",
                width=34, height=34, corner_radius=17, fg_color=color,
                hover_color=color, text_color="#FFFFFF",
                font=ctk.CTkFont(family=familia_iconos() or familia_texto(),
                                 size=13),
                command=lambda n=nombre: self._color(n))
            b.pack(side="left", padx=cfg.E1)
            self.bolas[nombre] = b

        etiqueta(self, "Tamano del panel").pack(anchor="w", padx=cfg.E4,
                                                pady=(cfg.E5, cfg.E2))
        fila2 = ctk.CTkFrame(self, fg_color="transparent")
        fila2.pack(padx=cfg.E4, anchor="w", fill="x")
        self.medidas = {}
        for nombre in cfg.TAMANOS:
            b = ctk.CTkButton(
                fila2, text=nombre.capitalize(), height=32, width=10,
                corner_radius=cfg.R_CONTROL,
                fg_color=C["acento"] if nombre == tamano else C["tarjeta"],
                hover_color=C["acento_h"] if nombre == tamano else C["hover"],
                text_color=C["sobre"] if nombre == tamano else C["medio"],
                font=fuente(cfg.T_MINI),
                command=lambda n=nombre: self._medida(n))
            b.pack(side="left", expand=True, fill="x", padx=2)
            self.medidas[nombre] = b

        etiqueta(self, "Carpetas").pack(anchor="w", padx=cfg.E4,
                                        pady=(cfg.E5, cfg.E2))
        fila3 = ctk.CTkFrame(self, fg_color="transparent")
        fila3.pack(padx=cfg.E4, anchor="w", fill="x")
        self.estilos = {}
        for valor, texto in ((cfg.CARPETAS_MENU, "Lista desplegable"),
                             (cfg.CARPETAS_FICHAS, "Fichas en fila")):
            b = ctk.CTkButton(
                fila3, text=texto, height=32, width=10,
                corner_radius=cfg.R_CONTROL,
                fg_color=C["acento"] if valor == carpetas else C["tarjeta"],
                hover_color=C["acento_h"] if valor == carpetas
                else C["hover"],
                text_color=C["sobre"] if valor == carpetas else C["medio"],
                font=fuente(cfg.T_MINI),
                command=lambda v=valor: self._estilo(v))
            b.pack(side="left", expand=True, fill="x", padx=2)
            self.estilos[valor] = b

        etiqueta(self, "Atajo para abrir").pack(anchor="w", padx=cfg.E4,
                                                pady=(cfg.E5, cfg.E2))
        self.cb = ctk.CTkComboBox(
            self, values=list(cfg.ATAJOS.values()), width=320, height=34,
            corner_radius=cfg.R_CONTROL, fg_color=C["tarjeta"],
            border_color=C["borde"], button_color=C["tarjeta"],
            button_hover_color=C["hover"], text_color=C["texto"],
            state="readonly", font=fuente())
        self.cb.set(cfg.ATAJOS.get(atajo, cfg.ATAJOS[cfg.ATAJO_DEF]))
        self.cb.pack(padx=cfg.E4, anchor="w")
        self.atajo = atajo

    def _color(self, nombre):
        self.acento = nombre
        for n, b in self.bolas.items():
            b.configure(text=simbolo("check") if n == nombre else "")

    def _estilo(self, valor):
        self.carpetas = valor
        for v, b in self.estilos.items():
            activo = v == valor
            b.configure(fg_color=C["acento"] if activo else C["tarjeta"],
                        hover_color=C["acento_h"] if activo else C["hover"],
                        text_color=C["sobre"] if activo else C["medio"])

    def _medida(self, nombre):
        self.tamano = nombre
        for n, b in self.medidas.items():
            activo = n == nombre
            b.configure(fg_color=C["acento"] if activo else C["tarjeta"],
                        hover_color=C["acento_h"] if activo else C["hover"],
                        text_color=C["sobre"] if activo else C["medio"])

    def ok(self):
        elegido = self.cb.get()
        combinacion = self.atajo
        for clave, texto in cfg.ATAJOS.items():
            if texto == elegido:
                combinacion = clave
                break
        self.resultado = (self.acento, self.tamano, combinacion,
                          self.carpetas)
        self.destroy()
