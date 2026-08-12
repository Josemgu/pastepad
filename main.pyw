# -*- coding: utf-8 -*-
"""Punto de entrada.

    pip install -r requirements.txt
    python main.pyw

Se llama main y no pastepad para no chocar con el paquete del mismo
nombre: Python no sabria cual de los dos importar.
"""

import sys
import time
import traceback
from tkinter import messagebox

from pastepad import config as cfg
from pastepad import windows as win
from pastepad.modelo import Almacen
from pastepad.panel import Panel
from pastepad.tema import aplicar


def registrar_error(texto):
    """Deja el fallo escrito.

    Sin consola, un error hace que la ventana desaparezca sin decir
    nada; este archivo es lo unico que queda para saber que paso.
    """
    try:
        with open(cfg.RUTA_LOG, "a", encoding="utf-8") as f:
            f.write("\n%s  v%s\n%s\n" % (
                time.strftime("%Y-%m-%d %H:%M:%S"), cfg.VERSION, texto))
    except Exception:
        pass


def al_fallar(tipo, valor, rastro):
    registrar_error("".join(traceback.format_exception(tipo, valor, rastro)))
    try:
        messagebox.showerror(
            cfg.APP, "Algo fallo y quedo anotado en errores.log\n\n%s: %s"
                     % (tipo.__name__, valor))
    except Exception:
        pass


def main():
    sys.excepthook = al_fallar
    almacen = Almacen()
    aplicar(almacen.pref("acento", "azul"))
    if almacen.pref("autoarranque", "si") == "si":
        win.autoarranque(True)
    app = Panel(almacen)
    app.mostrar()
    app.mainloop()


if __name__ == "__main__":
    main()
