# -*- coding: utf-8 -*-
"""Colores, medidas y piezas de interfaz.

Flet dibuja con Flutter, que rasteriza por GPU: aqui si valen las
sombras, los bordes suaves y las animaciones que tkinter no podia dar.
"""

import flet as ft

from . import config as cfg
from . import windows as win

# Colores fijos, iguales en todos los temas: el peligro tiene que
# leerse igual de rojo en claro que en oscuro, y la carpeta igual
# de ambar.
ROJO = "#EF4444"
AMBAR = "#F5A524"

# El segundo color es el del texto encima. Cambia con cada acento porque
# unos son claros y otros oscuros: blanco fijo sobre ambar o lima no se
# lee.
ACENTOS = {
    "menta":    ("#2DD4A7", "#052E23"),
    # azul, violeta y coral llevaban blanco encima y se quedaban en
    # 3:1, por debajo del 4.5:1 que pide WCAG AA para texto normal. El
    # color de fondo no cambia; solo el de la letra.
    "azul":     ("#4B8DF8", "#04183C"),
    "violeta":  ("#9B7BF7", "#1E1046"),
    "ambar":    ("#F5A524", "#3A2606"),
    "coral":    ("#F76B5C", "#3D0F09"),
    "rosa":     ("#F472B6", "#3A1128"),
    "cian":     ("#22D3EE", "#062E36"),
    "lima":     ("#A3E635", "#1A2E05"),
    "indigo":   ("#818CF8", "#111539"),
    "turquesa": ("#2DD4BF", "#042F2A"),
    "durazno":  ("#FB923C", "#3B1D06"),
    "lavanda":  ("#C084FC", "#2E1065"),
    "esmeralda": ("#34D399", "#04291C"),
    "cielo":    ("#38BDF8", "#052F42"),
    "oro":      ("#FCD34D", "#3B2606"),
    "fresa":    ("#FB7185", "#3F0A16"),
    "menta_fria": ("#5EEAD4", "#032F2A"),
    "arena":    ("#D6BC8A", "#332612"),
}

OSCURA = {
    "fondo": "#0B0B0D", "elevado": "#141417", "tarjeta": "#1B1B1F",
    "hover": "#26262B", "borde": "#242429",
    "texto": "#F4F4F5", "medio": "#9C9CA6", "tenue": "#6B6B75",
    "sombra": "#000000",
}
CLARA = {
    "fondo": "#F6F6F4", "elevado": "#FFFFFF", "tarjeta": "#FFFFFF",
    "hover": "#EFEFEC", "borde": "#E6E6E2",
    "texto": "#141416", "medio": "#5C5C66", "tenue": "#8E8E98",
    "sombra": "#94949E",
}

MEDIANOCHE = {
    "fondo": "#0A0F1E", "elevado": "#111A30", "tarjeta": "#16213C",
    "hover": "#1F2D4D", "borde": "#1C2942",
    "texto": "#E8ECF5", "medio": "#93A0BC", "tenue": "#64708C",
    "sombra": "#000000",
}
GRAFITO = {
    "fondo": "#141414", "elevado": "#1C1C1C", "tarjeta": "#242424",
    "hover": "#2E2E2E", "borde": "#2A2A2A",
    "texto": "#F0F0F0", "medio": "#A0A0A0", "tenue": "#6E6E6E",
    "sombra": "#000000",
}
BOSQUE = {
    "fondo": "#0A140F", "elevado": "#0F1D16", "tarjeta": "#14281E",
    "hover": "#1D3A2B", "borde": "#1A3327",
    "texto": "#E8F2EC", "medio": "#8FB3A0", "tenue": "#5E7A6B",
    "sombra": "#000000",
}
PAPEL = {
    "fondo": "#F5F1E8", "elevado": "#FFFDF7", "tarjeta": "#FFFDF7",
    "hover": "#EDE7D9", "borde": "#E2DACA",
    "texto": "#2B2620", "medio": "#6B6155", "tenue": "#96897A",
    "sombra": "#A89B87",
}

