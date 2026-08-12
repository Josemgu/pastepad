# -*- coding: utf-8 -*-
"""Las filas de la lista.

Cada una es una tarjeta de verdad: se ilumina al pasar el raton, tiene
sus esquinas suaves y el menu de acciones sale con un solo boton.
"""

import flet as ft

from . import config as cfg
from . import estilo as st
from . import idiomas as idi
from . import modelo


ROJO, AMBAR = st.ROJO, st.AMBAR      # viven en estilo.py


def _item(etiqueta, icono_nombre, al_pulsar, peligro=False):
    """Una opcion del menu.

    En esta version de Flet el texto va dentro de content, no en un
    parametro text.
    """
    # Sin icono: las maquetas 14 y 15 dibujan el menu como texto limpio.
    # Un icono por linea lo llenaba de ruido y hacia mas dificil leerlo.
    return ft.PopupMenuItem(
        content=ft.Container(
            content=ft.Text(etiqueta, size=st.T_MENOR,
                            color=ROJO if peligro else st.C["texto"],
                            font_family=cfg.FUENTE_ALT),
            padding=ft.Padding.symmetric(horizontal=st.E2)),
        on_click=al_pulsar)


def _resumen(dato, es_hist):
    """(titulo, detalle, es_enlace, icono) segun el tipo de entrada.

    El icono no es decoracion: es lo que deja distinguir de un vistazo
    un marcador de una plantilla, que es la diferencia entre pegar y que
    te pregunten antes de pegar.
    """
    if es_hist:
        if dato.get("tipo") == "imagen":
            return (idi.t("Imagen copiada"), idi.t("captura"), False,
                    ft.Icons.IMAGE_OUTLINED)
        texto = dato.get("texto", "")
        if modelo.es_enlace(texto):
            return (modelo.una_linea(texto, 80), modelo.dominio_de(texto),
                    True, ft.Icons.LINK)
        return (modelo.una_linea(texto, 80) or "—",
                "%d caracteres" % len(texto), False, None)

    texto = modelo.texto_de(dato.get("runs", []))
    carpeta = dato.get("categoria", "")
    if modelo.es_enlace(texto):
        return dato["titulo"], modelo.dominio_de(texto), True, ft.Icons.LINK
    return dato["titulo"], carpeta, False, None


