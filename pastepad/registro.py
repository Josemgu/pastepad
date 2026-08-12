# -*- coding: utf-8 -*-
"""El unico sitio que escribe en errores.log.

Separado de main.py porque los hilos de app.py tambien fallan y hasta
ahora sus excepciones se perdian enteras: el programa corre sin consola,
asi que no hay stderr que mirar, y cada hilo se tragaba lo suyo con un
except mudo. Ese silencio es lo que hacia imposible diagnosticar nada.

No importa Flet ni tkinter: se puede usar desde cualquier hilo.
"""

import sys
import threading
import time
import traceback

from . import config as cfg

# Los tres hilos pueden anotar a la vez. Sin candado los rastros se
# entrelazan linea con linea y el archivo queda ilegible justo cuando
# hace falta leerlo.
_candado = threading.Lock()

# Ultima firma anotada por origen, para fallo(repetir=False).
_ultimo = {}


def anotar(texto, origen="-"):
    """Aniade una entrada al log. No lanza nunca, pase lo que pase.

    Si esta funcion fallara y dejara subir la excepcion, tumbaria al
    propio manejador de errores que la llamo.
    """
    try:
        with _candado:
            with open(cfg.RUTA_LOG, "a", encoding="utf-8") as f:
                f.write("\n%s  v%s  [%s]\n%s\n" % (
                    time.strftime("%Y-%m-%d %H:%M:%S"), cfg.VERSION,
                    origen, texto.rstrip()))
    except Exception:
        pass


def fallo(origen, repetir=True):
    """Anota la excepcion que se esta manejando ahora mismo.

    Va dentro de un except, en el sitio donde antes habia un 'pass'.
    Con repetir=False un fallo identico consecutivo no se vuelve a
    escribir: el vigilante reintenta cada 0,7 s y un error persistente
    llenaria el archivo con cinco mil copias de lo mismo en una tarde.
    """
    rastro = traceback.format_exc()
    if repetir:
        _ultimo.pop(origen, None)
    else:
        lineas = rastro.strip().splitlines()
        firma = lineas[-1] if lineas else origen
        if _ultimo.get(origen) == firma:
            return
        _ultimo[origen] = firma
    anotar(rastro, origen)


def instalar():
    """Engancha los ganchos globales de excepcion.

    Son dos y no uno: sys.excepthook solo cubre el hilo principal, y lo
    que revienta dentro de un Thread pasa por threading.excepthook, que
    es un gancho aparte. Sin el segundo, un fallo en un hilo no dejaba
    ni una linea.

    Ninguno de los dos cubre page.run_thread: Flet entrega ese trabajo a
    un ThreadPoolExecutor, que atrapa la excepcion dentro de su Future y
    no la deja llegar a ningun gancho. Los bucles lanzados asi tienen
    que capturar y llamar a fallo() por su cuenta.
    """
    sys.excepthook = lambda tipo, valor, rastro: anotar(
        "".join(traceback.format_exception(tipo, valor, rastro)),
        "principal")

    def en_hilo(args):
        if args.exc_type is SystemExit:
            return
        anotar("".join(traceback.format_exception(
            args.exc_type, args.exc_value, args.exc_traceback)),
            "hilo %s" % (args.thread.name if args.thread else "?"))

    threading.excepthook = en_hilo
