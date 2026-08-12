# -*- coding: utf-8 -*-
"""Todo lo que habla con Windows: portapapeles, foco, ventana, arranque.

Aparte del resto para que el codigo de la interfaz no tenga que saber
nada de handles ni de estructuras del sistema.
"""

import ctypes
import os
import sys
import time

import pyperclip

from . import config as cfg

try:
    import win32api
    import win32clipboard
    import win32con
    import win32gui
    import win32process
    HAY_WIN32 = True
except Exception:
    HAY_WIN32 = False

try:
    import winreg
    HAY_REG = True
except Exception:
    HAY_REG = False

CLAVE_RUN = r"Software\Microsoft\Windows\CurrentVersion\Run"
NOMBRE_RUN = "GestorSnippets"
_MI_PID = os.getpid()

# Formatos con los que un programa dice "no guardes esto". Los usan
# KeePass, Bitwarden, el Administrador de credenciales de Windows y el
# modo incognito de Chrome.
# learn.microsoft.com/windows/win32/dataxchg/clipboard-formats
FORMATOS_PRIVADOS = ("Clipboard Viewer Ignore",
                     "ExcludeClipboardContentFromMonitorProcessing")
FORMATOS_CERO = ("CanIncludeInClipboardHistory", "CanUploadToCloudClipboard")


# ------------------------------------------------------------ portapapeles

def _con_portapapeles(accion, intentos=4):
    """Abre el portapapeles, hace algo y lo cierra pase lo que pase.

    Windows solo deja abrirlo a un programa a la vez, asi que hay que
    reintentar.
    """
    if not HAY_WIN32:
        return None
    for _ in range(intentos):
        try:
            win32clipboard.OpenClipboard()
            try:
                return accion()
            finally:
                win32clipboard.CloseClipboard()
        except Exception:
            try:
                win32clipboard.CloseClipboard()
            except Exception:
                pass
            time.sleep(0.04)
    return None


def contenido_privado():
    """True si quien copio pidio que no se guarde. Con el portapapeles
    ya abierto."""
    try:
        for nombre in FORMATOS_PRIVADOS:
            f = win32clipboard.RegisterClipboardFormat(nombre)
            if f and win32clipboard.IsClipboardFormatAvailable(f):
                return True
        for nombre in FORMATOS_CERO:
            f = win32clipboard.RegisterClipboardFormat(nombre)
            if f and win32clipboard.IsClipboardFormatAvailable(f):
                dato = win32clipboard.GetClipboardData(f)
                if isinstance(dato, bytes):
                    if int.from_bytes(dato[:4], "little") == 0:
                        return True
                elif not dato:
                    return True
    except Exception:
        pass
    return False


def secuencia():
    """Contador que Windows sube con cada copia.

    Leerlo cuesta una llamada; abrir el portapapeles y traer el texto
    cuesta muchisimo mas. Si el numero no cambio, no hay nada que hacer.
    """
    if not HAY_WIN32:
        return None
    try:
        return ctypes.windll.user32.GetClipboardSequenceNumber()
    except Exception:
        return None


def leer():
    """('texto', str) | ('imagen', bytes) | ('privado', None) | (None, None)"""
    if not HAY_WIN32:
        try:
            return "texto", pyperclip.paste()
        except Exception:
            return None, None

    def hacer():
        if contenido_privado():
            return "privado", None
        if win32clipboard.IsClipboardFormatAvailable(win32con.CF_UNICODETEXT):
            return "texto", win32clipboard.GetClipboardData(
                win32con.CF_UNICODETEXT)
        if win32clipboard.IsClipboardFormatAvailable(win32con.CF_DIB):
            return "imagen", win32clipboard.GetClipboardData(win32con.CF_DIB)
        return None, None
    return _con_portapapeles(hacer, 2) or (None, None)


def _escapar_rtf(s):
    salida = []
    for ch in s:
        if ch in "\\{}":
            salida.append("\\" + ch)
        elif ch == "\n":
            salida.append("\\par\n")
        elif ch == "\t":
            salida.append("\\tab ")
        elif ord(ch) < 128:
            salida.append(ch)
        else:
            salida.append("\\u%d?" % ord(ch))
    return "".join(salida)


