# -*- coding: utf-8 -*-
"""Colores, medidas y constantes. No importa tkinter a proposito:
esto tiene que poder leerse y probarse sin abrir una ventana."""

import os
import sys

APP = "pastepad"
VERSION = "3.0.0"

# --- ventana
TAMANOS = {"mini": (300, 380), "chico": (340, 460),
           "mediano": (380, 560), "grande": (470, 700)}
TAMANO_DEF = "mediano"
MIN_ANCHO, MIN_ALTO = 300, 340
MAX_ANCHO, MAX_ALTO = 720, 1100
AGARRE = 18          # lado del triangulo de la esquina para estirar

# --- atajos que se hacen con una sola mano
ATAJOS = {
    "ctrl+shift+v": "Ctrl + Shift + V",
    "ctrl+q": "Ctrl + Q",
    "ctrl+space": "Ctrl + Espacio",
    "ctrl+shift+space": "Ctrl + Shift + Espacio",
    "alt+q": "Alt + Q",
    "ctrl+alt+v": "Ctrl + Alt + V",
}
ATAJO_DEF = "ctrl+shift+v"

# --- limites
MAX_HIST = 80
MAX_CARACTERES = 200_000

# Anota en errores.log el estado del panel en cada pulsacion del atajo.
# Esta puesto mientras siga abierto el fallo de "el atajo responde una
# sola vez": es lo que permite ver si self.visible se desincroniza de la
# ventana real. Ponlo en False cuando ese fallo este cerrado.
TRAZA_ATAJO = True

# --- tipografia
FUENTE = "Segoe UI Variable Display"   # Windows 11
FUENTE_ALT = "Segoe UI"                # Windows 10
FUENTE_DEF, TAM_DEF, COLOR_DEF = "Calibri", 11, "#000000"

# Escala tipografica: cuatro tamanos y basta.
T_TITULO, T_CUERPO, T_MENOR, T_MINI = 15, 13, 11, 10

# --- espaciado, en multiplos de 4
E1, E2, E3, E4, E5 = 4, 8, 12, 16, 24

# --- radios. Generosos a proposito: es lo que separa una interfaz
# actual de una que parece de hace diez anos.
R_PANEL, R_TARJETA, R_CONTROL, R_PILDORA = 18, 14, 12, 999

# Acentos saturados, no pasteles: en fondo oscuro los apagados se
# pierden.
ACENTOS = {
    "menta": ("#2DD4A7", "#22B58D"),
    "azul": ("#4B8DF8", "#3B76DD"),
    "violeta": ("#9B7BF7", "#8460E8"),
    "ambar": ("#F5A524", "#DB8E14"),
    "coral": ("#F76B5C", "#E1523F"),
    "rosa": ("#F472B6", "#E0559F"),
}

# Las superficies suben en escalones: fondo, tarjeta, hover. Sin ese
# gris intermedio todo queda plano y hay que meter bordes por todos
# lados para separar las cosas.
OSCURA = {
    "fondo": "#0B0B0D", "elevado": "#141417", "tarjeta": "#1B1B1F",
    "hover": "#26262B", "borde": "#1F1F23", "borde_claro": "#2E2E34",
    "texto": "#F4F4F5", "medio": "#9C9CA6", "tenue": "#6B6B75",
}
CLARA = {
    "fondo": "#F6F6F4", "elevado": "#FFFFFF", "tarjeta": "#FFFFFF",
    "hover": "#EDEDEA", "borde": "#E8E8E4", "borde_claro": "#DBDBD6",
    "texto": "#141416", "medio": "#5C5C66", "tenue": "#8E8E98",
}


def carpeta_base():
    """Donde viven los datos: junto al ejecutable, o al codigo fuente."""
    if getattr(sys, "frozen", False):
        return os.path.dirname(sys.executable)
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


RUTA_DATOS = os.path.join(carpeta_base(), "snippets.json")
RUTA_HIST = os.path.join(carpeta_base(), "historial.json")
RUTA_PREFS = os.path.join(carpeta_base(), "config.json")
RUTA_LOG = os.path.join(carpeta_base(), "errores.log")
DIR_IMG = os.path.join(carpeta_base(), "imagenes")


def ruta_icono():
    """El .ico puede estar empaquetado (sys._MEIPASS) o en docs/."""
    posibles = []
    temporal = getattr(sys, "_MEIPASS", None)
    if temporal:
        posibles.append(os.path.join(temporal, "pastepad.ico"))
    posibles.append(os.path.join(carpeta_base(), "pastepad.ico"))
    posibles.append(os.path.join(carpeta_base(), "docs", "pastepad.ico"))
    for p in posibles:
        if os.path.exists(p):
            return p
    return None
