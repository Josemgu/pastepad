# -*- coding: utf-8 -*-
"""Punto de entrada.

    pip install -r requirements.txt
    python main.py

Se llama main y no pastepad para no chocar con el paquete del mismo
nombre: Python no sabria cual de los dos importar.
"""

import flet as ft

from pastepad import estilo as st
from pastepad import idiomas as idi
from pastepad import registro
from pastepad import windows as win
from pastepad.app import App
from pastepad.modelo import Almacen


def arrancar(page: ft.Page):
    """Lo primero que corre con la pagina ya creada.

    Va envuelto porque un fallo aqui deja la ventana en blanco sin decir
    nada: se anota y se deja subir, que es lo que hace que ft.run
    termine en vez de quedarse con una ventana muerta.
    """
    try:
        almacen = Almacen()
        idi.poner(almacen.pref("idioma", idi.IDIOMA_DEF))
        st.aplicar(almacen.pref("acento", "menta"),
                   almacen.pref("tema", st.TEMA_DEF))
        if almacen.pref("autoarranque", "si") == "si":
            win.autoarranque(True)
        App(page, almacen)
    except Exception:
        registro.fallo("arrancar")
        raise


if __name__ == "__main__":
    registro.instalar()
    ft.run(arrancar)
