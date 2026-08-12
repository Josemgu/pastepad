# -*- coding: utf-8 -*-
"""Todo lo que habla con Windows: portapapeles, foco, ventana, arranque.

Aparte del resto para que el codigo de la interfaz no tenga que saber
nada de handles ni de estructuras del sistema.
"""

import ctypes
import os
import queue
import sys
import threading
import time
from ctypes import wintypes

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


# --------------------------------------------------- una sola instancia

# Nombre unico del candado y del aviso. Van con un GUID para que no
# choquen con nada mas del sistema.
CANDADO = "Local\\pastepad-3ff1c0de-instancia-unica"
AVISO_MOSTRAR = "pastepad-3ff1c0de-mostrate"
ERROR_YA_EXISTE = 183          # ERROR_ALREADY_EXISTS


CLASE_BUZON = "pastepad_buzon_3ff1c0de"
HWND_MESSAGE = -3
_handle_candado = None
_wndproc = None          # hay que conservarlo o Python lo recolecta


def reservar_instancia():
    """True si somos el primero. False si ya hay otro pastepad abierto.

    Sin esto, abrir el programa dos veces deja dos procesos peleandose:
    Windows solo le da el atajo global a uno, y el segundo se queda como
    una ventana muda que no responde a nada. Desde fuera parece que el
    programa "se abre una vez y ya no vuelve a salir".
    """
    if not HAY_WIN32:
        return True
    try:
        k32 = ctypes.windll.kernel32
        # El handle se guarda a nivel de modulo a proposito: si el
        # recolector lo cierra, Windows suelta el candado y deja entrar
        # a un segundo proceso.
        global _handle_candado
        _handle_candado = k32.CreateMutexW(None, True, CANDADO)
        return k32.GetLastError() != ERROR_YA_EXISTE
    except Exception:
        return True


def pedir_que_se_muestre():
    """Le dice a la instancia que ya estaba abierta que saque el panel.

    Se busca su buzon por el nombre de clase. Es una ventana sin pixeles
    (HWND_MESSAGE), que existe solo para recibir esto.
    """
    if not HAY_WIN32:
        return False
    try:
        user32 = ctypes.windll.user32
        user32.FindWindowExW.argtypes = [wintypes.HWND, wintypes.HWND,
                                         wintypes.LPCWSTR, wintypes.LPCWSTR]
        user32.FindWindowExW.restype = wintypes.HWND
        hwnd = user32.FindWindowExW(wintypes.HWND(HWND_MESSAGE), None,
                                    CLASE_BUZON, None)
        if not hwnd:
            return False
        return bool(user32.PostMessageW(hwnd, 0x0400 + 7, 0, 0))
    except Exception:
        return False


def abrir_buzon(al_pedir):
    """Crea el buzon que escucha a las instancias que lleguen despues.

    Una ventana solo-mensajes y no una difusion a todo el escritorio:
    PostMessage a HWND_BROADCAST se entrega a ventanas de verdad, no a
    colas de hilo, asi que un bucle suelto nunca lo recibiria.
    """
    if not HAY_WIN32:
        return

    def bucle():
        global _wndproc
        user32 = ctypes.windll.user32
        PROC = ctypes.WINFUNCTYPE(ctypes.c_longlong, wintypes.HWND,
                                  ctypes.c_uint, ctypes.c_size_t,
                                  ctypes.c_ssize_t)

        def proc(hwnd, msg, wp, lp):
            if msg == 0x0400 + 7:          # WM_USER + 7
                try:
                    al_pedir()
                except Exception:
                    pass
                return 0
            return user32.DefWindowProcW(hwnd, msg, wp, lp)

        _wndproc = PROC(proc)

        class WNDCLASS(ctypes.Structure):
            _fields_ = [("style", ctypes.c_uint), ("lpfnWndProc", PROC),
                        ("cbClsExtra", ctypes.c_int),
                        ("cbWndExtra", ctypes.c_int),
                        ("hInstance", wintypes.HANDLE),
                        ("hIcon", wintypes.HANDLE),
                        ("hCursor", wintypes.HANDLE),
                        ("hbrBackground", wintypes.HANDLE),
                        ("lpszMenuName", wintypes.LPCWSTR),
                        ("lpszClassName", wintypes.LPCWSTR)]

        wc = WNDCLASS()
        wc.lpfnWndProc = _wndproc
        wc.lpszClassName = CLASE_BUZON
        wc.hInstance = ctypes.windll.kernel32.GetModuleHandleW(None)
        if not user32.RegisterClassW(ctypes.byref(wc)):
            return

        # Hay que declarar los tipos: sin esto, ctypes intenta pasar el
        # -3 de HWND_MESSAGE como entero sin signo y revienta con
        # "int too long to convert", asi que el buzon no llegaba a
        # existir y la segunda instancia no encontraba a quien avisar.
        user32.CreateWindowExW.argtypes = [
            wintypes.DWORD, wintypes.LPCWSTR, wintypes.LPCWSTR,
            wintypes.DWORD, ctypes.c_int, ctypes.c_int, ctypes.c_int,
            ctypes.c_int, wintypes.HWND, wintypes.HMENU,
            wintypes.HINSTANCE, wintypes.LPVOID]
        user32.CreateWindowExW.restype = wintypes.HWND

        hwnd = user32.CreateWindowExW(
            0, CLASE_BUZON, CLASE_BUZON, 0, 0, 0, 0, 0,
            wintypes.HWND(HWND_MESSAGE), None, wc.hInstance, None)
        if not hwnd:
            return
        msg = wintypes.MSG()
        while True:
            try:
                r = user32.GetMessageW(ctypes.byref(msg), None, 0, 0)
                if r in (0, -1):
                    break
                user32.TranslateMessage(ctypes.byref(msg))
                user32.DispatchMessageW(ctypes.byref(msg))
            except Exception:
                time.sleep(0.2)

    threading.Thread(target=bucle, daemon=True, name="buzon").start()


