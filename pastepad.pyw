# -*- coding: utf-8 -*-
"""
Snippets  (v9)

Panel de portapapeles: historial automatico + textos guardados en carpetas.
Ctrl+Alt+V lo abre junto al puntero.

pip install customtkinter keyboard pyperclip pywin32
"""

import json
import os
import queue
import sys
import threading
import time
import tkinter as tk
import tkinter.font as tkfont
from tkinter import messagebox

import customtkinter as ctk
import keyboard
import pyperclip

try:
    import win32clipboard
    import win32con
    import win32api
    import win32gui
    import win32process
    import ctypes
    HAY_WIN32 = True
except Exception:
    HAY_WIN32 = False

try:
    import winreg
    HAY_REG = True
except Exception:
    HAY_REG = False


# ================================================================ constantes

ANCHO, ALTO = 360, 470
MAX_HIST = 80
MAX_CARACTERES = 200000
HOTKEY = "ctrl+alt+v"
APP = "Snippets"
CLAVE_RUN = r"Software\Microsoft\Windows\CurrentVersion\Run"
NOMBRE_RUN = "GestorSnippets"

# Color que Windows vuelve transparente: sirve para las esquinas redondeadas.
MAGICO = "#010203"

FUENTE_ICONOS = "Segoe Fluent Icons"
FUENTE_ICONOS_ALT = "Segoe MDL2 Assets"

IC = {"buscar": "\uE721", "mas": "\uE710", "carpeta": "\uE8F4",
      "lista": "\uE8FD", "pin": "\uE718", "unpin": "\uE77A",
      "editar": "\uE70F", "borrar": "\uE74D", "cerrar": "\uE711",
      "paleta": "\uE790", "escoba": "\uE75C", "check": "\uE73E",
      "borrar_carp": "\uED43",
      "imagen": "\uEB9F"}

RESPALDO = {"buscar": "?", "mas": "+", "carpeta": "[+]", "lista": "=",
            "pin": "^", "unpin": "v", "editar": "/", "borrar": "x",
            "cerrar": "X", "paleta": "@", "escoba": "~", "check": "v",
            "borrar_carp": "[x]",
            "imagen": "[]"}

ACENTOS = {"azul": ("#3B82F6", "#2563EB"), "verde": ("#22C55E", "#16A34A"),
           "lima": ("#A3D93B", "#8FBF14"), "naranja": ("#F97316", "#EA580C"),
           "rosa": ("#EC4899", "#DB2777"), "violeta": ("#8B5CF6", "#7C3AED")}

OSC = {"fondo": "#161616", "panel": "#1F1F1F", "tarjeta": "#242424",
       "hover": "#2E2E2E", "borde": "#2E2E2E", "texto": "#F0F0F0",
       "tenue": "#8E8E8E"}
CLA = {"fondo": "#F7F7F5", "panel": "#FFFFFF", "tarjeta": "#EFEFEC",
       "hover": "#E3E3DF", "borde": "#DCDCD8", "texto": "#1C1C1C",
       "tenue": "#6E6E6B"}

C = dict(OSC)
C["acento"], C["acento_h"] = ACENTOS["azul"]
C["sobre"] = "#FFFFFF"

FUENTE_DEF, TAM_DEF, COLOR_DEF = "Calibri", 11, "#000000"

ctk.set_appearance_mode("dark")


# ================================================================ archivos

def base():
    if getattr(sys, "frozen", False):
        return os.path.dirname(sys.executable)
    return os.path.dirname(os.path.abspath(__file__))


R_DATOS = os.path.join(base(), "snippets.json")
R_HIST = os.path.join(base(), "historial.json")
R_PREFS = os.path.join(base(), "config.json")
D_IMG = os.path.join(base(), "imagenes")