# --- pasteles: fondos suaves, sin gris puro, para uso de dia
NIEBLA = {
    "fondo": "#EEF1F5", "elevado": "#FFFFFF", "tarjeta": "#FFFFFF",
    "hover": "#E2E8F0", "borde": "#DBE2EA",
    "texto": "#1E293B", "medio": "#5A6779", "tenue": "#8A95A6",
    "sombra": "#94A3B8",
}
ARENA = {
    "fondo": "#F7F4EF", "elevado": "#FFFFFF", "tarjeta": "#FFFFFF",
    "hover": "#EEE9E0", "borde": "#E5DED2",
    "texto": "#33302B", "medio": "#6E675C", "tenue": "#948B7E",
    "sombra": "#A8A093",
}
LILA = {
    "fondo": "#F4F1FA", "elevado": "#FFFFFF", "tarjeta": "#FFFFFF",
    "hover": "#E9E3F6", "borde": "#E1D9F2",
    "texto": "#2A2340", "medio": "#655885", "tenue": "#9086AE",
    "sombra": "#A296C4",
}
SALVIA = {
    "fondo": "#EEF7F3", "elevado": "#FFFFFF", "tarjeta": "#FFFFFF",
    "hover": "#DFF0E8", "borde": "#D5E9DF",
    "texto": "#1E332B", "medio": "#557264", "tenue": "#849E90",
    "sombra": "#93B3A4",
}
RUBOR = {
    "fondo": "#FBF2F4", "elevado": "#FFFFFF", "tarjeta": "#FFFFFF",
    "hover": "#F5E3E8", "borde": "#EEDAE0",
    "texto": "#3A2229", "medio": "#77565F", "tenue": "#A5858E",
    "sombra": "#B79AA2",
}

# "auto" sigue al tema de Windows; el resto manda sobre el sistema.
TEMAS = {
    "auto": None,
    "oscuro": OSCURA, "medianoche": MEDIANOCHE, "grafito": GRAFITO,
    "bosque": BOSQUE,
    "claro": CLARA, "papel": PAPEL, "niebla": NIEBLA, "arena": ARENA,
    "lila": LILA, "salvia": SALVIA, "rubor": RUBOR,
}
TEMA_DEF = "auto"

# Los que se leen sobre fondo claro: hay que decirle a Flutter que use
# su modo claro o los menus nativos saldrian oscuros por dentro.
TEMAS_CLAROS = ("claro", "papel", "niebla", "arena", "lila", "salvia",
                "rubor")

C = dict(OSCURA)
C["acento"], C["sobre"] = ACENTOS["menta"]
C["nombre"] = "menta"
C["modo"] = ft.ThemeMode.DARK

# --- medidas. Salen de las maquetas SVG de docs/maquetas.
R_PANEL, R_TARJETA, R_CONTROL = 20, 14, 12
E1, E2, E3, E4 = 4, 8, 12, 16
ALTO_FILA = 56
SEP_FILA = 6         # hueco entre tarjetas: 56 + 6 = 62 de paso
BARRA_ACTIVA = 3     # el filo blanco que marca la fila seleccionada

# --- tipografia. Los cuatro tamanos de ESPECIFICACION-UI.md, seccion 3.
# Las maquetas dibujan 12.5 y 10 en algunos textos, pero manda la spec:
# cuatro tamanos y nada intermedio.
T_TITULO, T_CUERPO, T_MENOR, T_MINI = 15, 13, 12, 11

# Fila en modo mini: mas baja y de una sola linea, sin subtitulo.
ALTO_FILA_MINI = 42

# Alturas fijas, medidas sobre las maquetas. Van explicitas y no salidas
# del padding: Flutter reserva su propio minimo y los inflaba (pestanas
# a 49 en vez de 30, buscador a 66 en vez de 42).
ALTO_BUSCADOR = 42
ALTO_PESTANA = 30
ALTO_BOTON = 26
LADO_CABECERA = 20      # los tres circulos de arriba, radio 10


