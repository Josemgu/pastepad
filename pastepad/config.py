# -*- coding: utf-8 -*-
"""Constantes, limites y rutas.

Los colores y las medidas NO estan aqui: viven en estilo.py, que es su
unica fuente. Este archivo llego a tener una copia de ambos con valores
distintos, y era peso muerto que solo servia para confundir.

No importa flet ni tkinter a proposito: tiene que poder leerse y
probarse sin abrir una ventana."""

import os
import sys

APP = "pastepad"
VERSION = "3.0.1"

# --- ventana
TAMANOS = {"mini": (300, 380), "chico": (340, 460),
           "mediano": (380, 560), "grande": (470, 700)}
TAMANO_DEF = "mediano"
MIN_ANCHO, MIN_ALTO = 300, 340
MAX_ANCHO, MAX_ALTO = 720, 1100
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
FUENTE_ALT = "Segoe UI"                # Windows 10
FUENTE_DEF, TAM_DEF, COLOR_DEF = "Calibri", 11, "#000000"


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