def a_rtf(fragmentos):
    """Convierte los fragmentos a RTF, que es lo que entienden Word y
    Outlook."""
    fuentes, colores = [], []
    for f in fragmentos:
        if f["f"] not in fuentes:
            fuentes.append(f["f"])
        if f["c"].upper() not in colores:
            colores.append(f["c"].upper())
    tabla_f = "".join("{\\f%d\\fnil %s;}" % (i, n)
                      for i, n in enumerate(fuentes))
    tabla_c = ";".join("\\red%d\\green%d\\blue%d"
                       % (int(c[1:3], 16), int(c[3:5], 16), int(c[5:7], 16))
                       for c in colores)
    cuerpo = ["\\pard\\plain "]
    for f in fragmentos:
        cuerpo.append("\\f%d\\fs%d\\cf%d" % (fuentes.index(f["f"]),
                                             int(f["s"] * 2),
                                             colores.index(f["c"].upper()) + 1))
        for marca, clave in (("\\b", "b"), ("\\i", "i"), ("\\ul", "u")):
            if f[clave]:
                cuerpo.append(marca)
        cuerpo.append(" " + _escapar_rtf(f["t"]))
        for marca, clave in (("\\ulnone", "u"), ("\\i0", "i"), ("\\b0", "b")):
            if f[clave]:
                cuerpo.append(marca)
    return "{\\rtf1\\ansi\\deff0{\\fonttbl%s}{\\colortbl;%s;}%s}" % (
        tabla_f, tabla_c, "".join(cuerpo))


def copiar(fragmentos, texto_de, sin_formato=False):
    """Deja el texto en el portapapeles en dos versiones a la vez: con
    formato para Word, plana para todo lo demas."""
    plano = texto_de(fragmentos)
    if sin_formato or not HAY_WIN32:
        pyperclip.copy(plano)
        return
    rtf = a_rtf(fragmentos).encode("ascii", "replace")

    def hacer():
        win32clipboard.EmptyClipboard()
        win32clipboard.SetClipboardData(win32con.CF_UNICODETEXT, plano)
        formato = win32clipboard.RegisterClipboardFormat("Rich Text Format")
        win32clipboard.SetClipboardData(formato, rtf)
        return True
    if not _con_portapapeles(hacer):
        pyperclip.copy(plano)


def dib_a_bmp(dib):
    """Le pone cabecera de archivo a los bytes crudos que da Windows."""
    return (b"BM" + (14 + len(dib)).to_bytes(4, "little") + b"\x00" * 4 +
            (54).to_bytes(4, "little") + dib)


def copiar_imagen(ruta):
    if not HAY_WIN32:
        return False
    try:
        with open(ruta, "rb") as f:
            dib = f.read()[14:]
    except Exception:
        return False

    def hacer():
        win32clipboard.EmptyClipboard()
        win32clipboard.SetClipboardData(win32con.CF_DIB, dib)
        return True
    return bool(_con_portapapeles(hacer))


# ------------------------------------------------------------ foco

def ventana_activa():
    """Handle de la ventana con el foco, salvo que sea la nuestra."""
    if not HAY_WIN32:
        return None
    try:
        hwnd = win32gui.GetForegroundWindow()
        if not hwnd:
            return None
        _, pid = win32process.GetWindowThreadProcessId(hwnd)
        return None if pid == _MI_PID else hwnd
    except Exception:
        return None


def devolver_foco(hwnd):
    """Vuelve a poner esa ventana al frente, con su cursor donde estaba.

    Windows no deja que cualquier proceso robe el primer plano. El truco
    es engancharse al hilo de esa ventana un instante: mientras dura el
    enganche, SetForegroundWindow si funciona.
    """
    if not HAY_WIN32 or not hwnd:
        return False
    try:
        if not win32gui.IsWindow(hwnd):
            return False
        user32 = ctypes.windll.user32
        hilo_destino, _ = win32process.GetWindowThreadProcessId(hwnd)
        hilo_propio = win32api.GetCurrentThreadId()
        enganchado = False
        if hilo_destino and hilo_destino != hilo_propio:
            enganchado = bool(user32.AttachThreadInput(hilo_propio,
                                                       hilo_destino, True))
        try:
            if win32gui.IsIconic(hwnd):
                win32gui.ShowWindow(hwnd, win32con.SW_RESTORE)
            win32gui.SetForegroundWindow(hwnd)
            try:
                win32gui.SetFocus(hwnd)
            except Exception:
                pass
        finally:
            if enganchado:
                user32.AttachThreadInput(hilo_propio, hilo_destino, False)
        return True
    except Exception:
        return False


# ------------------------------------------------------------ ventana

GA_ROOT = 2


def hwnd_real(widget):
    """La ventana que Windows conoce.

    winfo_id() devuelve el handle del widget interno de Tk. GetParent
    sube un nivel, pero segun como este montada la ventana pueden ser
    varios; GetAncestor con GA_ROOT llega arriba del todo de una vez.
    """
    try:
        h = widget.winfo_id()
        if HAY_WIN32:
            raiz = ctypes.windll.user32.GetAncestor(h, GA_ROOT)
            if raiz:
                return raiz
            padre = ctypes.windll.user32.GetParent(h)
            if padre:
                return padre
        return h
    except Exception:
        return None