def aplicar(acento=None, tema=None):
    """Recalcula toda la paleta.

    Con tema="auto" (o sin tema) sigue al de Windows; con cualquier otro
    nombre manda el elegido. Modifica C en el sitio, asi que quien ya
    tenga la referencia ve los colores nuevos.
    """
    if tema:
        C["tema"] = tema
    nombre_tema = C.get("tema", TEMA_DEF)
    if nombre_tema == "auto" or nombre_tema not in TEMAS:
        claro = win.tema_claro()
        C.update(CLARA if claro else OSCURA)
    else:
        claro = nombre_tema in TEMAS_CLAROS
        C.update(TEMAS[nombre_tema])
    if acento:
        C["nombre"] = acento
    C["acento"], C["sobre"] = ACENTOS.get(C["nombre"], ACENTOS["menta"])
    C["modo"] = ft.ThemeMode.LIGHT if claro else ft.ThemeMode.DARK
    return claro


GROSOR_BORDE = 6        # franja sensible al arrastre, en pixeles


def borde_redimension(page, lado, ancho=None, alto=None):
    """Una franja invisible que redimensiona la ventana al arrastrarla.

    Una ventana sin marco no tiene bordes de sistema, y por eso no se
    puede estirar aunque resizable este en True: no hay nada que agarrar.
    Windows dibuja esos bordes; aqui hay que ponerlos a mano.

    El cursor cambia solo al pasar por encima, que es lo que le dice al
    usuario que ahi se puede tirar.
    """
    cursores = {
        ft.WindowResizeEdge.LEFT: ft.MouseCursor.RESIZE_LEFT_RIGHT,
        ft.WindowResizeEdge.RIGHT: ft.MouseCursor.RESIZE_LEFT_RIGHT,
        ft.WindowResizeEdge.TOP: ft.MouseCursor.RESIZE_UP_DOWN,
        ft.WindowResizeEdge.BOTTOM: ft.MouseCursor.RESIZE_UP_DOWN,
        ft.WindowResizeEdge.TOP_LEFT: ft.MouseCursor.RESIZE_UP_LEFT,
        ft.WindowResizeEdge.TOP_RIGHT: ft.MouseCursor.RESIZE_UP_RIGHT,
        ft.WindowResizeEdge.BOTTOM_LEFT: ft.MouseCursor.RESIZE_DOWN_LEFT,
        ft.WindowResizeEdge.BOTTOM_RIGHT: ft.MouseCursor.RESIZE_DOWN_RIGHT,
    }
    return ft.GestureDetector(
        content=ft.Container(width=ancho, height=alto,
                             bgcolor=ft.Colors.TRANSPARENT),
        mouse_cursor=cursores.get(lado, ft.MouseCursor.BASIC),
        on_pan_start=lambda e, l=lado: page.window.start_resizing(l))


def sombra(intensidad=1.0):
    """Sombra suave: es lo que separa una tarjeta del fondo sin bordes."""
    return ft.BoxShadow(
        spread_radius=0,
        blur_radius=16 * intensidad,
        color=ft.Colors.with_opacity(0.30 * intensidad, C["sombra"]),
        offset=ft.Offset(0, 5 * intensidad),
    )


def texto(valor, tam=T_CUERPO, color=None, peso=None, lineas=1):
    return ft.Text(
        valor, size=tam, color=color or C["texto"],
        weight=peso or ft.FontWeight.W_400,
        max_lines=lineas, overflow=ft.TextOverflow.ELLIPSIS,
        font_family=cfg.FUENTE_ALT)


def icono(nombre, al_pulsar, tam=18, color=None, tip=None, fondo=False,
          lado=None):
    """Boton de icono. Con fondo=True sale como pastilla redonda.

    Los tres de la cabecera (pausa, paleta, cerrar) llevan fondo: sin el
    se pierden contra el panel, que es casi negro. Su lado es 20 porque
    las maquetas los dibujan con radio 10; el padding de Flutter tiraba
    a 36 y descolgaba toda la cabecera.
    """
    lado = lado or (LADO_CABECERA if fondo else 36)
    return ft.IconButton(
        icon=nombre, icon_size=tam, icon_color=color or C["medio"],
        on_click=al_pulsar, tooltip=tip, width=lado, height=lado,
        bgcolor=C["tarjeta"] if fondo else None,
        padding=0,
        style=ft.ButtonStyle(
            padding=0,
            shape=ft.CircleBorder() if fondo
            else ft.RoundedRectangleBorder(radius=10),
            overlay_color=ft.Colors.with_opacity(0.08, C["texto"])))