class Fila(ft.Container):
    """Una entrada de la lista."""

    def __init__(self, dato, tipo, activa, marcando, marcada, acciones,
                 compacta=False):
        super().__init__()
        self.dato = dato
        self.tipo = tipo
        self.acciones = acciones
        self.compacta = compacta
        es_hist = tipo == "h"
        titulo, detalle, enlace, icono = _resumen(dato, es_hist)
        fijada = bool(dato.get("pin")) if es_hist else False

        izquierda = []
        if activa and not marcando:
            # El filo blanco de la maqueta. Sobre el verde del acento es
            # lo unico que distingue "seleccionada" de "solo resaltada".
            izquierda.append(ft.Container(
                width=st.BARRA_ACTIVA, height=28, border_radius=2,
                bgcolor="#FFFFFF"))
        if marcando:
            izquierda.append(ft.Icon(
                ft.Icons.CHECK_BOX if marcada
                else ft.Icons.CHECK_BOX_OUTLINE_BLANK,
                size=19,
                color=st.C["acento"] if marcada and not activa
                else (st.C["sobre"] if activa else st.C["tenue"])))
        elif icono is not None:
            # Suelto y no dentro de una chapa: las maquetas lo dibujan
            # asi, y solo en las entradas que lo llevan.
            izquierda.append(ft.Icon(
                icono, size=17,
                color=st.C["sobre"] if activa
                else (st.C["acento"] if enlace else st.C["tenue"])))

        lineas = [st.texto(titulo, st.T_CUERPO,
                           st.C["sobre"] if activa else st.C["texto"],
                           ft.FontWeight.W_500 if activa
                           else ft.FontWeight.W_400)]
        if not compacta:
            # En mini la fila es de una sola linea: con 42 px de alto el
            # subtitulo no cabe sin apretar el titulo.
            # El dominio va en acento y no en tenue: es lo que avisa de
            # que ese clic abre el navegador en vez de pegar.
            if activa:
                color_detalle = ft.Colors.with_opacity(0.75, st.C["sobre"])
            elif enlace:
                color_detalle = st.C["acento"]
            else:
                color_detalle = st.C["tenue"]
            lineas.append(st.texto(detalle, st.T_MINI, color_detalle))
        centro = ft.Column(lineas, spacing=2, tight=True,
                           alignment=ft.MainAxisAlignment.CENTER, expand=True)

        derecha = []
        if fijada and not marcando:
            # El alfiler se queda visible siempre que algo este fijado:
            # si no, habria que pasar el raton por cada fila para saber
            # cual lo esta.
            derecha.append(ft.Icon(
                ft.Icons.PUSH_PIN, size=15,
                color=st.C["sobre"] if activa else st.C["acento"]))
        self._menu_ctl = None
        if not marcando:
            self._menu_ctl = self._menu(es_hist, enlace, fijada, activa)
            # Solo asoma al pasar el raton o en la fila activa: con el
            # boton siempre puesto la lista se ve cargada de puntos.
            self._menu_ctl.visible = activa
            derecha.append(self._menu_ctl)

        self.content = ft.Row(
            izquierda + [centro] + derecha,
            spacing=st.E2, vertical_alignment=ft.CrossAxisAlignment.CENTER)
        self.bgcolor = st.C["acento"] if activa else st.C["tarjeta"]
        self.border_radius = st.R_TARJETA
        # 16 sin icono, 22 con el: spec seccion 3, "Filas de la lista".
        # Cuenta el icono de tipo, no la barra de activa ni la casilla.
        self.padding = ft.Padding.only(
            left=22 if (icono is not None and not marcando) else 16,
            right=8, top=4, bottom=4)
        self.height = st.ALTO_FILA_MINI if compacta else st.ALTO_FILA
        self.on_click = self._pulsar
        self.on_hover = self._sobrevolar
        self.ink = not marcando
        self.animate = ft.Animation(130, ft.AnimationCurve.EASE_OUT)
        self._activa = activa

        # En tema claro la tarjeta es blanca sobre fondo casi blanco: sin
        # borde y sin sombra no se despega del fondo. En oscuro sobra.
        claro = st.C["modo"] == ft.ThemeMode.LIGHT
        if activa:
            self.shadow = st.sombra(0.5)
            self.border = None
        elif claro:
            self.shadow = st.sombra(0.25)
            self.border = ft.Border.all(1, st.C["borde"])
        else:
            self.shadow = None
            self.border = None

    def _menu(self, es_hist, enlace, fijada, activa):
        """Un solo boton con todas las acciones detras."""
        def opcion(etiqueta, icono_nombre, accion):
            return _item(etiqueta, icono_nombre,
                         lambda e, a=accion: self.acciones(a, self.dato))

        opciones = []
        if enlace:
            opciones.append(opcion(idi.t("Abrir en el navegador"),
                                   ft.Icons.OPEN_IN_NEW, "abrir"))
            opciones.append(ft.PopupMenuItem())
        opciones += [
            opcion(idi.t("Pegar"), ft.Icons.CONTENT_PASTE, "pegar"),
            opcion(idi.t("Pegar sin formato"), ft.Icons.FORMAT_CLEAR, "pegar_plano"),
            opcion(idi.t("Copiar"), ft.Icons.COPY_ALL, "copiar"),
            ft.PopupMenuItem(),
        ]
        if es_hist:
            opciones.append(opcion(
                idi.t("Quitar de arriba") if fijada else idi.t("Fijar arriba"),
                ft.Icons.PUSH_PIN_OUTLINED, "fijar"))
            if self.dato.get("tipo") == "texto":
                opciones.append(opcion(idi.t("Editar y guardar..."),
                                       ft.Icons.EDIT_OUTLINED, "editar"))
        else:
            opciones.append(opcion(idi.t("Editar..."), ft.Icons.EDIT_OUTLINED,
                                   "editar"))
        opciones += [
            ft.PopupMenuItem(),
            _item(idi.t("Borrar"), ft.Icons.DELETE_OUTLINE,
                  lambda e: self.acciones("borrar", self.dato), True)]

        return ft.PopupMenuButton(
            items=opciones, icon=ft.Icons.MORE_HORIZ, icon_size=18,
            icon_color=st.C["sobre"] if activa else st.C["tenue"],
            menu_position=ft.PopupMenuPosition.UNDER,
            bgcolor=st.C["elevado"],
            shape=ft.RoundedRectangleBorder(radius=12))

    def _pulsar(self, e):
        self.acciones("elegir", self.dato)

    def _sobrevolar(self, e):
        """Resalta la fila bajo el raton y saca su boton de acciones.

        Solo cambia propiedades del contenedor: no se reconstruye nada.
        """
        encima = e.data == "true"
        if self._menu_ctl is not None and not self._activa:
            self._menu_ctl.visible = encima
        if not self._activa:
            self.bgcolor = st.C["hover"] if encima else st.C["tarjeta"]
        self.update()


def cabecera_grupo(etiqueta, cuantos, abierto, al_pulsar, icono=None,
                   color=None):
    """La barra que abre y cierra un grupo dentro de la lista.

    Los marcadores y las notas viven en la misma pestania pero no se
    usan igual: un marcador se abre en el navegador y una nota se pega.
    Separarlos deja encontrar cada cosa sin leerlas todas.
    """
    return ft.Container(
        content=ft.Row([
            ft.Icon(ft.Icons.EXPAND_MORE if abierto
                    else ft.Icons.CHEVRON_RIGHT, size=18,
                    color=st.C["medio"]),
            ft.Icon(icono, size=15, color=color or st.C["medio"])
            if icono else ft.Container(width=0),
            st.texto(etiqueta, st.T_MENOR, st.C["medio"],
                     ft.FontWeight.W_500),
            ft.Container(expand=True),
            st.texto(str(cuantos), st.T_MINI, st.C["tenue"]),
        ], spacing=st.E2, vertical_alignment=ft.CrossAxisAlignment.CENTER),
        height=32, padding=ft.Padding.only(left=st.E2, right=st.E3),
        border_radius=st.R_CONTROL, on_click=al_pulsar, ink=True)


def vacio(mensaje, icono=ft.Icons.CONTENT_PASTE_OFF):
    """El hueco cuando no hay nada que enseniar.

    El icono cambia con el motivo: portapapeles tachado si no se ha
    copiado nada, carpeta si no hay guardados, lupa si la busqueda no
    encontro. Decirlo con el dibujo ahorra leer el mensaje.
    """
    return ft.Container(
        content=ft.Column(
            # 76 y no 30: en las maquetas el dibujo ocupa media columna y
            # es lo que hace que el hueco no parezca un error.
            [ft.Icon(icono, size=76, color=st.C["tenue"]),
             st.texto(mensaje, st.T_MENOR, st.C["tenue"], lineas=2)],
            spacing=st.E4, horizontal_alignment=ft.CrossAxisAlignment.CENTER,
            alignment=ft.MainAxisAlignment.CENTER),
        alignment=ft.Alignment.CENTER, padding=40, expand=True)