def _leer(ruta, defecto):
    try:
        with open(ruta, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception:
        return defecto


def _escribir(ruta, datos):
    try:
        with open(ruta, "w", encoding="utf-8") as f:
            json.dump(datos, f, ensure_ascii=False, indent=1)
    except Exception:
        pass


def run(t, f=FUENTE_DEF, s=TAM_DEF, b=0, i=0, u=0, c=COLOR_DEF):
    return {"t": t, "f": f, "s": s, "b": b, "i": i, "u": u, "c": c}


def cargar_datos():
    d = _leer(R_DATOS, {"categorias": [], "snippets": []})
    d.setdefault("categorias", [])
    d.setdefault("snippets", [])
    for s in d["snippets"]:
        if "runs" not in s:
            s["runs"] = [run(s.get("texto", ""))]
    return d


guardar_datos = lambda d: _escribir(R_DATOS, d)
cargar_hist = lambda: _leer(R_HIST, [])
guardar_hist = lambda l: _escribir(R_HIST, l)
cargar_prefs = lambda: _leer(R_PREFS, {"acento": "azul"})
guardar_prefs = lambda p: _escribir(R_PREFS, p)


def plano(runs):
    return "".join(r["t"] for r in runs)


def una_linea(texto, tope=52):
    # Cortar primero: con textos de miles de lineas, partir todo en
    # palabras solo para mostrar 50 caracteres cuesta mucho.
    crudo = texto[:tope * 4]
    t = " ".join(crudo.split())
    if len(texto) > len(crudo) or len(t) > tope:
        return t[:tope] + "..."
    return t


ACENTOS_MAP = str.maketrans("áéíóúüñÁÉÍÓÚÜÑ", "aeiouunAEIOUUN")


def normalizar(texto):
    """Minusculas y sin tildes, para que 'informacion' encuentre
    'informacion' y tambien 'informacion' escrito con tilde."""
    return texto[:4000].lower().translate(ACENTOS_MAP)


def puntuar(consulta, titulo_n, cuerpo_n):
    """Devuelve cuanto se parece, o None si no coincide.

    Cada palabra de la consulta tiene que estar en algun lado. Vale mas
    si aparece en el titulo, si empieza una palabra, y si esta al
    principio del texto.
    """
    total = 0
    junto = titulo_n + " " + cuerpo_n
    for palabra in consulta:
        pos_t = titulo_n.find(palabra)
        pos_c = cuerpo_n.find(palabra)
        if pos_t < 0 and pos_c < 0:
            return None
        if pos_t >= 0:
            total += 100
            # empieza una palabra del titulo, no va pegada en el medio
            if pos_t == 0 or titulo_n[pos_t - 1] in " -_.:,/()":
                total += 60
            if titulo_n[pos_t:pos_t + len(palabra) + 1].rstrip() == palabra:
                total += 30
            total += max(0, 25 - pos_t // 2)
        else:
            total += 30
            if pos_c == 0 or cuerpo_n[pos_c - 1] in " -_.:,/()\n":
                total += 20
            total += max(0, 15 - pos_c // 40)
    # todas las palabras seguidas, tal cual las escribio
    if len(consulta) > 1 and " ".join(consulta) in junto:
        total += 80
    return total


def coincide(q, *textos):
    if not q:
        return True
    junto = " ".join(normalizar(t) for t in textos if t)
    return all(p in junto for p in normalizar(q).split())


def campos_de(texto):
    campos, resto = [], texto
    while "[[" in resto and "]]" in resto:
        i = resto.index("[[")
        j = resto.index("]]", i)
        n = resto[i + 2:j].strip()
        if n and n not in campos:
            campos.append(n)
        resto = resto[j + 2:]
    return campos


def rellenar(runs, valores):
    out = []
    for r in runs:
        t = r["t"]
        for k, v in valores.items():
            t = t.replace("[[%s]]" % k, v)
        r2 = dict(r)
        r2["t"] = t
        out.append(r2)
    return out


# ================================================================ tema

def windows_claro():
    if not HAY_REG:
        return False
    try:
        k = winreg.OpenKey(
            winreg.HKEY_CURRENT_USER,
            r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")
        try:
            v, _ = winreg.QueryValueEx(k, "AppsUseLightTheme")
            return bool(v)
        finally:
            winreg.CloseKey(k)
    except Exception:
        return False


def aplicar_tema(acento=None):
    claro = windows_claro()
    C.update(CLA if claro else OSC)
    if acento is None:
        acento = cargar_prefs().get("acento", "azul")
    C["acento"], C["acento_h"] = ACENTOS.get(acento, ACENTOS["azul"])
    C["sobre"] = "#FFFFFF"
    ctk.set_appearance_mode("light" if claro else "dark")
    return claro


_fam_cache = [None, False]


def fam_iconos():
    if not _fam_cache[1]:
        d = set(tkfont.families())
        _fam_cache[0] = (FUENTE_ICONOS if FUENTE_ICONOS in d
                         else FUENTE_ICONOS_ALT if FUENTE_ICONOS_ALT in d
                         else None)
        _fam_cache[1] = True
    return _fam_cache[0]


def simbolo(clave):
    return IC[clave] if fam_iconos() else RESPALDO[clave]


# ================================================================ portapapeles

def _esc(s):
    out = []
    for ch in s:
        if ch in "\\{}":
            out.append("\\" + ch)
        elif ch == "\n":
            out.append("\\par\n")
        elif ch == "\t":
            out.append("\\tab ")
        elif ord(ch) < 128:
            out.append(ch)
        else:
            out.append("\\u%d?" % ord(ch))
    return "".join(out)


def a_rtf(runs):
    fs, cs = [], []
    for r in runs:
        if r["f"] not in fs:
            fs.append(r["f"])
        if r["c"].upper() not in cs:
            cs.append(r["c"].upper())
    tf = "".join("{\\f%d\\fnil %s;}" % (i, f) for i, f in enumerate(fs))
    tc = ";".join("\\red%d\\green%d\\blue%d"
                  % (int(c[1:3], 16), int(c[3:5], 16), int(c[5:7], 16))
                  for c in cs)
    cu = ["\\pard\\plain "]
    for r in runs:
        cu.append("\\f%d\\fs%d\\cf%d" % (fs.index(r["f"]), int(r["s"] * 2),
                                         cs.index(r["c"].upper()) + 1))
        for m, k in (("\\b", "b"), ("\\i", "i"), ("\\ul", "u")):
            if r[k]:
                cu.append(m)
        cu.append(" " + _esc(r["t"]))
        for m, k in (("\\ulnone", "u"), ("\\i0", "i"), ("\\b0", "b")):
            if r[k]:
                cu.append(m)
    return "{\\rtf1\\ansi\\deff0{\\fonttbl%s}{\\colortbl;%s;}%s}" % (
        tf, tc, "".join(cu))


def _cb(accion, intentos=4):
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


def copiar_runs(runs, sin_formato=False):
    txt = plano(runs)
    if sin_formato or not HAY_WIN32:
        pyperclip.copy(txt)
        return
    rtf = a_rtf(runs).encode("ascii", "replace")

    def hacer():
        win32clipboard.EmptyClipboard()
        win32clipboard.SetClipboardData(win32con.CF_UNICODETEXT, txt)
        f = win32clipboard.RegisterClipboardFormat("Rich Text Format")
        win32clipboard.SetClipboardData(f, rtf)
        return True
    if not _cb(hacer):
        pyperclip.copy(txt)


def leer_portapapeles():
    if not HAY_WIN32:
        try:
            return "texto", pyperclip.paste()
        except Exception:
            return None, None

    def hacer():
        if win32clipboard.IsClipboardFormatAvailable(win32con.CF_UNICODETEXT):
            return "texto", win32clipboard.GetClipboardData(
                win32con.CF_UNICODETEXT)
        if win32clipboard.IsClipboardFormatAvailable(win32con.CF_DIB):
            return "imagen", win32clipboard.GetClipboardData(win32con.CF_DIB)
        return None, None
    return _cb(hacer, 2) or (None, None)


def dib_a_bmp(dib):
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
    return bool(_cb(hacer))


def autoarranque(activar=True):
    if not HAY_REG:
        return
    ruta = sys.executable if getattr(sys, "frozen", False) \
        else os.path.abspath(__file__)
    try:
        k = winreg.OpenKey(winreg.HKEY_CURRENT_USER, CLAVE_RUN, 0,
                           winreg.KEY_SET_VALUE)
        try:
            if activar:
                winreg.SetValueEx(k, NOMBRE_RUN, 0, winreg.REG_SZ, '"%s"' % ruta)
            else:
                try:
                    winreg.DeleteValue(k, NOMBRE_RUN)
                except FileNotFoundError:
                    pass
        finally:
            winreg.CloseKey(k)
    except Exception:
        pass


# ================================================================ foco

_MI_PID = os.getpid()


def ventana_activa():
    """Handle de la ventana con el foco, salvo que sea la nuestra."""
    if not HAY_WIN32:
        return None
    try:
        hwnd = win32gui.GetForegroundWindow()
        if not hwnd:
            return None
        _, pid = win32process.GetWindowThreadProcessId(hwnd)
        if pid == _MI_PID:
            return None
        return hwnd
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


# ================================================================ piezas

def ico(padre, clave, comando, tam=14, lado=26, color=None):
    return ctk.CTkButton(padre, text=simbolo(clave), width=lado, height=lado,
                         corner_radius=7, fg_color="transparent",
                         hover_color=C["hover"], text_color=color or C["tenue"],
                         font=ctk.CTkFont(family=fam_iconos() or None, size=tam),
                         command=comando)


def btn(padre, texto, comando, ancho=80, alto=30, acento=False, tam=12):
    return ctk.CTkButton(
        padre, text=texto, command=comando, width=ancho, height=alto,
        corner_radius=8,
        fg_color=C["acento"] if acento else C["tarjeta"],
        hover_color=C["acento_h"] if acento else C["hover"],
        text_color=C["sobre"] if acento else C["texto"],
        border_width=0 if acento else 1, border_color=C["borde"],
        font=ctk.CTkFont(size=tam))


class ListaCanvas(tk.Frame):
    """Lista dibujada en un lienzo.

    CustomTkinter se arrastra cuando hay decenas de widgets: cada fila
    serian ~8 widgets que hay que destruir y recrear en cada tecla.
    Aqui todo es un solo widget y las filas son rectangulos y texto,
    asi que repintar 500 elementos es instantaneo.
    """

    FILA = 44
    PAD = 3

    def __init__(self, padre, al_click, al_pegar, al_accion):
        super().__init__(padre, bg=C["fondo"], highlightthickness=0)
        self.al_click = al_click
        self.al_pegar = al_pegar
        self.al_accion = al_accion
        self.items = []
        self.sel = 0
        self.desliz = 0
        self.zonas = []

        self.cv = tk.Canvas(self, bg=C["fondo"], highlightthickness=0, bd=0)
        self.cv.pack(side="left", fill="both", expand=True)
        self.barra = tk.Canvas(self, bg=C["fondo"], highlightthickness=0,
                               bd=0, width=5)
        self.barra.pack(side="right", fill="y")

        self.cv.bind("<Configure>", lambda e: self.pintar())
        self.cv.bind("<MouseWheel>", self._rueda)
        self.cv.bind("<Button-1>", self._click)
        self.cv.bind("<Double-Button-1>", self._doble)
        self.cv.bind("<Motion>", self._mover)
        self.cv.bind("<Leave>", lambda e: self._hover(None))
        self.encima = None

        self.f_tit = tkfont.Font(family="Segoe UI", size=9)
        self.f_sub = tkfont.Font(family="Segoe UI", size=8)
        self.f_ico = tkfont.Font(family=fam_iconos() or "Segoe UI", size=9)

    # ---------------- datos

    def cargar(self, items):
        """items: lista de (dato, tipo). tipo 'h' historial, 'g' guardado."""
        self.items = items
        self.sel = 0
        self.desliz = 0
        self.pintar()

    def alto_total(self):
        return len(self.items) * (self.FILA + self.PAD) + self.PAD

    def visible(self):
        return max(1, self.cv.winfo_height())

    # ---------------- dibujo

    def pintar(self):
        cv = self.cv
        cv.delete("all")
        self.zonas = []
        ancho = cv.winfo_width()
        if ancho <= 1:
            return

        if not self.items:
            cv.create_text(ancho // 2, 40, text=self.vacio_msg(),
                           fill=C["tenue"], font=self.f_sub, width=ancho - 40)
            self._barra()
            return

        alto_v = self.visible()
        primera = max(0, int(self.desliz // (self.FILA + self.PAD)) - 1)
        ultima = min(len(self.items),
                     primera + int(alto_v // (self.FILA + self.PAD)) + 3)

        for i in range(primera, ultima):
            dato, tipo = self.items[i]
            y = self.PAD + i * (self.FILA + self.PAD) - self.desliz
            self._fila(i, dato, tipo, y, ancho)
        self._barra()

    def vacio_msg(self):
        return getattr(self, "_msg", "Sin resultados")

    def mensaje(self, texto):
        self._msg = texto

    def _fila(self, i, dato, tipo, y, ancho):
        cv = self.cv
        activa = (i == self.sel)
        hover = (self.encima == i)
        fondo = C["acento"] if activa else (C["hover"] if hover else C["tarjeta"])
        col_t = C["sobre"] if activa else C["texto"]
        col_s = C["sobre"] if activa else C["tenue"]

        self._redondo(4, y, ancho - 4, y + self.FILA, 8, fondo)

        es_h = (tipo == "h")
        if es_h:
            if dato["tipo"] == "imagen":
                titulo, sub = "Imagen copiada", "captura"
            else:
                titulo = una_linea(dato.get("texto", ""), 40) or "(vacio)"
                sub = "%d caracteres" % len(dato.get("texto", ""))
            fijado = bool(dato.get("pin"))
        else:
            titulo = una_linea(dato["titulo"], 40)
            sub = dato["categoria"]
            fijado = False

        cv.create_text(16, y + 13, text=titulo, anchor="w", fill=col_t,
                       font=self.f_tit)
        cv.create_text(16, y + 30, text=sub, anchor="w", fill=col_s,
                       font=self.f_sub)

        x = ancho - 22
        iconos = [("borrar", "borrar")]
        if (not es_h) or dato["tipo"] == "texto":
            iconos.insert(0, ("editar", "editar"))
        if es_h:
            iconos.insert(0, ("unpin" if fijado else "pin", "pin"))
        for clave, accion in reversed(iconos):
            color = C["acento"] if (clave == "unpin" and not activa) else col_s
            cv.create_text(x, y + self.FILA // 2, text=simbolo(clave),
                           fill=color, font=self.f_ico)
            self.zonas.append((x - 10, y, x + 10, y + self.FILA, accion, i))
            x -= 24

    def _redondo(self, x1, y1, x2, y2, r, color):
        cv = self.cv
        cv.create_rectangle(x1 + r, y1, x2 - r, y2, fill=color, outline=color)
        cv.create_rectangle(x1, y1 + r, x2, y2 - r, fill=color, outline=color)
        for cx, cy, ini in ((x1 + r, y1 + r, 90), (x2 - r, y1 + r, 0),
                            (x1 + r, y2 - r, 180), (x2 - r, y2 - r, 270)):
            cv.create_arc(cx - r, cy - r, cx + r, cy + r, start=ini, extent=90,
                          fill=color, outline=color)

    def _barra(self):
        b = self.barra
        b.delete("all")
        total = self.alto_total()
        alto_v = self.visible()
        if total <= alto_v:
            return
        largo = max(24, int(alto_v * alto_v / total))
        pos = int((alto_v - largo) * self.desliz / max(1, total - alto_v))
        b.create_rectangle(1, pos, 4, pos + largo, fill=C["hover"],
                           outline=C["hover"])

    # ---------------- interaccion

    def _indice(self, ey):
        i = int((ey + self.desliz - self.PAD) // (self.FILA + self.PAD))
        return i if 0 <= i < len(self.items) else None

    def _click(self, e):
        # Los iconos de la derecha mandan sobre el resto de la fila.
        for x1, y1, x2, y2, accion, i in self.zonas:
            if x1 <= e.x <= x2 and y1 <= e.y <= y2:
                self.al_accion(accion, self.items[i][0])
                return
        i = self._indice(e.y)
        if i is not None:
            self.sel = i
            self.al_click(i)
            self.al_pegar()

    def _doble(self, e):
        # El primer clic ya pego: el segundo no debe repetirlo.
        return "break"

    def _mover(self, e):
        self._hover(self._indice(e.y))

    def _hover(self, i):
        if i != self.encima:
            self.encima = i
            self.pintar()

    def _rueda(self, e):
        self.mover_scroll(-1 if e.delta > 0 else 1)
        return "break"

    def mover_scroll(self, pasos):
        tope = max(0, self.alto_total() - self.visible())
        self.desliz = max(0, min(tope, self.desliz + pasos * 48))
        self.pintar()

    def seleccionar(self, i):
        if not self.items:
            return
        self.sel = max(0, min(len(self.items) - 1, i))
        y = self.PAD + self.sel * (self.FILA + self.PAD)
        if y < self.desliz:
            self.desliz = y
        elif y + self.FILA > self.desliz + self.visible():
            self.desliz = y + self.FILA - self.visible()
        self.pintar()

    def repintar_colores(self):
        self.configure(bg=C["fondo"])
        self.cv.configure(bg=C["fondo"])
        self.barra.configure(bg=C["fondo"])
        self.f_ico.configure(family=fam_iconos() or "Segoe UI")
        self.pintar()


class Ventana(ctk.CTkToplevel):
    """Base de los dialogos: estilo comun y atajos de portapapeles."""

    def __init__(self, master, titulo, ancho, alto):
        super().__init__(master, fg_color=C["fondo"])
        self.resultado = None
        self.title(titulo)
        self.geometry("%dx%d" % (ancho, alto))
        self.minsize(ancho, alto)
        self.attributes("-topmost", True)
        self.after(130, self._tomar)
        self.bind("<Escape>", lambda e: self.destroy())

    def _tomar(self):
        try:
            self.grab_set()
        except Exception:
            pass

    def atajos(self, w):
        for sec, fn in (("v", self._peg), ("c", self._cop),
                        ("x", self._cor), ("a", self._tod)):
            w.bind("<Control-%s>" % sec, fn)
            w.bind("<Control-%s>" % sec.upper(), fn)

    def _peg(self, e):
        try:
            texto = pyperclip.paste()
        except Exception:
            return "break"
        w = e.widget
        try:
            try:
                w.delete("sel.first", "sel.last")
            except Exception:
                pass
            w.insert("insert", texto if isinstance(w, tk.Text)
                     else texto.replace("\n", " "))
        except Exception:
            pass
        return "break"

    def _cop(self, e):
        try:
            w = e.widget
            pyperclip.copy(w.get("sel.first", "sel.last")
                           if isinstance(w, tk.Text) else w.selection_get())
        except Exception:
            pass
        return "break"

    def _cor(self, e):
        self._cop(e)
        try:
            e.widget.delete("sel.first", "sel.last")
        except Exception:
            pass
        return "break"

    def _tod(self, e):
        w = e.widget
        try:
            if isinstance(w, tk.Text):
                w.tag_add("sel", "1.0", "end-1c")
            else:
                w.select_range(0, "end")
        except Exception:
            pass
        return "break"

    def caja(self, padre):
        marco = ctk.CTkFrame(padre, fg_color=C["panel"], corner_radius=10)
        t = tk.Text(marco, wrap="word", undo=True, bd=0, bg=C["panel"],
                    fg=C["texto"], insertbackground=C["acento"],
                    selectbackground=C["acento"], selectforeground=C["sobre"],
                    font=("Segoe UI", 10), padx=11, pady=9, highlightthickness=0)
        t.pack(fill="both", expand=True, padx=4, pady=4)
        self.atajos(t)
        return marco, t


# ================================================================ dialogos

class DlgTexto(Ventana):
    def __init__(self, master, categorias, snippet=None, texto_inicial=""):
        super().__init__(master, "Nuevo texto", 560, 440)

        # El pie va primero para que nunca lo empuje el area de texto.
        pie = ctk.CTkFrame(self, fg_color="transparent", height=54)
        pie.pack(side="bottom", fill="x", padx=16, pady=(6, 14))
        pie.pack_propagate(False)
        btn(pie, "Guardar", self.guardar, 108, 34, True, 13).pack(side="right")
        btn(pie, "Cancelar", self.destroy, 92, 34).pack(side="right", padx=8)
        ctk.CTkLabel(pie, text="Ctrl+Enter guarda", text_color=C["tenue"],
                     font=ctk.CTkFont(size=10)).pack(side="left")

        cab = ctk.CTkFrame(self, fg_color="transparent")
        cab.pack(fill="x", padx=16, pady=(14, 8))
        ctk.CTkLabel(cab, text="Guardar en", text_color=C["tenue"],
                     font=ctk.CTkFont(size=11)).pack(side="left")
        opciones = list(categorias) or ["Mis textos"]
        self.cb = ctk.CTkComboBox(cab, values=opciones, width=185, height=30,
                                  corner_radius=8, fg_color=C["tarjeta"],
                                  border_color=C["borde"],
                                  button_color=C["tarjeta"],
                                  button_hover_color=C["hover"],
                                  text_color=C["texto"])
        self.cb.pack(side="left", padx=8)
        self.cb.set(snippet["categoria"] if snippet else opciones[0])
        ctk.CTkLabel(cab, text="[[campo]] se pregunta al pegar",
                     text_color=C["tenue"], font=ctk.CTkFont(size=10)
                     ).pack(side="left", padx=6)

        marco, self.txt = self.caja(self)
        marco.pack(fill="both", expand=True, padx=16, pady=(0, 4))
        if snippet:
            for r in snippet["runs"]:
                self.txt.insert("end", r["t"])
        elif texto_inicial:
            self.txt.insert("1.0", texto_inicial)

        self.bind("<Control-Return>", lambda e: self.guardar())
        self.after(220, self.txt.focus_set)

    def guardar(self):
        texto = self.txt.get("1.0", "end-1c")
        if not texto.strip():
            self.destroy()
            return
        primera = texto.strip().splitlines()[0]
        self.resultado = {"titulo": una_linea(primera, 48),
                          "categoria": self.cb.get().strip() or "Mis textos",
                          "runs": [run(texto)]}
        self.destroy()


class DlgLinea(Ventana):
    def __init__(self, master, titulo, etiqueta):
        super().__init__(master, titulo, 340, 160)
        pie = ctk.CTkFrame(self, fg_color="transparent")
        pie.pack(side="bottom", fill="x", padx=16, pady=14)
        btn(pie, "Crear", self.ok, 88, 32, True).pack(side="right")
        btn(pie, "Cancelar", self.destroy, 88, 32).pack(side="right", padx=8)
        ctk.CTkLabel(self, text=etiqueta, text_color=C["tenue"],
                     font=ctk.CTkFont(size=11)).pack(anchor="w", padx=16,
                                                     pady=(20, 6))
        self.e = ctk.CTkEntry(self, height=32, corner_radius=8,
                              fg_color=C["tarjeta"], border_color=C["borde"],
                              text_color=C["texto"])
        self.e.pack(fill="x", padx=16)
        self.atajos(self.e)
        self.bind("<Return>", lambda e: self.ok())
        self.after(220, self.e.focus_set)

    def ok(self):
        self.resultado = self.e.get().strip()
        self.destroy()


class DlgCampos(Ventana):
    def __init__(self, master, campos):
        super().__init__(master, "Completar", 360, 96 + 58 * len(campos))
        pie = ctk.CTkFrame(self, fg_color="transparent")
        pie.pack(side="bottom", fill="x", padx=18, pady=14)
        btn(pie, "Pegar", self.ok, 88, 32, True).pack(side="right")
        btn(pie, "Cancelar", self.destroy, 88, 32).pack(side="right", padx=8)
        ctk.CTkLabel(self, text="Completar antes de pegar",
                     text_color=C["texto"], font=ctk.CTkFont(size=13)
                     ).pack(anchor="w", padx=18, pady=(14, 8))
        self.entradas = {}
        for c in campos:
            ctk.CTkLabel(self, text=c, text_color=C["tenue"],
                         font=ctk.CTkFont(size=11)).pack(anchor="w", padx=18)
            e = ctk.CTkEntry(self, height=30, corner_radius=8,
                             fg_color=C["tarjeta"], border_color=C["borde"],
                             text_color=C["texto"])
            e.pack(fill="x", padx=18, pady=(2, 8))
            self.atajos(e)
            self.entradas[c] = e
        self.bind("<Return>", lambda e: self.ok())
        vs = list(self.entradas.values())
        if vs:
            self.after(220, vs[0].focus_set)

    def ok(self):
        self.resultado = {k: e.get() for k, e in self.entradas.items()}
        self.destroy()


class DlgLista(Ventana):
    """Pega varias cosas: separadas por linea o todo en una sola nota."""

    def __init__(self, master, carpeta):
        super().__init__(master, "Agregar a " + carpeta, 540, 470)

        pie = ctk.CTkFrame(self, fg_color="transparent", height=54)
        pie.pack(side="bottom", fill="x", padx=16, pady=(6, 14))
        pie.pack_propagate(False)
        btn(pie, "Agregar", self.ok, 100, 34, True, 13).pack(side="right")
        btn(pie, "Cancelar", self.destroy, 92, 34).pack(side="right", padx=8)
        self.lbl = ctk.CTkLabel(pie, text="", text_color=C["tenue"],
                                font=ctk.CTkFont(size=11))
        self.lbl.pack(side="left")

        opciones = ctk.CTkFrame(self, fg_color=C["panel"], corner_radius=10)
        opciones.pack(side="bottom", fill="x", padx=16, pady=(6, 0))
        self.modo = ctk.StringVar(value="separado")
        ctk.CTkRadioButton(opciones, text="Una nota por cada linea",
                           variable=self.modo, value="separado",
                           radiobutton_width=16, radiobutton_height=16,
                           fg_color=C["acento"], hover_color=C["acento_h"],
                           text_color=C["texto"], font=ctk.CTkFont(size=11),
                           command=self._contar).pack(anchor="w", padx=12,
                                                      pady=(10, 4))
        ctk.CTkRadioButton(opciones, text="Todo junto en una sola nota",
                           variable=self.modo, value="junto",
                           radiobutton_width=16, radiobutton_height=16,
                           fg_color=C["acento"], hover_color=C["acento_h"],
                           text_color=C["texto"], font=ctk.CTkFont(size=11),
                           command=self._contar).pack(anchor="w", padx=12,
                                                      pady=(0, 6))
        self.var_limpiar = ctk.BooleanVar(value=True)
        ctk.CTkCheckBox(opciones, text="Quitar numeracion y vinetas (1.  -  *)",
                        variable=self.var_limpiar, checkbox_width=16,
                        checkbox_height=16, corner_radius=4,
                        fg_color=C["acento"], hover_color=C["acento_h"],
                        text_color=C["tenue"], font=ctk.CTkFont(size=11),
                        command=self._contar).pack(anchor="w", padx=12,
                                                   pady=(0, 10))

        ctk.CTkLabel(self, text="Pega aqui tu lista", text_color=C["texto"],
                     font=ctk.CTkFont(size=13)).pack(anchor="w", padx=16,
                                                     pady=(14, 6))
        marco, self.txt = self.caja(self)
        marco.pack(fill="both", expand=True, padx=16)
        self.txt.bind("<KeyRelease>", lambda e: self._contar())
        self.after(220, self.txt.focus_set)
        self._contar()

    @staticmethod
    def _limpiar(t):
        for m in ("- ", "* ", "\u2022 ", "\u2013 "):
            if t.startswith(m):
                return t[len(m):].strip()
        i = 0
        while i < len(t) and t[i].isdigit():
            i += 1
        if i and i < len(t) and t[i] in ".)-":
            return t[i + 1:].strip()
        return t

    def lineas(self):
        out = []
        for l in self.txt.get("1.0", "end-1c").splitlines():
            t = l.strip()
            if not t:
                continue
            if self.var_limpiar.get():
                t = self._limpiar(t)
            if t:
                out.append(t)
        return out

    def _contar(self):
        n = len(self.lineas())
        if self.modo.get() == "junto":
            self.lbl.configure(text="Se creara 1 nota con %d lineas" % n)
        else:
            self.lbl.configure(text="Se crearan %d nota%s"
                                    % (n, "" if n == 1 else "s"))

    def ok(self):
        l = self.lineas()
        if not l:
            self.destroy()
            return
        self.resultado = ["\n".join(l)] if self.modo.get() == "junto" else l
        self.destroy()


class DlgColores(Ventana):
    def __init__(self, master):
        super().__init__(master, "Color", 300, 175)
        self.acento = cargar_prefs().get("acento", "azul")
        pie = ctk.CTkFrame(self, fg_color="transparent")
        pie.pack(side="bottom", fill="x", padx=18, pady=16)
        btn(pie, "Aplicar", self.ok, 92, 32, True).pack(side="right")
        btn(pie, "Cancelar", self.destroy, 88, 32).pack(side="right", padx=8)
        ctk.CTkLabel(self, text="Color de acento", text_color=C["texto"],
                     font=ctk.CTkFont(size=13)).pack(anchor="w", padx=18,
                                                     pady=(18, 12))
        fila = ctk.CTkFrame(self, fg_color="transparent")
        fila.pack(padx=18, anchor="w")
        self.bolas = {}
        for nombre, (col, _) in ACENTOS.items():
            b = ctk.CTkButton(
                fila, text=simbolo("check") if nombre == self.acento else "",
                width=32, height=32, corner_radius=16, fg_color=col,
                hover_color=col, text_color="#FFFFFF",
                font=ctk.CTkFont(family=fam_iconos() or None, size=13),
                command=lambda n=nombre: self._elegir(n))
            b.pack(side="left", padx=4)
            self.bolas[nombre] = b

    def _elegir(self, nombre):
        self.acento = nombre
        for n, b in self.bolas.items():
            b.configure(text=simbolo("check") if n == nombre else "")

    def ok(self):
        self.resultado = self.acento
        self.destroy()


# ================================================================ panel

class Panel(ctk.CTk):
    def __init__(self):
        super().__init__(fg_color=MAGICO)
        self.overrideredirect(True)
        self.geometry("%dx%d" % (ANCHO, ALTO))
        self.attributes("-topmost", True)
        try:
            # Vuelve transparente el color de fondo: deja las esquinas curvas.
            self.attributes("-transparentcolor", MAGICO)
        except Exception:
            self.configure(fg_color=C["fondo"])

        self.datos = cargar_datos()
        self.hist = cargar_hist()
        self.pestana = "reciente"
        self.categoria = None
        self.ultimo_cb = None
        self.visibles, self.tipos = [], []
        self.sel = 0
        self._pendiente = None
        self._idx = None
        self.destino = None
        self.ocupado = False
        self._claro = windows_claro()
        self.cola = queue.Queue()

        self._construir()
        self._pintar_carpetas()
        self._pintar_lista()

        self.bind("<Escape>", lambda e: self.ocultar())
        self.bind("<FocusOut>", lambda e: self.after(170, self._chequear))
        self._hotkey()
        self.after(120, self._cola)
        self.after(900, self._vigilar)
        self.after(5000, self._tema)

    # ------------------------------------------------ construccion

    def _construir(self):
        self.marco = ctk.CTkFrame(self, fg_color=C["fondo"], corner_radius=12,
                                  border_width=1, border_color=C["borde"])
        self.marco.pack(fill="both", expand=True, padx=1, pady=1)

        # --- pie primero, para que nunca lo empuje la lista
        pie = ctk.CTkFrame(self.marco, fg_color="transparent", height=42)
        pie.pack(side="bottom", fill="x", padx=10, pady=(0, 9))
        pie.pack_propagate(False)
        ctk.CTkButton(pie, text=" %s  Nuevo" % simbolo("mas"), width=92,
                      height=30, corner_radius=8, fg_color=C["acento"],
                      hover_color=C["acento_h"], text_color=C["sobre"],
                      font=ctk.CTkFont(family=fam_iconos() or None, size=12),
                      command=self.nuevo).pack(side="right")
        self.b_carpeta = ico(pie, "carpeta", self.nueva_carpeta, 15, 28)
        self.b_carpeta.pack(side="left")
        self.b_lista = ico(pie, "lista", self.pegar_lista, 15, 28)
        self.b_lista.pack(side="left", padx=4)
        self.b_borrar_carp = ico(pie, "borrar_carp",
                                 lambda: self.borrar_carpeta(self.categoria),
                                 15, 28)
        self.b_escoba = ico(pie, "escoba", self.limpiar, 15, 28)
        self.b_escoba.pack(side="left", padx=4)

        # --- cabecera
        cab = ctk.CTkFrame(self.marco, fg_color="transparent", height=28)
        cab.pack(fill="x", padx=11, pady=(8, 2))
        cab.pack_propagate(False)
        tit = ctk.CTkLabel(cab, text="Portapapeles", text_color=C["tenue"],
                           font=ctk.CTkFont(size=11))
        tit.pack(side="left")
        ico(cab, "cerrar", self.ocultar, 12, 24).pack(side="right")
        ico(cab, "paleta", self.colores, 14, 24).pack(side="right", padx=2)
        for w in (cab, tit):
            w.bind("<Button-1>", self._agarrar)
            w.bind("<B1-Motion>", self._mover_ventana)

        # --- buscador
        caja = ctk.CTkFrame(self.marco, fg_color=C["tarjeta"], corner_radius=9,
                            height=34)
        caja.pack(fill="x", padx=11, pady=(4, 7))
        caja.pack_propagate(False)
        ctk.CTkLabel(caja, text=simbolo("buscar"), width=16,
                     text_color=C["tenue"],
                     font=ctk.CTkFont(family=fam_iconos() or None, size=13)
                     ).pack(side="left", padx=(10, 0))
        self.e_buscar = ctk.CTkEntry(caja, height=28, border_width=0,
                                     fg_color=C["tarjeta"],
                                     text_color=C["texto"],
                                     placeholder_text="Buscar en todo",
                                     font=ctk.CTkFont(size=12))
        self.e_buscar.pack(side="left", fill="x", expand=True, padx=(4, 10))
        self.e_buscar.bind("<KeyRelease>", self._tecla)
        self.e_buscar.bind("<Return>", lambda e: self.pegar())
        self.e_buscar.bind("<Control-Return>", lambda e: self.pegar(True))
        self.e_buscar.bind("<Down>", lambda e: self._saltar(1))
        self.e_buscar.bind("<Up>", lambda e: self._saltar(-1))

        # --- pestanas
        tabs = ctk.CTkFrame(self.marco, fg_color=C["tarjeta"], corner_radius=9,
                            height=32)
        tabs.pack(fill="x", padx=11, pady=(0, 6))
        tabs.pack_propagate(False)
        self.tab_r = ctk.CTkButton(
            tabs, text="Reciente", height=26, corner_radius=7,
            fg_color=C["acento"], hover_color=C["acento_h"],
            text_color=C["sobre"], font=ctk.CTkFont(size=11),
            command=lambda: self.cambiar("reciente"))
        self.tab_r.pack(side="left", expand=True, fill="x", padx=3, pady=3)
        self.tab_g = ctk.CTkButton(
            tabs, text="Guardados", height=26, corner_radius=7,
            fg_color="transparent", hover_color=C["hover"],
            text_color=C["texto"], font=ctk.CTkFont(size=11),
            command=lambda: self.cambiar("guardados"))
        self.tab_g.pack(side="left", expand=True, fill="x", padx=3, pady=3)

        # --- barra de carpetas: altura fija, siempre presente
        self.carp = ctk.CTkFrame(self.marco, fg_color="transparent", height=0)
        self.carp.pack(fill="x", padx=9)

        self.lista = ListaCanvas(self.marco, self._click_fila, self.pegar,
                                 self._accion)
        self.lista.pack(fill="both", expand=True, padx=7, pady=(2, 4))

    def _agarrar(self, e):
        self._dx = e.x_root - self.winfo_x()
        self._dy = e.y_root - self.winfo_y()

    def _mover_ventana(self, e):
        self.geometry("+%d+%d" % (e.x_root - self._dx, e.y_root - self._dy))

    # ------------------------------------------------ pestanas

    def cambiar(self, cual):
        self.pestana = cual
        rec = cual == "reciente"
        self.tab_r.configure(fg_color=C["acento"] if rec else "transparent",
                             text_color=C["sobre"] if rec else C["texto"])
        self.tab_g.configure(fg_color="transparent" if rec else C["acento"],
                             text_color=C["texto"] if rec else C["sobre"])
        # Solo cambia la altura: no se reordena nada, no hay salto.
        self.carp.configure(height=0 if rec else 30)
        self._ajustar_pie()
        self._pintar_lista()
        self.e_buscar.focus_set()

    def _pintar_carpetas(self):
        for w in self.carp.winfo_children():
            w.destroy()
        for cat in [None] + list(self.datos["categorias"]):
            act = cat == self.categoria
            b = ctk.CTkButton(
                self.carp, text="Todas" if cat is None else cat,
                height=22, width=10, corner_radius=11,
                fg_color=C["acento"] if act else C["tarjeta"],
                hover_color=C["acento_h"] if act else C["hover"],
                text_color=C["sobre"] if act else C["texto"],
                font=ctk.CTkFont(size=11),
                command=lambda c=cat: self._carpeta(c))
            b.pack(side="left", padx=3, pady=4)
            if cat is not None:
                # Clic derecho sobre la ficha: menu de la carpeta.
                for hijo in (b,) + tuple(b.winfo_children()):
                    hijo.bind("<Button-3>",
                              lambda e, c=cat: self._menu_carpeta(e, c))

    def _menu_carpeta(self, evento, cat):
        m = tk.Menu(self, tearoff=0, bd=0, bg=C["tarjeta"], fg=C["texto"],
                    activebackground=C["acento"], activeforeground=C["sobre"],
                    font=("Segoe UI", 9))
        m.add_command(label="Renombrar carpeta",
                      command=lambda: self.renombrar_carpeta(cat))
        m.add_separator()
        m.add_command(label="Eliminar carpeta y su contenido",
                      command=lambda: self.borrar_carpeta(cat))
        try:
            m.tk_popup(evento.x_root, evento.y_root)
        finally:
            m.grab_release()

    def renombrar_carpeta(self, cat):
        d = self._dialogo(lambda: DlgLinea(self, "Renombrar carpeta",
                                           "Nuevo nombre para " + cat))
        nuevo = d.resultado
        if not nuevo or nuevo == cat:
            return
        if nuevo in self.datos["categorias"]:
            self.ocupado = True
            messagebox.showinfo(APP, "Ya existe una carpeta con ese nombre.")
            self.ocupado = False
            return
        i = self.datos["categorias"].index(cat)
        self.datos["categorias"][i] = nuevo
        for sn in self.datos["snippets"]:
            if sn["categoria"] == cat:
                sn["categoria"] = nuevo
        if self.categoria == cat:
            self.categoria = nuevo
        guardar_datos(self.datos)
        self._cambio()
        self._pintar_carpetas()
        self._pintar_lista()
        self._ajustar_pie()

    def borrar_carpeta(self, cat):
        dentro = [x for x in self.datos["snippets"] if x["categoria"] == cat]
        self.ocupado = True
        if dentro:
            aviso = ("Eliminar la carpeta %s y sus %d texto%s?\n\n"
                     "Esto no se puede deshacer."
                     % (cat, len(dentro), "" if len(dentro) == 1 else "s"))
        else:
            aviso = "Eliminar la carpeta %s?" % cat
        ok = messagebox.askyesno(APP, aviso, icon="warning")
        self.ocupado = False
        if not ok:
            return
        for x in dentro:
            self.datos["snippets"].remove(x)
        try:
            self.datos["categorias"].remove(cat)
        except ValueError:
            pass
        if self.categoria == cat:
            self.categoria = None
        guardar_datos(self.datos)
        self._cambio()
        self._pintar_carpetas()
        self._pintar_lista()
        self._ajustar_pie()

    def _carpeta(self, cat):
        self.categoria = cat
        self._pintar_carpetas()
        self._pintar_lista()
        self._ajustar_pie()

    def _ajustar_pie(self):
        """El boton de borrar carpeta solo aparece si hay una elegida."""
        if self.pestana == "guardados" and self.categoria:
            self.b_borrar_carp.pack(side="left", padx=(0, 4))
        else:
            self.b_borrar_carp.pack_forget()

    # ------------------------------------------------ lista

    def _tecla(self, e):
        if e.keysym in ("Down", "Up", "Return", "Escape"):
            return
        if self._pendiente:
            try:
                self.after_cancel(self._pendiente)
            except Exception:
                pass
        self._pendiente = self.after(45, self._buscar_ya)

    def _buscar_ya(self):
        self._pendiente = None
        self._pintar_lista()

    def _indice(self):
        """Arma una vez la lista de (dato, tipo, titulo, cuerpo) normalizada.

        Se rehace solo cuando cambian los datos, no en cada tecla.
        """
        if self._idx is not None:
            return self._idx
        idx = []
        for g in self.datos["snippets"]:
            idx.append((g, "g", normalizar(g["titulo"]),
                        normalizar(plano(g["runs"]))))
        fijos = [x for x in self.hist if x.get("pin")]
        resto = [x for x in self.hist if not x.get("pin")]
        for it in fijos + resto:
            if it["tipo"] == "imagen":
                idx.append((it, "h", "imagen captura", ""))
            else:
                t = it.get("texto", "")
                idx.append((it, "h", normalizar(una_linea(t, 80)),
                            normalizar(t)))
        self._idx = idx
        return idx

    def _pintar_lista(self):
        q = self.e_buscar.get().strip()
        palabras = normalizar(q).split() if q else []
        buscando = bool(palabras)
        items = []

        if buscando:
            # Con busqueda activa se mira todo: guardados e historial.
            puntuados = []
            for dato, tipo, tit, cue in self._indice():
                p = puntuar(palabras, tit, cue)
                if p is None:
                    continue
                # los guardados pesan un poco mas que lo copiado al vuelo
                if tipo == "g":
                    p += 40
                puntuados.append((p, dato, tipo))
            puntuados.sort(key=lambda x: -x[0])
            items = [(d, t) for _, d, t in puntuados]
        elif self.pestana == "guardados":
            for g in self.datos["snippets"]:
                if self.categoria and g["categoria"] != self.categoria:
                    continue
                items.append((g, "g"))
        else:
            fijos = [x for x in self.hist if x.get("pin")]
            resto = [x for x in self.hist if not x.get("pin")]
            items = [(x, "h") for x in fijos + resto]

        self.visibles = [d for d, _ in items]
        self.tipos = [t for _, t in items]
        self.sel = 0
        self.lista.mensaje(
            "Nada coincide con esa busqueda." if buscando else
            "Copia algo y aparecera aqui." if self.pestana == "reciente" else
            "Vacio. Usa Nuevo para guardar un texto.")
        self.lista.cargar(items)

    def _click_fila(self, i):
        self.sel = i

    def _cambio(self):
        """Los datos cambiaron: hay que rehacer el indice de busqueda."""
        self._idx = None

    def _accion(self, que, dato):
        if que == "pin":
            dato["pin"] = not dato.get("pin")
            guardar_hist(self.hist)
            self._cambio()
            self._pintar_lista()
        elif que == "editar":
            self.editar(dato)
        elif que == "borrar":
            self.borrar(dato)

    def _saltar(self, paso):
        if self.visibles:
            self.sel = max(0, min(len(self.visibles) - 1, self.sel + paso))
            self.lista.seleccionar(self.sel)
        return "break"

    def actual(self):
        if self.sel < len(self.visibles):
            return self.visibles[self.sel], self.tipos[self.sel]
        return None, None

    # ------------------------------------------------ dialogos

    def _dialogo(self, fabrica):
        self.ocupado = True
        try:
            d = fabrica()
            self.wait_window(d)
            return d
        finally:
            self.ocupado = False

    def nuevo(self):
        d = self._dialogo(lambda: DlgTexto(self, self.datos["categorias"]))
        if d.resultado:
            self._asegurar(d.resultado["categoria"])
            self.datos["snippets"].append(d.resultado)
            guardar_datos(self.datos)
            self._cambio()
            self.cambiar("guardados")

    def editar(self, dato=None):
        if dato is None:
            dato, _ = self.actual()
        if dato is None:
            return
        if dato.get("tipo") == "texto":
            d = self._dialogo(lambda: DlgTexto(self, self.datos["categorias"],
                                               texto_inicial=dato["texto"]))
            if d.resultado:
                self._asegurar(d.resultado["categoria"])
                self.datos["snippets"].append(d.resultado)
                guardar_datos(self.datos)
                self._cambio()
                self.cambiar("guardados")
            return
        d = self._dialogo(lambda: DlgTexto(self, self.datos["categorias"], dato))
        if d.resultado:
            self.datos["snippets"][self.datos["snippets"].index(dato)] = \
                d.resultado
            self._asegurar(d.resultado["categoria"])
            guardar_datos(self.datos)
            self._cambio()
            self._pintar_lista()

    def borrar(self, dato=None):
        if dato is None:
            dato, _ = self.actual()
        if dato is None:
            return
        if dato.get("tipo"):
            if dato["tipo"] == "imagen":
                try:
                    os.remove(dato.get("ruta", ""))
                except Exception:
                    pass
            try:
                self.hist.remove(dato)
            except ValueError:
                return
            guardar_hist(self.hist)
            self._cambio()
        else:
            try:
                self.datos["snippets"].remove(dato)
            except ValueError:
                return
            guardar_datos(self.datos)
            self._cambio()
        self._pintar_lista()

    def limpiar(self):
        self.ocupado = True
        ok = messagebox.askyesno(APP, "Vaciar el historial?\n"
                                      "Los fijados se quedan.")
        self.ocupado = False
        if not ok:
            return
        for it in self.hist:
            if not it.get("pin") and it["tipo"] == "imagen":
                try:
                    os.remove(it.get("ruta", ""))
                except Exception:
                    pass
        self.hist = [x for x in self.hist if x.get("pin")]
        guardar_hist(self.hist)
        self._cambio()
        self._pintar_lista()

    def _asegurar(self, cat):
        if cat and cat not in self.datos["categorias"]:
            self.datos["categorias"].append(cat)
            guardar_datos(self.datos)
            self._cambio()
            self._pintar_carpetas()

    def nueva_carpeta(self):
        d = self._dialogo(lambda: DlgLinea(self, "Nueva carpeta",
                                           "Nombre de la carpeta"))
        if not d.resultado:
            return
        self._asegurar(d.resultado)
        self.categoria = d.resultado
        self.cambiar("guardados")
        self._pintar_carpetas()
        self.pegar_lista(d.resultado)

    def pegar_lista(self, carpeta=None):
        carpeta = carpeta or self.categoria
        if not carpeta:
            self.ocupado = True
            messagebox.showinfo(APP, "Elige primero una carpeta arriba.")
            self.ocupado = False
            return
        d = self._dialogo(lambda: DlgLista(self, carpeta))
        if not d.resultado:
            self._pintar_lista()
            return
        for texto in d.resultado:
            primera = texto.strip().splitlines()[0]
            self.datos["snippets"].append(
                {"titulo": una_linea(primera, 48), "categoria": carpeta,
                 "runs": [run(texto)]})
        guardar_datos(self.datos)
        self._cambio()
        self.cambiar("guardados")

    def colores(self):
        d = self._dialogo(lambda: DlgColores(self))
        if not d.resultado:
            return
        p = cargar_prefs()
        p["acento"] = d.resultado
        guardar_prefs(p)
        aplicar_tema(d.resultado)
        self._reconstruir()

    def _reconstruir(self):
        estado = self.pestana
        self.marco.destroy()
        self._construir()
        self._pintar_carpetas()
        self.cambiar(estado)
        self.lista.repintar_colores()

    # ------------------------------------------------ pegar

    def pegar(self, sin_formato=False):
        dato, tipo = self.actual()
        if dato is None:
            return
        if tipo == "h":
            if dato["tipo"] == "imagen":
                if not copiar_imagen(dato.get("ruta", "")):
                    return
                self.ultimo_cb = None
            else:
                pyperclip.copy(dato.get("texto", ""))
                self.ultimo_cb = dato.get("texto", "")
        else:
            runs = dato["runs"]
            campos = campos_de(plano(runs))
            if campos:
                d = self._dialogo(lambda: DlgCampos(self, campos))
                if d.resultado is None:
                    return
                runs = rellenar(runs, d.resultado)
            copiar_runs(runs, sin_formato)
            self.ultimo_cb = plano(runs)
        destino = self.destino
        self.withdraw()
        self.destino = None
        threading.Thread(target=self._enviar, args=(destino,),
                         daemon=True).start()

    @staticmethod
    def _enviar(destino):
        # Primero devolvemos el foco al campo donde estaba el cursor;
        # si no, el Ctrl+V se va al vacio.
        time.sleep(0.08)
        devolver_foco(destino)
        time.sleep(0.14)
        try:
            keyboard.send("ctrl+v")
        except Exception:
            pass

    # ------------------------------------------------ vigilancia

    def _vigilar(self):
        try:
            tipo, dato = leer_portapapeles()
            if tipo == "texto" and dato and dato.strip():
                if len(dato) > MAX_CARACTERES:
                    dato = dato[:MAX_CARACTERES]
                if dato != self.ultimo_cb:
                    self.ultimo_cb = dato
                    self._anotar({"tipo": "texto", "texto": dato})
            elif tipo == "imagen" and dato:
                marca = "img%d" % len(dato)
                if marca != self.ultimo_cb:
                    self.ultimo_cb = marca
                    self._imagen(dato)
        except Exception:
            pass
        self.after(900, self._vigilar)

    def _imagen(self, dib):
        try:
            os.makedirs(D_IMG, exist_ok=True)
            ruta = os.path.join(D_IMG, "img_%d.bmp" % int(time.time() * 1000))
            with open(ruta, "wb") as f:
                f.write(dib_a_bmp(dib))
            self._anotar({"tipo": "imagen", "ruta": ruta})
        except Exception:
            pass

    def _anotar(self, item):
        for x in self.hist[:4]:
            if item["tipo"] == "texto" and x.get("texto") == item.get("texto"):
                return
        fijos = sum(1 for x in self.hist if x.get("pin"))
        self.hist.insert(fijos, item)
        libres = [x for x in self.hist if not x.get("pin")]
        for viejo in libres[MAX_HIST:]:
            if viejo["tipo"] == "imagen":
                try:
                    os.remove(viejo.get("ruta", ""))
                except Exception:
                    pass
            self.hist.remove(viejo)
        guardar_hist(self.hist)
        self._cambio()
        if self.state() != "withdrawn" and self.pestana == "reciente" \
                and not self.e_buscar.get().strip():
            self._pintar_lista()

    def _tema(self):
        try:
            ahora = windows_claro()
            if ahora != self._claro:
                self._claro = ahora
                aplicar_tema()
                self._reconstruir()
        except Exception:
            pass
        self.after(5000, self._tema)

    # ------------------------------------------------ mostrar

    def _area(self, x, y):
        if HAY_WIN32:
            try:
                info = win32api.GetMonitorInfo(
                    win32api.MonitorFromPoint((x, y), 2))
                return info["Work"]
            except Exception:
                pass
        return (0, 0, self.winfo_screenwidth(), self.winfo_screenheight())

    def mostrar(self):
        if self.destino is None:
            self.destino = ventana_activa()
        px, py = self.winfo_pointerx(), self.winfo_pointery()
        izq, arr, der, aba = self._area(px, py)
        x = px + 10 if px + 10 + ANCHO <= der else px - ANCHO - 10
        y = py + 14 if py + 14 + ALTO <= aba else py - ALTO - 14
        x = max(izq + 6, min(int(x), der - ANCHO - 6))
        y = max(arr + 6, min(int(y), aba - ALTO - 6))
        self.geometry("%dx%d+%d+%d" % (ANCHO, ALTO, x, y))
        self.e_buscar.delete(0, "end")
        self._pintar_lista()
        self.deiconify()
        self.lift()
        self.after(40, self.focus_force)
        self.after(90, self.e_buscar.focus_set)

    def ocultar(self):
        self.withdraw()
        self.destino = None

    def _chequear(self):
        if self.ocupado:
            return
        try:
            if self.focus_displayof() is None:
                self.ocultar()
        except Exception:
            pass

    def alternar(self):
        if self.state() == "withdrawn":
            self.mostrar()
        else:
            self.ocultar()

    def _hotkey(self):
        try:
            keyboard.add_hotkey(HOTKEY, self._atajo)
        except Exception as e:
            messagebox.showwarning(
                APP, "No pude registrar %s.\nAbrelo como administrador.\n\n%s"
                     % (HOTKEY, e))

    def _atajo(self):
        # Se anota AQUI, en el hilo del teclado: es el ultimo instante en
        # que la ventana del usuario todavia tiene el foco.
        self.cola.put(ventana_activa())

    def _cola(self):
        try:
            while True:
                hwnd = self.cola.get_nowait()
                if self.state() == "withdrawn":
                    self.destino = hwnd
                self.alternar()
        except queue.Empty:
            pass
        self.after(120, self._cola)


# ================================================================ arranque

if __name__ == "__main__":
    aplicar_tema()
    try:
        if cargar_prefs().get("autoarranque", "si") == "si":
            autoarranque(True)
    except Exception:
        pass
    app = Panel()
    app.mostrar()
    app.mainloop()
