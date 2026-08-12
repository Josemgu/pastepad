# -*- coding: utf-8 -*-
"""Colores, medidas y piezas de interfaz.

Flet dibuja con Flutter, que rasteriza por GPU: aqui si valen las
sombras, los bordes suaves y las animaciones que tkinter no podia dar.
"""

import flet as ft

from . import config as cfg
from . import windows as win

ACENTOS = {
    "menta":   ("#2DD4A7", "#052E23"),
    "azul":    ("#4B8DF8", "#FFFFFF"),
    "violeta": ("#9B7BF7", "#FFFFFF"),
    "ambar":   ("#F5A524", "#3A2606"),
    "coral":   ("#F76B5C", "#FFFFFF"),
    "rosa":    ("#F472B6", "#3A1128"),
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

C = dict(OSCURA)
C["acento"], C["sobre"] = ACENTOS["menta"]
C["nombre"] = "menta"
C["modo"] = ft.ThemeMode.DARK

# --- medidas
R_PANEL, R_TARJETA, R_CONTROL = 20, 14, 12
E1, E2, E3, E4 = 4, 8, 12, 16
ALTO_FILA = 58

# --- tipografia
T_TITULO, T_CUERPO, T_MENOR, T_MINI = 15, 13, 12, 11


def aplicar(acento=None):
    """Recalcula los colores segun el tema de Windows."""
    claro = win.tema_claro()
    C.update(CLARA if claro else OSCURA)
    if acento:
        C["nombre"] = acento
    C["acento"], C["sobre"] = ACENTOS.get(C["nombre"], ACENTOS["menta"])
    C["modo"] = ft.ThemeMode.LIGHT if claro else ft.ThemeMode.DARK
    return claro


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


def icono(nombre, al_pulsar, tam=18, color=None, tip=None):
    return ft.IconButton(
        icon=nombre, icon_size=tam, icon_color=color or C["medio"],
        on_click=al_pulsar, tooltip=tip, width=36, height=36,
        style=ft.ButtonStyle(
            shape=ft.RoundedRectangleBorder(radius=10),
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
        padding=ft.Padding.symmetric(horizontal=16, vertical=9),
        border_radius=18, alignment=ft.Alignment.CENTER,
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
        padding=ft.Padding.symmetric(horizontal=18, vertical=11),
        border_radius=R_CONTROL, on_click=al_pulsar, ink=True,
        animate=ft.Animation(120, ft.AnimationCurve.EASE_OUT))


def campo(marcador, al_cambiar=None, al_enviar=None, valor="", lineas=1):
    return ft.TextField(
        value=valor, hint_text=marcador, on_change=al_cambiar,
        on_submit=al_enviar, border_radius=R_CONTROL, filled=True,
        bgcolor=C["elevado"], border_color=ft.Colors.TRANSPARENT,
        focused_border_color=C["acento"], border_width=1,
        focused_border_width=1, color=C["texto"],
        hint_style=ft.TextStyle(color=C["tenue"], size=T_CUERPO),
        text_size=T_CUERPO, cursor_color=C["acento"],
        content_padding=ft.Padding.symmetric(horizontal=14, vertical=12),
        multiline=lineas > 1, min_lines=lineas, max_lines=lineas,
        text_style=ft.TextStyle(font_family=cfg.FUENTE_ALT))
