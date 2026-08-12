# -*- coding: utf-8 -*-
"""Los colores vivos y las piezas de interfaz que se repiten."""

import tkinter as tk
import tkinter.font as tkfont

import customtkinter as ctk

from . import config as cfg
from . import windows as win

# Diccionario que el resto del programa consulta. aplicar() lo reescribe.
C = dict(cfg.OSCURA)
C["acento"], C["acento_h"] = cfg.ACENTOS["azul"]
C["sobre"] = "#FFFFFF"

FUENTE_ICONOS = "Segoe Fluent Icons"      # Windows 11
FUENTE_ICONOS_ALT = "Segoe MDL2 Assets"   # Windows 10

IC = {"buscar": "\uE721", "mas": "\uE710", "carpeta": "\uE8F4",
      "lista": "\uE8FD", "pin": "\uE718", "unpin": "\uE77A",
      "editar": "\uE70F", "borrar": "\uE74D", "cerrar": "\uE711",
      "paleta": "\uE790", "escoba": "\uE75C", "check": "\uE73E",
      "pausa": "\uE769", "grabar": "\uE768", "marcado": "\uE73A",
      "sin_marcar": "\uE739", "carpeta_x": "\uED43",
      "mas_opciones": "\uE712", "enlace": "\uE71B", "abrir": "\uE8A7",
      "abajo": "\uE70D"}

SIN_ICONOS = {"buscar": "?", "mas": "+", "carpeta": "[+]", "lista": "=",
              "pin": "^", "unpin": "v", "editar": "/", "borrar": "x",
              "cerrar": "X", "paleta": "@", "escoba": "~", "check": "v",
              "pausa": "||", "grabar": ">", "marcado": "[x]",
              "sin_marcar": "[ ]", "carpeta_x": "[x]",
              "mas_opciones": "...", "enlace": "@", "abrir": ">",
              "abajo": "v"}

_cache = {"iconos": None, "texto": None, "listo": False}


def _detectar_fuentes():
    if _cache["listo"]:
        return
    familias = set(tkfont.families())
    _cache["iconos"] = (FUENTE_ICONOS if FUENTE_ICONOS in familias
                        else FUENTE_ICONOS_ALT
                        if FUENTE_ICONOS_ALT in familias else None)
    _cache["texto"] = (cfg.FUENTE if cfg.FUENTE in familias
                       else cfg.FUENTE_ALT if cfg.FUENTE_ALT in familias
                       else None)
    _cache["listo"] = True


def familia_iconos():
    _detectar_fuentes()
    return _cache["iconos"]


def familia_texto():
    _detectar_fuentes()
    return _cache["texto"] or "TkDefaultFont"


def simbolo(clave):
    return IC[clave] if familia_iconos() else SIN_ICONOS[clave]


def fuente(tam=cfg.T_CUERPO, peso="normal"):
    return ctk.CTkFont(family=familia_texto(), size=tam, weight=peso)


def aplicar(acento=None):
    """Reescribe C segun el tema de Windows y el acento elegido."""
    claro = win.tema_claro()
    C.update(cfg.CLARA if claro else cfg.OSCURA)
    if acento:
        C["nombre_acento"] = acento
    nombre = C.get("nombre_acento", "azul")
    C["acento"], C["acento_h"] = cfg.ACENTOS.get(nombre, cfg.ACENTOS["azul"])
    C["sobre"] = "#FFFFFF"
    ctk.set_appearance_mode("light" if claro else "dark")
    return claro


# ------------------------------------------------------------ piezas

def boton_icono(padre, clave, comando, tam=15, lado=30, color=None):
    """Boton cuadrado con un icono del sistema."""
    return ctk.CTkButton(
        padre, text=simbolo(clave), width=lado, height=lado,
        corner_radius=cfg.R_CONTROL, fg_color="transparent",
        hover_color=C["hover"], text_color=color or C["medio"],
        font=ctk.CTkFont(family=familia_iconos() or familia_texto(), size=tam),
        command=comando)


def boton(padre, texto, comando, ancho=84, alto=34, tipo="normal",
          tam=cfg.T_MENOR):
    """tipo: normal | acento | peligro | fantasma"""
    estilos = {
        "acento": (C["acento"], C["acento_h"], C["sobre"], 0),
        "peligro": ("#DC2626", "#B91C1C", "#FFFFFF", 0),
        "fantasma": ("transparent", C["hover"], C["medio"], 0),
        "normal": (C["tarjeta"], C["hover"], C["texto"], 1),
    }
    fondo, hover, letra, borde = estilos.get(tipo, estilos["normal"])
    return ctk.CTkButton(
        padre, text=texto, command=comando, width=ancho, height=alto,
        corner_radius=cfg.R_CONTROL, fg_color=fondo, hover_color=hover,
        text_color=letra, border_width=borde, border_color=C["borde_claro"],
        font=fuente(tam))


def pildora(padre, texto, comando, activa=False):
    """Ficha redondeada para carpetas y pestanas."""
    return ctk.CTkButton(
        padre, text=texto, command=comando, height=30, width=10,
        corner_radius=15,
        fg_color=C["acento"] if activa else C["tarjeta"],
        hover_color=C["acento_h"] if activa else C["hover"],
        text_color=C["sobre"] if activa else C["medio"],
        font=fuente(cfg.T_MENOR))


def entrada(padre, marcador="", ancho=None):
    return ctk.CTkEntry(
        padre, height=36, corner_radius=cfg.R_CONTROL,
        fg_color=C["tarjeta"], border_color=C["borde"], border_width=1,
        text_color=C["texto"], placeholder_text=marcador,
        placeholder_text_color=C["tenue"], font=fuente(),
        width=ancho if ancho else 140)


def etiqueta(padre, texto, tam=cfg.T_MENOR, color=None):
    return ctk.CTkLabel(padre, text=texto, font=fuente(tam),
                        text_color=color or C["medio"])


def caja_texto(padre):
    """Area de escritura con el mismo aspecto que el resto."""
    marco = ctk.CTkFrame(padre, fg_color=C["tarjeta"],
                         corner_radius=cfg.R_TARJETA)
    caja = tk.Text(marco, wrap="word", undo=True, bd=0,
                   bg=C["tarjeta"], fg=C["texto"],
                   insertbackground=C["acento"],
                   selectbackground=C["acento"], selectforeground=C["sobre"],
                   font=(familia_texto(), 10), padx=cfg.E3, pady=cfg.E3,
                   highlightthickness=0, spacing1=2, spacing3=2)
    caja.pack(fill="both", expand=True, padx=cfg.E1, pady=cfg.E1)
    return marco, caja