# ------------------------------------------------------------ atajo global

MOD_ALT, MOD_CONTROL, MOD_SHIFT, MOD_WIN = 0x0001, 0x0002, 0x0004, 0x0008
MOD_NOREPEAT = 0x4000
WM_HOTKEY = 0x0312
WM_RECARGAR = 0x0400 + 1          # WM_USER + 1

_MODIFICADORES = {"ctrl": MOD_CONTROL, "control": MOD_CONTROL,
                  "alt": MOD_ALT, "shift": MOD_SHIFT, "win": MOD_WIN}
_TECLAS = {"space": 0x20, "espacio": 0x20, "enter": 0x0D, "return": 0x0D,
           "tab": 0x09, "esc": 0x1B, "escape": 0x1B, "insert": 0x2D}


def descomponer_atajo(texto):
    """'ctrl+shift+v' -> (MOD_CONTROL|MOD_SHIFT, 0x56). None si no vale.

    Exige al menos un modificador: Windows rechaza registrar una tecla
    suelta, y ademas se la quitaria al resto del sistema.
    """
    mods, vk = 0, None
    for parte in texto.lower().replace(" ", "").split("+"):
        if not parte:
            continue
        if parte in _MODIFICADORES:
            mods |= _MODIFICADORES[parte]
        elif parte in _TECLAS:
            vk = _TECLAS[parte]
        elif len(parte) == 1:
            vk = ord(parte.upper())
        else:
            return None
    return (mods, vk) if vk and mods else None


class AtajoGlobal:
    """Atajo global con RegisterHotKey, sin hook de teclado.

    La libreria keyboard instala un hook WH_KEYBOARD_LL, y Windows lo
    desengancha en silencio si el callback tarda mas que
    LowLevelHooksTimeout (300 ms por defecto). No avisa, no lanza, no
    deja rastro: el atajo respondia unas cuantas veces y despues moria.
    Se vio en errores.log, con las pulsaciones dejando de contarse de
    golpe mientras el resto de la aplicacion seguia sana.

    RegisterHotKey no usa hook. El sistema encola un WM_HOTKEY en el
    hilo que lo registro, y ese hilo tiene que bombear su cola. Por eso
    todo pasa aqui dentro, en un hilo propio: registrar y recibir tienen
    que ocurrir en el mismo.
    """

    ID = 1

    def __init__(self, al_pulsar, anotar=None):
        self._al_pulsar = al_pulsar
        self._anotar = anotar or (lambda *a: None)
        self._pendiente = None
        self._puesto = False
        self._id_hilo = None
        self._resultado = queue.Queue()
        self._listo = threading.Event()
        if HAY_WIN32:
            self._hilo = threading.Thread(target=self._bucle, daemon=True,
                                          name="atajo")
            self._hilo.start()
            self._listo.wait(3)

    def poner(self, combinacion):
        """Registra o cambia el atajo. True si Windows lo acepto."""
        partes = descomponer_atajo(combinacion)
        if not partes or not self._listo.is_set():
            return False
        self._pendiente = partes
        while not self._resultado.empty():       # sobras de un intento previo
            self._resultado.get_nowait()
        if not ctypes.windll.user32.PostThreadMessageW(
                self._id_hilo, WM_RECARGAR, 0, 0):
            return False
        try:
            return self._resultado.get(timeout=3)
        except queue.Empty:
            return False

    def soltar(self):
        if HAY_WIN32 and self._puesto:
            try:
                ctypes.windll.user32.UnregisterHotKey(None, self.ID)
            except Exception:
                pass
            self._puesto = False

    def _registrar(self):
        user32 = ctypes.windll.user32
        if self._puesto:
            user32.UnregisterHotKey(None, self.ID)
            self._puesto = False
        mods, vk = self._pendiente
        # MOD_NOREPEAT: sin esto, mantener la combinacion pulsada la
        # dispara en bucle y el panel parpadea.
        self._puesto = bool(user32.RegisterHotKey(
            None, self.ID, mods | MOD_NOREPEAT, vk))
        return self._puesto

    def _bucle(self):
        user32 = ctypes.windll.user32
        self._id_hilo = ctypes.windll.kernel32.GetCurrentThreadId()
        msg = wintypes.MSG()
        # Obliga a Windows a crear la cola de mensajes de este hilo antes
        # de avisar de que esta listo: un PostThreadMessage a un hilo sin
        # cola se pierde sin decir nada.
        user32.PeekMessageW(ctypes.byref(msg), None, 0, 0, 0)
        self._listo.set()
        while True:
            try:
                r = user32.GetMessageW(ctypes.byref(msg), None, 0, 0)
                if r in (0, -1):
                    break
                if msg.message == WM_HOTKEY:
                    self._al_pulsar()
                elif msg.message == WM_RECARGAR:
                    self._resultado.put(self._registrar())
            except Exception:
                self._anotar("fallo en el bucle del atajo", "AtajoGlobal")
                time.sleep(0.2)


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