def pildora(etiqueta, al_pulsar, activa=False, expandir=True):
    return ft.Container(
        content=ft.Text(
            etiqueta, size=T_MENOR,
            color=C["sobre"] if activa else C["medio"],
            weight=ft.FontWeight.W_500 if activa else ft.FontWeight.W_400,
            font_family=cfg.FUENTE_ALT, text_align=ft.TextAlign.CENTER,
            max_lines=1, overflow=ft.TextOverflow.ELLIPSIS),
        bgcolor=C["acento"] if activa else C["tarjeta"],
        height=ALTO_PESTANA,
        padding=ft.Padding.symmetric(horizontal=16),
        border_radius=15,
        # alignment solo cuando la pastilla se estira. Las carpetas van
        # en una fila con scroll, donde el ancho no esta acotado: pedir
        # centrado en un espacio infinito dejaba al texto sin sitio y
        # las pastillas salian vacias.
        alignment=ft.Alignment.CENTER if expandir else None,
        on_click=al_pulsar, ink=True, expand=expandir,
        animate=ft.Animation(140, ft.AnimationCurve.EASE_OUT))


def boton(etiqueta, al_pulsar, tipo="normal", icono_nombre=None):
    estilos = {"acento": (C["acento"], C["sobre"]),
               "peligro": ("#DC2626", "#FFFFFF"),
               "normal": (C["tarjeta"], C["texto"])}
    fondo, letra = estilos.get(tipo, estilos["normal"])
    hijos = []
    if icono_nombre:
        hijos.append(ft.Icon(icono_nombre, size=16, color=letra))
    hijos.append(ft.Text(etiqueta, size=T_MENOR, color=letra,
                         weight=ft.FontWeight.W_500,
                         font_family=cfg.FUENTE_ALT))
    return ft.Container(
        content=ft.Row(hijos, spacing=6, tight=True,
                       alignment=ft.MainAxisAlignment.CENTER),
        bgcolor=fondo,
        height=ALTO_BOTON,
        padding=ft.Padding.symmetric(horizontal=16),
        alignment=ft.Alignment.CENTER,
        border_radius=10, on_click=al_pulsar, ink=True,
        animate=ft.Animation(120, ft.AnimationCurve.EASE_OUT))


def campo(marcador, al_cambiar=None, al_enviar=None, valor="", lineas=1,
          icono_nombre=None, alto=None):
    una = lineas <= 1
    return ft.TextField(
        value=valor, hint_text=marcador, on_change=al_cambiar,
        on_submit=al_enviar, border_radius=R_CONTROL, filled=True,
        prefix_icon=icono_nombre,
        height=alto if una else None,
        bgcolor=C["elevado"], border_color=ft.Colors.TRANSPARENT,
        focused_border_color=C["acento"], border_width=1,
        focused_border_width=1, color=C["texto"],
        hint_style=ft.TextStyle(color=C["tenue"], size=T_CUERPO),
        text_size=T_CUERPO, cursor_color=C["acento"],
        content_padding=ft.Padding.symmetric(horizontal=14,
                                             vertical=8 if una else 12),
        multiline=not una, min_lines=lineas, max_lines=lineas,
        text_style=ft.TextStyle(font_family=cfg.FUENTE_ALT))


def desplegable(valor, opciones, al_cambiar=None, visibles=4):
    """Lista desplegable con el aspecto del tema.

    menu_height limita el menu a `visibles` filas y deja el resto tras
    scroll: con doce idiomas o doce atajos, un menu sin tope se sale de
    la pantalla y las ultimas opciones no se alcanzan.
    """
    d = ft.Dropdown(
        value=valor,
        options=[ft.DropdownOption(o) for o in opciones],
        menu_height=visibles * 44,
        border_radius=R_CONTROL, filled=True, bgcolor=C["tarjeta"],
        border_color=ft.Colors.TRANSPARENT,
        focused_border_color=C["acento"], text_size=T_MENOR,
        color=C["texto"],
        content_padding=ft.Padding.symmetric(horizontal=12, vertical=10))
    if al_cambiar:
        # on_change no se acepta en el constructor de esta version.
        d.on_change = al_cambiar
    return d