def redondear(hwnd, ancho, alto, radio=16):
    """Recorta la ventana con esquinas curvas, como hace Windows 11."""
    if not HAY_WIN32 or not hwnd:
        return
    try:
        region = ctypes.windll.gdi32.CreateRoundRectRgn(
            0, 0, ancho + 1, alto + 1, radio, radio)
        if region:
            # Windows se queda con la region: no hay que liberarla.
            ctypes.windll.user32.SetWindowRgn(hwnd, region, True)
    except Exception:
        pass


def marco_hueco(hwnd, ancho, alto, radio=18, grosor=2):
    """Recorta la ventana dejando solo el borde: el centro queda
    transparente. Sirve para el contorno que marca el tamano futuro."""
    if not HAY_WIN32 or not hwnd:
        return
    try:
        gdi = ctypes.windll.gdi32
        fuera = gdi.CreateRoundRectRgn(0, 0, ancho + 1, alto + 1,
                                       radio, radio)
        if not fuera:
            return
        dentro = gdi.CreateRoundRectRgn(
            grosor, grosor, ancho + 1 - grosor, alto + 1 - grosor,
            max(1, radio - grosor), max(1, radio - grosor))
        if dentro:
            gdi.CombineRgn(fuera, fuera, dentro, 4)   # RGN_DIFF
            gdi.DeleteObject(dentro)
        ctypes.windll.user32.SetWindowRgn(hwnd, fuera, True)
    except Exception:
        pass


VK_CONTROL, VK_V = 0x11, 0x56
KEYEVENTF_KEYUP = 0x0002


VK_SHIFT, VK_MENU = 0x10, 0x12


def pegar_con_teclado():
    """Manda Ctrl+V con la API de Windows.

    La libreria keyboard esta ocupada escuchando el atajo global;
    pedirle que ademas escriba puede dejarla sin responder.
    """
    if not HAY_WIN32:
        return False
    try:
        user32 = ctypes.windll.user32
        # El atajo lleva Shift o Alt, y pueden seguir pulsadas: si no se
        # sueltan primero, el destino recibe Ctrl+Shift+V y no Ctrl+V.
        for tecla in (VK_SHIFT, VK_MENU):
            user32.keybd_event(tecla, 0, KEYEVENTF_KEYUP, 0)
        time.sleep(0.02)
        user32.keybd_event(VK_CONTROL, 0, 0, 0)
        user32.keybd_event(VK_V, 0, 0, 0)
        time.sleep(0.03)
        user32.keybd_event(VK_V, 0, KEYEVENTF_KEYUP, 0)
        user32.keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0)
        return True
    except Exception:
        return False


def puntero():
    """Donde esta el raton ahora mismo, en pixeles de pantalla."""
    if HAY_WIN32:
        try:
            return win32api.GetCursorPos()
        except Exception:
            pass
    return (0, 0)


def pantalla():
    """Ancho y alto del escritorio completo."""
    if HAY_WIN32:
        try:
            user32 = ctypes.windll.user32
            return user32.GetSystemMetrics(0), user32.GetSystemMetrics(1)
        except Exception:
            pass
    return (1920, 1080)


def area_util(x, y, ancho_pantalla, alto_pantalla):
    """El area del monitor donde esta el punto, sin la barra de tareas."""
    if HAY_WIN32:
        try:
            info = win32api.GetMonitorInfo(
                win32api.MonitorFromPoint((x, y), 2))
            return info["Work"]
        except Exception:
            pass
    return (0, 0, ancho_pantalla, alto_pantalla)


def tema_claro():
    """True si Windows esta en tema claro."""
    if not HAY_REG:
        return False
    try:
        k = winreg.OpenKey(
            winreg.HKEY_CURRENT_USER,
            r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")
        try:
            valor, _ = winreg.QueryValueEx(k, "AppsUseLightTheme")
            return bool(valor)
        finally:
            winreg.CloseKey(k)
    except Exception:
        return False


def autoarranque(activar=True):
    if not HAY_REG:
        return
    ruta = sys.executable if getattr(sys, "frozen", False) \
        else os.path.abspath(sys.argv[0])
    try:
        k = winreg.OpenKey(winreg.HKEY_CURRENT_USER, CLAVE_RUN, 0,
                           winreg.KEY_SET_VALUE)
        try:
            if activar:
                winreg.SetValueEx(k, NOMBRE_RUN, 0, winreg.REG_SZ,
                                  '"%s"' % ruta)
            else:
                try:
                    winreg.DeleteValue(k, NOMBRE_RUN)
                except FileNotFoundError:
                    pass
        finally:
            winreg.CloseKey(k)
    except Exception:
        pass
