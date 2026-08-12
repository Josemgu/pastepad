# -*- coding: utf-8 -*-
"""Las filas de la lista.

Cada una es una tarjeta de verdad: se ilumina al pasar el raton, tiene
sus esquinas suaves y el menu de acciones sale con un solo boton.
"""

import flet as ft

from . import config as cfg
from . import estilo as st
from . import modelo


def _item(etiqueta, icono_nombre, al_pulsar):
    """Una opcion del menu.

    En esta version de Flet el texto va dentro de content, no en un
    parametro text.
    """
    return ft.PopupMenuItem(
        content=ft.Row(
            [ft.Icon(icono_nombre, size=16, color=st.C["medio"]),
             ft.Text(etiqueta, size=st.T_MENOR, color=st.C["texto"],
                     font_family=cfg.FUENTE_ALT)],
            spacing=10, tight=True),
        on_click=al_pulsar)


def _resumen(dato, es_hist):
    """Devuelve (titulo, detalle, es_enlace) segun el tipo de entrada."""
    if es_hist:
        if dato.get("tipo") == "imagen":
            return "Imagen copiada", "captura", False
        texto = dato.get("texto", "")
        if modelo.es_enlace(texto):
            return modelo.una_linea(texto, 80), modelo.dominio_de(texto), True
        return (modelo.una_linea(texto, 80) or "—",
                "%d caracteres" % len(texto), False)
    texto = modelo.texto_de(dato.get("runs", []))
    if modelo.es_enlace(texto):
        return dato["titulo"], modelo.dominio_de(texto), True
    return dato["titulo"], dato["categoria"], False


class Fila(ft.Container):
    """Una entrada de la lista."""

    def __init__(self, dato, tipo, activa, marcando, marcada, acciones):
        super().__init__()
        self.dato = dato
        self.tipo = tipo
        self.acciones = acciones
        es_hist = tipo == "h"
        titulo, detalle, enlace = _resumen(dato, es_hist)
        fijada = bool(dato.get("pin")) if es_hist else False

        izquierda = []
        if marcando:
            izquierda.append(ft.Icon(
                ft.Icons.CHECK_BOX if marcada
                else ft.Icons.CHECK_BOX_OUTLINE_BLANK,
                size=19,
                color=st.C["acento"] if marcada and not activa
                else (st.C["sobre"] if activa else st.C["tenue"])))
        elif enlace:
            izquierda.append(ft.Icon(
                ft.Icons.LINK, size=17,
                color=st.C["sobre"] if activa else st.C["acento"]))
        elif es_hist and dato.get("tipo") == "imagen":
            izquierda.append(ft.Icon(
                ft.Icons.IMAGE_OUTLINED, size=17,
                color=st.C["sobre"] if activa else st.C["tenue"]))

        centro = ft.Column(
            [st.texto(titulo, st.T_CUERPO,
                      st.C["sobre"] if activa else st.C["texto"],
                      ft.FontWeight.W_500 if activa else ft.FontWeight.W_400),
             st.texto(detalle, st.T_MINI,
                      ft.Colors.with_opacity(0.75, st.C["sobre"]) if activa
                      else st.C["tenue"])],
            spacing=2, tight=True,
            alignment=ft.MainAxisAlignment.CENTER, expand=True)

        derecha = []
        if fijada and not marcando:
            derecha.append(ft.Icon(
                ft.Icons.PUSH_PIN, size=15,
                color=st.C["sobre"] if activa else st.C["acento"]))
        if not marcando:
            derecha.append(self._menu(es_hist, enlace, fijada, activa))

        self.content = ft.Row(
            izquierda + [centro] + derecha,
            spacing=st.E3, vertical_alignment=ft.CrossAxisAlignment.CENTER)
        self.bgcolor = st.C["acento"] if activa else st.C["tarjeta"]
        self.border_radius = st.R_TARJETA
        self.padding = ft.Padding.only(left=16, right=6, top=4, bottom=4)
        self.height = st.ALTO_FILA
        self.on_click = self._pulsar
        self.on_hover = self._sobrevolar
        self.ink = not marcando
        self.animate = ft.Animation(130, ft.AnimationCurve.EASE_OUT)
        self.shadow = st.sombra(0.5) if activa else None
        self._activa = activa

    def _menu(self, es_hist, enlace, fijada, activa):
        """Un solo boton con todas las acciones detras."""
        def opcion(etiqueta, icono_nombre, accion):
            return _item(etiqueta, icono_nombre,
                         lambda e, a=accion: self.acciones(a, self.dato))

        opciones = []
        if enlace:
            opciones.append(opcion("Abrir en el navegador",
                                   ft.Icons.OPEN_IN_NEW, "abrir"))
            opciones.append(ft.PopupMenuItem())
        opciones += [
            opcion("Pegar", ft.Icons.CONTENT_PASTE, "pegar"),
            opcion("Pegar sin formato", ft.Icons.FORMAT_CLEAR, "pegar_plano"),
            opcion("Copiar", ft.Icons.COPY_ALL, "copiar"),
            ft.PopupMenuItem(),
        ]
        if es_hist:
            opciones.append(opcion(
                "Quitar de arriba" if fijada else "Fijar arriba",
                ft.Icons.PUSH_PIN_OUTLINED, "fijar"))
            if self.dato.get("tipo") == "texto":
                opciones.append(opcion("Editar y guardar...",
                                       ft.Icons.EDIT_OUTLINED, "editar"))
        else:
            opciones.append(opcion("Editar...", ft.Icons.EDIT_OUTLINED,
                                   "editar"))
        opciones += [ft.PopupMenuItem(),
                     opcion("Borrar", ft.Icons.DELETE_OUTLINE, "borrar")]

        return ft.PopupMenuButton(
            items=opciones, icon=ft.Icons.MORE_HORIZ, icon_size=18,
            icon_color=st.C["sobre"] if activa else st.C["tenue"],
            menu_position=ft.PopupMenuPosition.UNDER,
            bgcolor=st.C["elevado"],
            shape=ft.RoundedRectangleBorder(radius=12))

    def _pulsar(self, e):
        self.acciones("elegir", self.dato)

    def _sobrevolar(self, e):
        """Resalta la fila bajo el raton. Solo cambia el color: no se
        vuelve a construir nada."""
        if self._activa:
            return
        self.bgcolor = st.C["hover"] if e.data == "true" else st.C["tarjeta"]
        self.update()


def vacio(mensaje):
    return ft.Container(
        content=ft.Column(
            [ft.Icon(ft.Icons.CONTENT_PASTE_OFF, size=30,
                     color=st.C["tenue"]),
             st.texto(mensaje, st.T_MENOR, st.C["tenue"], lineas=2)],
            spacing=st.E3, horizontal_alignment=ft.CrossAxisAlignment.CENTER,
            alignment=ft.MainAxisAlignment.CENTER),
        alignment=ft.Alignment.CENTER, padding=40, expand=True)
