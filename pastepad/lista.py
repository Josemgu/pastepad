# -*- coding: utf-8 -*-
"""La lista de entradas, dibujada en un lienzo.

CustomTkinter se arrastra cuando hay decenas de widgets: cada fila
serian unos ocho que habria que destruir y recrear en cada tecla. Aqui
todo es un solo widget y las filas son rectangulos y texto, asi que
repintar cientos de elementos es instantaneo.
"""

import tkinter as tk
import tkinter.font as tkfont

from . import config as cfg
from . import modelo
from .tema import C, familia_iconos, familia_texto, simbolo


class Lista(tk.Frame):

    ALTO_FILA = 56
    ALTO_MINI = 42
    HUECO = 6
    MARGEN = 4

    def __init__(self, padre, al_elegir, al_pegar, al_accion):
        super().__init__(padre, bg=C["fondo"], highlightthickness=0)
        self.al_elegir = al_elegir
        self.al_pegar = al_pegar
        self.al_accion = al_accion

        self.items = []
        self.sel = 0
        self.encima = None
        self.desliz = 0
        self.zonas = []
        self.marcando = False
        self.marcados = set()
        self.aviso = "Sin resultados"
        self._espera = None

        self.cv = tk.Canvas(self, bg=C["fondo"], highlightthickness=0, bd=0)
        self.cv.pack(side="left", fill="both", expand=True)
        self.barra = tk.Canvas(self, bg=C["fondo"], highlightthickness=0,
                               bd=0, width=6)
        self.barra.pack(side="right", fill="y")

        self.cv.bind("<Configure>", self._al_cambiar_tamano)
        self.cv.bind("<MouseWheel>", self._rueda)
        self.cv.bind("<Button-1>", self._click)
        self.cv.bind("<Double-Button-1>", lambda e: "break")
        self.cv.bind("<Motion>", self._raton)
        self.cv.bind("<Leave>", lambda e: self._hover(None))

        familia = familia_texto()
        self.f_tit = tkfont.Font(family=familia, size=10)
        self.f_sub = tkfont.Font(family=familia, size=8)
        self.f_ico = tkfont.Font(family=familia_iconos() or familia, size=10)

    # ---------------------------------------------------------- datos

    def cargar(self, items, aviso=None):
        """items: lista de (dato, tipo) donde tipo es 'h' o 'g'."""
        self.items = items
        self.sel = 0
        self.desliz = 0
        if aviso:
            self.aviso = aviso
        self.pintar()

    def compactar(self, si):
        alto = self.ALTO_MINI if si else 52
        if alto != self.ALTO_FILA:
            self.ALTO_FILA = alto
            self.pintar()

    def modo_marcar(self, activo):
        self.marcando = activo
        self.marcados.clear()
        self.pintar()

    def marcar_todos(self):
        if len(self.marcados) == len(self.items):
            self.marcados.clear()
        else:
            self.marcados = {id(d) for d, _ in self.items}
        self.pintar()

    def elegidos(self):
        return [d for d, _ in self.items if id(d) in self.marcados]

    def _paso(self):
        return self.ALTO_FILA + self.HUECO

    def _alto_total(self):
        return len(self.items) * self._paso() + self.HUECO

    def _visible(self):
        return max(1, self.cv.winfo_height())

    # ---------------------------------------------------------- dibujo

    def _al_cambiar_tamano(self, evento):
        """Al redimensionar llegan decenas de avisos seguidos: esperamos
        a que la cosa se quede quieta antes de dibujar."""
        if self._espera:
            try:
                self.after_cancel(self._espera)
            except Exception:
                pass
        self._espera = self.after(50, self._pintar_ya)

    def _pintar_ya(self):
        self._espera = None
        self.pintar()

    def pintar(self):
        cv = self.cv
        cv.delete("all")
        self.zonas = []
        ancho = cv.winfo_width()
        if ancho <= 1:
            return

        if not self.items:
            cv.create_text(ancho // 2, 44, text=self.aviso, fill=C["tenue"],
                           font=self.f_sub, width=ancho - 48, justify="center")
            self._pintar_barra()
            return

        alto_visible = self._visible()
        paso = self._paso()
        desde = max(0, int(self.desliz // paso) - 1)
        hasta = min(len(self.items), desde + int(alto_visible // paso) + 3)

        for i in range(desde, hasta):
            dato, tipo = self.items[i]
            y = self.HUECO + i * paso - self.desliz
            self._fila(i, dato, tipo, y, ancho)
        self._pintar_barra()

    def _fila(self, i, dato, tipo, y, ancho):
        """Cada pieza lleva etiquetas para poder recolorearla luego sin
        volver a dibujarla."""
        cv = self.cv
        activa = i == self.sel
        hover = self.encima == i
        fondo = (C["acento"] if activa else
                 C["hover"] if hover else C["tarjeta"])
        col_titulo = C["sobre"] if activa else C["texto"]
        col_sub = C["sobre"] if activa else C["tenue"]
        et_fondo, et_tit, et_sub = "f%d" % i, "t%d" % i, "s%d" % i

        self._redondo(self.MARGEN, y, ancho - self.MARGEN,
                      y + self.ALTO_FILA, cfg.R_TARJETA, fondo, et_fondo)
        # Barrita de color a la izquierda de la fila elegida: marca
        # donde estas sin depender solo del fondo.
        if activa:
            self._redondo(self.MARGEN + 5, y + 14, self.MARGEN + 8,
                          y + self.ALTO_FILA - 14, 2, C["sobre"], et_fondo)

        desde_x = 22
        if self.marcando:
            marcado = id(dato) in self.marcados
            cv.create_text(22, y + self.ALTO_FILA // 2,
                           text=simbolo("marcado" if marcado
                                        else "sin_marcar"),
                           fill=C["acento"] if marcado and not activa
                           else col_sub,
                           font=self.f_ico, tags=(et_sub,))
            desde_x = 48

        reservado = 24 if self.marcando else 82
        tope = max(12, int((ancho - desde_x - reservado) / 5.9))

        es_hist = tipo == "h"
        enlace = False
        if es_hist:
            if dato["tipo"] == "imagen":
                titulo, sub = "Imagen copiada", "captura"
            else:
                texto = dato.get("texto", "")
                enlace = modelo.es_enlace(texto)
                titulo = modelo.una_linea(texto, tope) or "—"
                sub = (modelo.dominio_de(texto) if enlace
                       else "%d caracteres" % len(texto))
            fijado = bool(dato.get("pin"))
        else:
            texto = modelo.texto_de(dato["runs"])
            enlace = modelo.es_enlace(texto)
            titulo = modelo.una_linea(dato["titulo"], tope)
            sub = (modelo.dominio_de(texto) if enlace else dato["categoria"])
            fijado = False

        if enlace:
            # Marca de enlace a la izquierda, como en las listas del
            # navegador: se reconoce de un vistazo.
            cv.create_text(desde_x, y + self.ALTO_FILA // 2,
                           text=simbolo("enlace"),
                           fill=C["sobre"] if activa else C["acento"],
                           font=self.f_ico, tags=(et_sub,))
            desde_x += 22

        if self.ALTO_FILA <= self.ALTO_MINI:
            cv.create_text(desde_x, y + self.ALTO_FILA // 2, text=titulo,
                           anchor="w", fill=col_titulo, font=self.f_tit,
                           tags=(et_tit,))
        else:
            cv.create_text(desde_x, y + 19, text=titulo, anchor="w",
                           fill=col_titulo, font=self.f_tit, tags=(et_tit,))
            cv.create_text(desde_x, y + 37, text=sub, anchor="w",
                           fill=col_sub, font=self.f_sub, tags=(et_sub,))

        if self.marcando:
            return

        x = ancho - 26
        # El alfiler se queda visible si esta fijado: hay que saberlo
        # sin pasar el raton por encima.
        if fijado:
            cv.create_text(x, y + self.ALTO_FILA // 2, text=simbolo("unpin"),
                           fill=C["sobre"] if activa else C["acento"],
                           font=self.f_ico, tags=("p%d" % i,))
            self.zonas.append((x - 12, y, x + 12, y + self.ALTO_FILA,
                               "pin", i))
            x -= 26

        # Un solo boton para todo lo demas, y solo cuando hace falta.
        if hover or activa:
            cv.create_text(x, y + self.ALTO_FILA // 2,
                           text=simbolo("mas_opciones"), fill=col_sub,
                           font=self.f_ico, tags=(et_sub,))
            self.zonas.append((x - 13, y, x + 13, y + self.ALTO_FILA,
                               "menu", i))

    def _redondo(self, x1, y1, x2, y2, r, color, etiqueta=None):
        cv = self.cv
        t = (etiqueta,) if etiqueta else ()
        cv.create_rectangle(x1 + r, y1, x2 - r, y2, fill=color,
                            outline=color, tags=t)
        cv.create_rectangle(x1, y1 + r, x2, y2 - r, fill=color,
                            outline=color, tags=t)
        for cx, cy, ini in ((x1 + r, y1 + r, 90), (x2 - r, y1 + r, 0),
                            (x1 + r, y2 - r, 180), (x2 - r, y2 - r, 270)):
            cv.create_arc(cx - r, cy - r, cx + r, cy + r, start=ini,
                          extent=90, fill=color, outline=color, tags=t)

    def _recolorear(self, i):
        """Cambia los colores de una fila sin redibujarla.

        Repintar el lienzo entero en cada movimiento del raton llegaba a
        comerse casi un nucleo con la ventana grande.
        """
        if i is None or not (0 <= i < len(self.items)):
            return
        activa = i == self.sel
        hover = self.encima == i
        fondo = (C["acento"] if activa else
                 C["hover"] if hover else C["tarjeta"])
        try:
            self.cv.itemconfig("f%d" % i, fill=fondo, outline=fondo)
            self.cv.itemconfig("t%d" % i,
                               fill=C["sobre"] if activa else C["texto"])
            self.cv.itemconfig("s%d" % i,
                               fill=C["sobre"] if activa else C["tenue"])
        except Exception:
            pass

    def _pintar_barra(self):
        b = self.barra
        b.delete("all")
        total, visible = self._alto_total(), self._visible()
        if total <= visible:
            return
        largo = max(28, int(visible * visible / total))
        pos = int((visible - largo) * self.desliz / max(1, total - visible))
        b.create_rectangle(2, pos, 5, pos + largo, fill=C["borde_claro"],
                           outline=C["borde_claro"])

    # ---------------------------------------------------------- raton

    def _indice_en(self, y):
        i = int((y + self.desliz - self.HUECO) // self._paso())
        return i if 0 <= i < len(self.items) else None

    def _click(self, e):
        i = self._indice_en(e.y)
        if self.marcando:
            if i is not None:
                clave = id(self.items[i][0])
                self.marcados.symmetric_difference_update({clave})
                self.pintar()
                self.al_accion("conteo", None)
            return
        for x1, y1, x2, y2, accion, j in self.zonas:
            if x1 <= e.x <= x2 and y1 <= e.y <= y2:
                if accion == "menu":
                    self.sel = j
                    self.pintar()
                    self.al_accion("menu", (self.items[j][0],
                                            e.x_root, e.y_root))
                else:
                    self.al_accion(accion, self.items[j][0])
                return
        if i is not None:
            anterior, self.sel = self.sel, i
            self._recolorear(anterior)
            self._recolorear(i)
            self.al_elegir(i)
            self.al_pegar()

    def _raton(self, e):
        self._hover(self._indice_en(e.y))

    def _hover(self, i):
        if i == self.encima:
            return
        anterior, self.encima = self.encima, i
        # Los iconos aparecen y desaparecen, asi que hay que redibujar.
        self.pintar() if not self.marcando else self._recolorear(i)

    def _rueda(self, e):
        self.desplazar(-1 if e.delta > 0 else 1)
        return "break"

    def desplazar(self, pasos):
        tope = max(0, self._alto_total() - self._visible())
        self.desliz = max(0, min(tope, self.desliz + pasos * 54))
        self.pintar()

    def elegir(self, i):
        if not self.items:
            return
        anterior = self.sel
        self.sel = max(0, min(len(self.items) - 1, i))
        y = self.HUECO + self.sel * self._paso()
        desliz = self.desliz
        if y < desliz:
            desliz = y
        elif y + self.ALTO_FILA > desliz + self._visible():
            desliz = y + self.ALTO_FILA - self._visible()
        if desliz != self.desliz:
            self.desliz = desliz
            self.pintar()
        else:
            self._recolorear(anterior)
            self._recolorear(self.sel)

    def repintar_colores(self):
        for w in (self, self.cv, self.barra):
            w.configure(bg=C["fondo"])
        self.pintar()
