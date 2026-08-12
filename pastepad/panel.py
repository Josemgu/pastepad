# -*- coding: utf-8 -*-
"""El panel: coordina el modelo, la lista y los dialogos.

No toca el disco ni habla con Windows directamente; para eso estan
modelo.py y windows.py.
"""

import queue
import threading
import webbrowser
import time
import tkinter as tk
from tkinter import messagebox

import customtkinter as ctk
import keyboard
import pyperclip

from . import config as cfg
from . import dialogos as dlg
from . import modelo
from . import windows as win
from .busqueda import Indice
from .lista import Lista
from .tema import C, aplicar, boton, boton_icono, entrada, etiqueta, fuente
from .tema import pildora, simbolo


class Panel(ctk.CTk):

    def __init__(self, almacen):
        super().__init__(fg_color=C["fondo"])
        self.almacen = almacen
        self.indice = Indice(almacen)

        self.overrideredirect(True)
        self.tamano = almacen.pref("tamano", cfg.TAMANO_DEF)
        if self.tamano == "libre":
            self.ancho = max(cfg.MIN_ANCHO, min(cfg.MAX_ANCHO,
                             int(almacen.pref("ancho", cfg.TAMANOS[
                                 cfg.TAMANO_DEF][0]))))
            self.alto = max(cfg.MIN_ALTO, min(cfg.MAX_ALTO,
                            int(almacen.pref("alto", cfg.TAMANOS[
                                cfg.TAMANO_DEF][1]))))
        else:
            if self.tamano not in cfg.TAMANOS:
                self.tamano = cfg.TAMANO_DEF
            self.ancho, self.alto = cfg.TAMANOS[self.tamano]
        self.geometry("%dx%d" % (self.ancho, self.alto))
        self.attributes("-topmost", True)

        # --- estado
        self.pestana = "reciente"
        self.carpeta = None
        self.visibles = []
        self.tipos = []
        self.sel = 0
        self.marcando = False
        self.ocupado = False
        self.destino = None
        self.ultimo_texto = None
        self._seq = win.secuencia()
        self._espera_busqueda = None
        self._atajo_puesto = None
        self._estirando = False
        self._contorno = None
        self._destino = None
        self.atajo = almacen.pref("atajo", cfg.ATAJO_DEF)
        if self.atajo not in cfg.ATAJOS:
            self.atajo = cfg.ATAJO_DEF
        self.pausado = bool(almacen.pref("pausado", False))
        self.modo_carpetas = almacen.pref("carpetas", cfg.CARPETAS_DEF)
        self._claro = win.tema_claro()
        self.cola = queue.Queue()

        self._construir()
        self._pintar_carpetas()
        self.refrescar()

        self.bind("<Escape>", lambda e: self.ocultar())
        self.bind("<FocusOut>", lambda e: self.after(170, self._revisar_foco))
        self.after(80, self._redondear)
        self.registrar_atajo()
        self.after(120, self._atender_cola)
        self.after(900, self._vigilar)
        self.after(30000, self._revisar_tema)

    # ------------------------------------------------------ interfaz

    def _construir(self):
        self.marco = ctk.CTkFrame(self, fg_color=C["fondo"],
                                  corner_radius=cfg.R_PANEL,
                                  border_width=1, border_color=C["borde"])
        self.marco.pack(fill="both", expand=True, padx=1, pady=1)

        self._pie()
        self._cabecera()
        self._buscador()
        self._pestanas()

        self.barra_carpetas = ctk.CTkFrame(self.marco, fg_color="transparent",
                                           height=0)
        self.barra_carpetas.pack(fill="x", padx=cfg.E3)

        self.lista = Lista(self.marco, self._al_elegir, self.pegar,
                           self._al_accion)
        self.lista.compactar(self.alto <= 400)
        self.lista.pack(fill="both", expand=True, padx=cfg.E2,
                        pady=(cfg.E1, cfg.E2))
        self._montar_agarre()

    def _cabecera(self):
        cab = ctk.CTkFrame(self.marco, fg_color="transparent", height=36)
        cab.pack(fill="x", padx=cfg.E3, pady=(cfg.E3, 0))
        cab.pack_propagate(False)

        self.titulo = etiqueta(cab, cfg.ATAJOS.get(self.atajo, self.atajo),
                               cfg.T_MENOR, C["tenue"])
        self.titulo.pack(side="left", padx=cfg.E1)

        boton_icono(cab, "cerrar", self.ocultar, 13, 28).pack(side="right")
        boton_icono(cab, "paleta", self.apariencia, 14,
                    28).pack(side="right", padx=2)
        self.b_pausa = boton_icono(
            cab, "grabar" if self.pausado else "pausa", self.alternar_pausa,
            13, 28, "#EF4444" if self.pausado else None)
        self.b_pausa.pack(side="right", padx=2)
        self.aviso_pausa = etiqueta(cab, "En pausa", cfg.T_MINI, "#EF4444")
        if self.pausado:
            self.aviso_pausa.pack(side="left", padx=cfg.E2)

        for w in (cab, self.titulo):
            w.bind("<Button-1>", self._agarrar)
            w.bind("<B1-Motion>", self._arrastrar)

    def _buscador(self):
        caja = ctk.CTkFrame(self.marco, fg_color=C["elevado"],
                            corner_radius=cfg.R_CONTROL, height=42)
        caja.pack(fill="x", padx=cfg.E3, pady=(cfg.E2, cfg.E2))
        caja.pack_propagate(False)
        ctk.CTkLabel(caja, text=simbolo("buscar"), width=18,
                     text_color=C["tenue"],
                     font=fuente(cfg.T_CUERPO)).pack(side="left",
                                                     padx=(cfg.E3, 0))
        self.buscador = ctk.CTkEntry(
            caja, height=34, border_width=0, fg_color=C["elevado"],
            text_color=C["texto"], placeholder_text="Buscar en todo",
            placeholder_text_color=C["tenue"], font=fuente())
        self.buscador.pack(side="left", fill="x", expand=True,
                           padx=(cfg.E1, cfg.E3))
        self.buscador.bind("<KeyRelease>", self._al_teclear)
        self.buscador.bind("<Return>", lambda e: self.pegar())
        self.buscador.bind("<Control-Return>", lambda e: self.pegar(True))
        self.buscador.bind("<Down>", lambda e: self._mover(1))
        self.buscador.bind("<Up>", lambda e: self._mover(-1))

    def _pestanas(self):
        marco = ctk.CTkFrame(self.marco, fg_color="transparent", height=36)
        marco.pack(fill="x", padx=cfg.E3, pady=(0, cfg.E2))
        marco.pack_propagate(False)
        self.tab_reciente = pildora(marco, "Reciente",
                                    lambda: self.cambiar("reciente"), True)
        self.tab_reciente.pack(side="left", expand=True, fill="x",
                               padx=(0, 3))
        self.tab_guardados = pildora(marco, "Guardados",
                                     lambda: self.cambiar("guardados"))
        self.tab_guardados.pack(side="left", expand=True, fill="x",
                                padx=(3, 0))

    def _pie(self):
        pie = ctk.CTkFrame(self.marco, fg_color="transparent", height=46)
        pie.pack(side="bottom", fill="x", padx=cfg.E3, pady=(0, cfg.E3))
        pie.pack_propagate(False)

        ctk.CTkButton(
            pie, text=" %s  Nuevo" % simbolo("mas"), width=98, height=34,
            corner_radius=cfg.R_CONTROL, fg_color=C["acento"],
            hover_color=C["acento_h"], text_color=C["sobre"],
            font=fuente(cfg.T_MENOR), command=self.nuevo).pack(side="right")

        self.b_carpeta = boton_icono(pie, "carpeta", self.nueva_carpeta)
        self.b_lista = boton_icono(pie, "lista", self.agregar_lista)
        self.b_escoba = boton_icono(pie, "escoba", self.vaciar)
        self.b_borrar_carpeta = boton(pie, "Borrar carpeta",
                                      self.borrar_carpeta, 112, 32,
                                      "fantasma", cfg.T_MINI)
        self.b_marcar = boton(pie, "Seleccionar", self.alternar_marcado,
                              94, 32, "fantasma", cfg.T_MINI)
        self.b_todos = boton(pie, "Todos", self.marcar_todos, 60, 32,
                             "fantasma", cfg.T_MINI)
        self.b_borrar_sel = boton(pie, "Borrar", self.borrar_marcados, 76,
                                  32, "peligro", cfg.T_MINI)

    def _agarrar(self, e):
        self._dx = e.x_root - self.winfo_x()
        self._dy = e.y_root - self.winfo_y()

    def _arrastrar(self, e):
        self.geometry("+%d+%d" % (e.x_root - self._dx, e.y_root - self._dy))

    def _redondear(self):
        try:
            self.update_idletasks()
            win.redondear(win.hwnd_real(self), self.ancho, self.alto,
                          cfg.R_PANEL)
        except Exception:
            pass

    # ------------------------------------------------------ pestanas

    def cambiar(self, cual):
        self.pestana = cual
        reciente = cual == "reciente"
        for tab, activa in ((self.tab_reciente, reciente),
                            (self.tab_guardados, not reciente)):
            tab.configure(
                fg_color=C["acento"] if activa else C["tarjeta"],
                hover_color=C["acento_h"] if activa else C["hover"],
                text_color=C["sobre"] if activa else C["medio"])
        self.barra_carpetas.configure(height=0 if reciente else 38)
        if self.marcando:
            self.marcando = False
            self.lista.modo_marcar(False)
        self._ajustar_pie()
        self.refrescar()
        self.buscador.focus_set()

    def _pintar_carpetas(self):
        for w in self.barra_carpetas.winfo_children():
            w.destroy()
        if self.modo_carpetas == cfg.CARPETAS_MENU:
            self._carpetas_desplegable()
        else:
            self._carpetas_fichas()

    def _carpetas_fichas(self):
        for nombre in [None] + list(self.almacen.carpetas):
            activa = nombre == self.carpeta
            b = pildora(self.barra_carpetas,
                        "Todas" if nombre is None else nombre,
                        lambda n=nombre: self._elegir_carpeta(n), activa)
            b.pack(side="left", padx=3, pady=cfg.E1)
            if nombre is not None:
                for hijo in (b,) + tuple(b.winfo_children()):
                    hijo.bind("<Button-3>",
                              lambda e, n=nombre: self._menu_carpeta(e, n))

    def _carpetas_desplegable(self):
        """Un solo boton con el nombre de la carpeta activa.

        Con muchas carpetas, las fichas se salen de la ventana; asi
        ocupan siempre lo mismo.
        """
        texto = self.carpeta or "Todas las carpetas"
        b = ctk.CTkButton(
            self.barra_carpetas,
            text="  %s   %s" % (texto, simbolo("abajo")),
            height=30, corner_radius=15, anchor="w",
            fg_color=C["acento"] if self.carpeta else C["tarjeta"],
            hover_color=C["acento_h"] if self.carpeta else C["hover"],
            text_color=C["sobre"] if self.carpeta else C["medio"],
            font=fuente(cfg.T_MENOR),
            command=self._desplegar_carpetas)
        b.pack(side="left", fill="x", expand=True, padx=3, pady=cfg.E1)
        self._boton_carpetas = b

    def _desplegar_carpetas(self):
        m = self._menu_nuevo()
        m.add_command(label="Todas las carpetas",
                      command=lambda: self._elegir_carpeta(None))
        if self.almacen.carpetas:
            m.add_separator()
        for nombre in self.almacen.carpetas:
            marca = "  \u2713  " if nombre == self.carpeta else "     "
            m.add_command(label=marca + nombre,
                          command=lambda n=nombre: self._elegir_carpeta(n))
        m.add_separator()
        m.add_command(label="Nueva carpeta...", command=self.nueva_carpeta)
        if self.carpeta:
            m.add_command(label="Renombrar %s..." % self.carpeta,
                          command=lambda: self.renombrar_carpeta())
            m.add_command(label="Eliminar %s y su contenido" % self.carpeta,
                          command=lambda: self.borrar_carpeta())
        b = self._boton_carpetas
        self._abrir_menu(m, b.winfo_rootx(), b.winfo_rooty() + b.winfo_height())

    def _elegir_carpeta(self, nombre):
        self.carpeta = nombre
        self._pintar_carpetas()
        self.refrescar()
        self._ajustar_pie()

    def _ajustar_pie(self):
        """Solo se ven los botones que tienen sentido ahora mismo."""
        for b in (self.b_carpeta, self.b_lista, self.b_borrar_carpeta,
                  self.b_escoba, self.b_marcar, self.b_todos,
                  self.b_borrar_sel):
            b.pack_forget()

        if self.marcando:
            self.b_todos.pack(side="left")
            self.b_borrar_sel.pack(side="left", padx=cfg.E1)
            self.b_marcar.configure(text="Cancelar")
            self.b_marcar.pack(side="left")
            self._contar_marcados()
            return

        self.b_marcar.configure(text="Seleccionar")
        if self.pestana == "guardados":
            self.b_carpeta.pack(side="left")
            self.b_lista.pack(side="left", padx=cfg.E1)
            if self.carpeta:
                self.b_borrar_carpeta.pack(side="left", padx=(0, cfg.E1))
        else:
            self.b_escoba.pack(side="left")
        if self.visibles:
            self.b_marcar.pack(side="left", padx=cfg.E1)

    # ------------------------------------------------------ lista

    def _al_teclear(self, e):
        if e.keysym in ("Down", "Up", "Return", "Escape"):
            return
        if self._espera_busqueda:
            try:
                self.after_cancel(self._espera_busqueda)
            except Exception:
                pass
        self._espera_busqueda = self.after(45, self._buscar)

    def _buscar(self):
        self._espera_busqueda = None
        self.refrescar()

    def refrescar(self):
        """Rehace lo que se ve, segun la pestana y lo que haya escrito."""
        consulta = self.buscador.get().strip()
        if consulta:
            items = self.indice.buscar(consulta)
            aviso = "Nada coincide con esa busqueda."
        elif self.pestana == "guardados":
            items = [(s, "g") for s in self.almacen.snippets
                     if not self.carpeta or s["categoria"] == self.carpeta]
            aviso = "Vacio. Usa Nuevo para guardar un texto."
        else:
            items = [(h, "h") for h in self.almacen.hist_ordenado()]
            aviso = "Copia algo y aparecera aqui."

        self.visibles = [d for d, _ in items]
        self.tipos = [t for _, t in items]
        self.sel = 0
        self.lista.cargar(items, aviso)
        if hasattr(self, "b_marcar"):
            self._ajustar_pie()

    def _al_elegir(self, i):
        self.sel = i

    def _al_accion(self, accion, dato):
        if accion == "menu":
            entrada, x, y = dato
            self._menu_fila(entrada, x, y)
            return
        acciones = {"conteo": lambda: self._contar_marcados(),
                    "pin": lambda: self._fijar(dato),
                    "editar": lambda: self.editar(dato),
                    "borrar": lambda: self.borrar(dato)}
        hacer = acciones.get(accion)
        if hacer:
            hacer()

    def _fijar(self, dato):
        self.almacen.fijar(dato)
        self.indice.invalidar()
        self.refrescar()

    def _mover(self, paso):
        if self.visibles:
            self.sel = max(0, min(len(self.visibles) - 1, self.sel + paso))
            self.lista.elegir(self.sel)
        return "break"

    def actual(self):
        if self.sel < len(self.visibles):
            return self.visibles[self.sel], self.tipos[self.sel]
        return None, None

    # ------------------------------------------------------ dialogos

    def _abrir(self, fabrica):
        """Abre un dialogo y espera. Marca ocupado para que el panel no
        se esconda al perder el foco."""
        self.ocupado = True
        try:
            d = fabrica()
            self.wait_window(d)
            return d.resultado
        finally:
            self.ocupado = False

    def _preguntar(self, mensaje, icono=None):
        self.ocupado = True
        try:
            return messagebox.askyesno(cfg.APP, mensaje,
                                       **({"icon": icono} if icono else {}))
        finally:
            self.ocupado = False

    def _avisar(self, mensaje):
        self.ocupado = True
        try:
            messagebox.showinfo(cfg.APP, mensaje)
        finally:
            self.ocupado = False

    def nuevo(self):
        r = self._abrir(lambda: dlg.DlgTexto(self, self.almacen.carpetas))
        if r:
            self.almacen.anadir_snippet(r)
            self.indice.invalidar()
            self._pintar_carpetas()
            self.cambiar("guardados")

    def editar(self, dato=None):
        if dato is None:
            dato, _ = self.actual()
        if dato is None:
            return
        if dato.get("tipo") == "texto":
            r = self._abrir(lambda: dlg.DlgTexto(
                self, self.almacen.carpetas, texto=dato["texto"]))
            if r:
                self.almacen.anadir_snippet(r)
                self.indice.invalidar()
                self._pintar_carpetas()
                self.cambiar("guardados")
            return
        r = self._abrir(lambda: dlg.DlgTexto(self, self.almacen.carpetas,
                                             dato))
        if r and self.almacen.reemplazar_snippet(dato, r):
            self.indice.invalidar()
            self._pintar_carpetas()
            self.refrescar()

    def borrar(self, dato=None):
        if dato is None:
            dato, _ = self.actual()
        if dato and self.almacen.borrar(dato):
            self.indice.invalidar()
            self.refrescar()

    def vaciar(self):
        if self._preguntar("Vaciar el historial?\nLos fijados se quedan."):
            self.almacen.vaciar_historial()
            self.indice.invalidar()
            self.refrescar()

    def nueva_carpeta(self):
        nombre = self._abrir(lambda: dlg.DlgLinea(self, "Nueva carpeta",
                                                  "Nombre de la carpeta"))
        if not nombre:
            return
        self.almacen.crear_carpeta(nombre)
        self.carpeta = nombre
        self.cambiar("guardados")
        self._pintar_carpetas()
        self.agregar_lista(nombre)

    def agregar_lista(self, carpeta=None):
        carpeta = carpeta or self.carpeta
        if not carpeta:
            self._avisar("Elige primero una carpeta arriba.")
            return
        lineas = self._abrir(lambda: dlg.DlgLista(self, carpeta))
        if not lineas:
            self.refrescar()
            return
        for texto in lineas:
            primera = texto.strip().splitlines()[0]
            self.almacen.anadir_snippet({
                "titulo": modelo.una_linea(primera, 48),
                "categoria": carpeta,
                "runs": [modelo.fragmento(texto)]})
        self.indice.invalidar()
        self.cambiar("guardados")

    def _menu_nuevo(self):
        return tk.Menu(self, tearoff=0, bd=0, bg=C["elevado"], fg=C["texto"],
                       activebackground=C["acento"],
                       activeforeground=C["sobre"],
                       activeborderwidth=0, relief="flat",
                       font=(cfg.FUENTE_ALT, 9))

    def _abrir_menu(self, menu, x, y):
        """Mientras el menu esta abierto el panel no debe esconderse."""
        self.ocupado = True
        try:
            menu.tk_popup(x, y)
        finally:
            menu.grab_release()
            self.after(300, lambda: setattr(self, "ocupado", False))

    def _menu_fila(self, dato, x, y):
        """Las acciones de una entrada, detras del boton de tres puntos."""
        es_hist = bool(dato.get("tipo"))
        texto = (dato.get("texto", "") if es_hist
                 else modelo.texto_de(dato.get("runs", [])))
        m = self._menu_nuevo()

        if modelo.es_enlace(texto):
            m.add_command(label="Abrir en el navegador",
                          command=lambda: self.abrir_enlace(texto))
            m.add_separator()
        m.add_command(label="Pegar", command=self.pegar)
        m.add_command(label="Pegar sin formato",
                      command=lambda: self.pegar(True))
        m.add_command(label="Copiar", command=lambda: self.copiar(dato))
        m.add_separator()
        if es_hist:
            m.add_command(label="Quitar de arriba" if dato.get("pin")
                          else "Fijar arriba",
                          command=lambda: self._fijar(dato))
            if dato.get("tipo") == "texto":
                m.add_command(label="Editar y guardar...",
                              command=lambda: self.editar(dato))
        else:
            m.add_command(label="Editar...", command=lambda: self.editar(dato))
        m.add_separator()
        m.add_command(label="Borrar", command=lambda: self.borrar(dato))
        self._abrir_menu(m, x, y)

    def abrir_enlace(self, texto):
        self.ocultar()
        try:
            webbrowser.open(modelo.url_de(texto))
        except Exception:
            pass

    def copiar(self, dato):
        """Deja el texto en el portapapeles sin pegarlo en ningun sitio."""
        if dato.get("tipo"):
            if dato["tipo"] == "imagen":
                win.copiar_imagen(dato.get("ruta", ""))
            else:
                pyperclip.copy(dato.get("texto", ""))
                self.ultimo_texto = dato.get("texto", "")
        else:
            win.copiar(dato["runs"], modelo.texto_de)
            self.ultimo_texto = modelo.texto_de(dato["runs"])
        self._seq = win.secuencia()
        self.ocultar()

    def _menu_carpeta(self, evento, nombre):
        m = self._menu_nuevo()
        m.add_command(label="Renombrar carpeta",
                      command=lambda: self.renombrar_carpeta(nombre))
        m.add_separator()
        m.add_command(label="Eliminar carpeta y su contenido",
                      command=lambda: self.borrar_carpeta(nombre))
        self._abrir_menu(m, evento.x_root, evento.y_root)

    def renombrar_carpeta(self, nombre=None):
        nombre = nombre or self.carpeta
        if not nombre:
            return
        nuevo = self._abrir(lambda: dlg.DlgLinea(
            self, "Renombrar carpeta", "Nuevo nombre para " + nombre))
        if not nuevo or nuevo == nombre:
            return
        if not self.almacen.renombrar_carpeta(nombre, nuevo):
            self._avisar("Ya existe una carpeta con ese nombre.")
            return
        if self.carpeta == nombre:
            self.carpeta = nuevo
        self.indice.invalidar()
        self._pintar_carpetas()
        self.refrescar()

    def borrar_carpeta(self, nombre=None):
        nombre = nombre or self.carpeta
        if not nombre:
            return
        cuantos = len(self.almacen.contenido_de(nombre))
        aviso = ("Eliminar la carpeta %s y sus %d texto%s?\n\n"
                 "Esto no se puede deshacer."
                 % (nombre, cuantos, "" if cuantos == 1 else "s")
                 if cuantos else "Eliminar la carpeta %s?" % nombre)
        if not self._preguntar(aviso, "warning"):
            return
        self.almacen.borrar_carpeta(nombre)
        if self.carpeta == nombre:
            self.carpeta = None
        self.indice.invalidar()
        self._pintar_carpetas()
        self.refrescar()
        self._ajustar_pie()

    def apariencia(self):
        r = self._abrir(lambda: dlg.DlgApariencia(
            self, C.get("nombre_acento", "menta"), self.tamano, self.atajo,
            self.modo_carpetas))
        if not r:
            return
        acento, tamano, atajo, carpetas = r
        self.almacen.poner_pref("acento", acento)
        if carpetas != self.modo_carpetas:
            self.modo_carpetas = carpetas
            self.almacen.poner_pref("carpetas", carpetas)
        if atajo != self.atajo and self.registrar_atajo(atajo):
            self.almacen.poner_pref("atajo", atajo)
        aplicar(acento)
        if tamano != self.tamano:
            self.cambiar_tamano(tamano)
        else:
            self.reconstruir()

    def cambiar_tamano(self, nombre):
        """Aplica la medida y rehace la interfaz. Pasa una sola vez, no
        decenas por segundo como cuando se arrastraba un borde, asi que
        no deja restos en pantalla."""
        if nombre not in cfg.TAMANOS:
            return
        self.tamano = nombre
        self.ancho, self.alto = cfg.TAMANOS[nombre]
        self.almacen.poner_pref("tamano", nombre)
        izq, arr, der, aba = self._area()
        x = max(izq + 6, min(self.winfo_x(), der - self.ancho - 6))
        y = max(arr + 6, min(self.winfo_y(), aba - self.alto - 6))
        self.geometry("%dx%d+%d+%d" % (self.ancho, self.alto, x, y))
        self.reconstruir()

    def reconstruir(self):
        texto, estado = self.buscador.get(), self.pestana
        try:
            self.marco.destroy()
        except Exception:
            pass
        self.configure(fg_color=C["fondo"])
        self._construir()
        self._pintar_carpetas()
        self.cambiar(estado)
        if texto:
            self.buscador.insert(0, texto)
            self.refrescar()
        self._redondear()

    # ------------------------------------------------------ marcado

    def alternar_marcado(self):
        self.marcando = not self.marcando
        self.lista.modo_marcar(self.marcando)
        self._ajustar_pie()

    def marcar_todos(self):
        self.lista.marcar_todos()
        self._contar_marcados()

    def _contar_marcados(self):
        n = len(self.lista.marcados)
        self.b_borrar_sel.configure(text="Borrar" if not n
                                    else "Borrar (%d)" % n)

    def borrar_marcados(self):
        elegidos = self.lista.elegidos()
        if not elegidos:
            return
        if not self._preguntar(
                "Borrar %d elemento%s?\n\nEsto no se puede deshacer."
                % (len(elegidos), "" if len(elegidos) == 1 else "s"),
                "warning"):
            return
        self.almacen.borrar_varios(elegidos)
        self.indice.invalidar()
        self.marcando = False
        self.lista.modo_marcar(False)
        self.refrescar()
        self._ajustar_pie()

    # ------------------------------------------------------ pegar

    def pegar(self, sin_formato=False):
        dato, tipo = self.actual()
        if dato is None:
            return
        texto = (dato.get("texto", "") if tipo == "h"
                 else modelo.texto_de(dato.get("runs", [])))
        if modelo.es_enlace(texto):
            # Un enlace se abre, que es lo que espera cualquiera al
            # hacerle clic. Para pegarlo esta el menu.
            self.abrir_enlace(texto)
            return
        if tipo == "h":
            if dato["tipo"] == "imagen":
                if not win.copiar_imagen(dato.get("ruta", "")):
                    return
                self.ultimo_texto = None
            else:
                pyperclip.copy(dato.get("texto", ""))
                self.ultimo_texto = dato.get("texto", "")
        else:
            fragmentos = dato["runs"]
            campos = modelo.campos_de(modelo.texto_de(fragmentos))
            if campos:
                valores = self._abrir(lambda: dlg.DlgCampos(self, campos))
                if valores is None:
                    return
                fragmentos = modelo.rellenar(fragmentos, valores)
            win.copiar(fragmentos, modelo.texto_de, sin_formato)
            self.ultimo_texto = modelo.texto_de(fragmentos)

        self._seq = win.secuencia()
        destino = self.destino
        self.withdraw()
        self.destino = None
        threading.Thread(target=self._enviar_pegado, args=(destino,),
                         daemon=True).start()

    @staticmethod
    def _enviar_pegado(destino):
        """Devuelve el foco a la ventana de antes y manda Ctrl+V.

        Sin lo primero, el pegado se va al vacio: al abrirse el panel, el
        campo donde estaba el cursor perdio el foco.
        """
        time.sleep(0.08)
        win.devolver_foco(destino)
        time.sleep(0.14)
        try:
            keyboard.send("ctrl+v")
        except Exception:
            pass

    # ------------------------------------------------------ vigilancia

    def alternar_pausa(self):
        self.pausado = not self.pausado
        self.almacen.poner_pref("pausado", self.pausado)
        self.b_pausa.configure(
            text=simbolo("grabar" if self.pausado else "pausa"),
            text_color="#EF4444" if self.pausado else C["medio"])
        if self.pausado:
            self.aviso_pausa.pack(side="left", padx=cfg.E2)
        else:
            self.aviso_pausa.pack_forget()
            self._seq = win.secuencia()

    def _vigilar(self):
        """Mira el portapapeles, pero solo de verdad cuando cambio."""
        if self.pausado:
            self.after(1200, self._vigilar)
            return
        try:
            seq = win.secuencia()
            if seq is not None and seq == self._seq:
                self.after(700, self._vigilar)
                return
            self._seq = seq
            tipo, dato = win.leer()
            if tipo == "privado":
                pass          # alguien pidio que no se guarde
            elif tipo == "texto" and dato and dato.strip():
                if len(dato) > cfg.MAX_CARACTERES:
                    dato = dato[:cfg.MAX_CARACTERES]
                if dato != self.ultimo_texto:
                    self.ultimo_texto = dato
                    self._anotar({"tipo": "texto", "texto": dato})
            elif tipo == "imagen" and dato:
                marca = "img%d" % len(dato)
                if marca != self.ultimo_texto:
                    self.ultimo_texto = marca
                    if self.almacen.guardar_imagen(dato, win.dib_a_bmp):
                        self._tras_anotar()
        except Exception:
            pass
        self.after(700, self._vigilar)

    def _anotar(self, entrada):
        if self.almacen.anotar(entrada):
            self._tras_anotar()

    def _tras_anotar(self):
        self.indice.invalidar()
        if self.state() == "withdrawn":
            return
        if self.pestana == "reciente" and not self.buscador.get().strip():
            self.refrescar()

    def _revisar_tema(self):
        try:
            claro = win.tema_claro()
            if claro != self._claro:
                self._claro = claro
                aplicar()
                self.reconstruir()
        except Exception:
            pass
        self.after(30000, self._revisar_tema)

    # ------------------------------------------------------ ventana

    def _area(self):
        return win.area_util(self.winfo_x(), self.winfo_y(),
                             self.winfo_screenwidth(),
                             self.winfo_screenheight())

    def mostrar(self):
        if self.destino is None:
            self.destino = win.ventana_activa()
        px, py = self.winfo_pointerx(), self.winfo_pointery()
        izq, arr, der, aba = win.area_util(px, py, self.winfo_screenwidth(),
                                           self.winfo_screenheight())
        ancho, alto = self.ancho, self.alto
        # Si el tamano elegido no cabe en esta pantalla, usa uno menor.
        if ancho > der - izq - 12 or alto > aba - arr - 12:
            for nombre in ("mediano", "chico", "mini"):
                a, l = cfg.TAMANOS[nombre]
                if a <= der - izq - 12 and l <= aba - arr - 12:
                    ancho, alto = a, l
                    break
        x = px + 12 if px + 12 + ancho <= der else px - ancho - 12
        y = py + 16 if py + 16 + alto <= aba else py - alto - 16
        x = max(izq + 8, min(int(x), der - ancho - 8))
        y = max(arr + 8, min(int(y), aba - alto - 8))
        self.geometry("%dx%d+%d+%d" % (ancho, alto, x, y))
        self._redondear()
        self.buscador.delete(0, "end")
        self.refrescar()
        self.deiconify()
        self.lift()
        self.after(40, self.focus_force)
        self.after(90, self.buscador.focus_set)

    def ocultar(self):
        self.withdraw()
        self.destino = None

    def _revisar_foco(self):
        if self.ocupado or self._estirando:
            return
        try:
            if self.focus_displayof() is None:
                self.ocultar()
        except Exception:
            pass

    def alternar_ventana(self):
        self.mostrar() if self.state() == "withdrawn" else self.ocultar()

    # ------------------------------------------------------ atajo

    def registrar_atajo(self, combinacion=None):
        if self._atajo_puesto:
            try:
                keyboard.remove_hotkey(self._atajo_puesto)
            except Exception:
                pass
            self._atajo_puesto = None
        self._estirando = False
        self._contorno = None
        self._destino = None
        combinacion = combinacion or self.atajo
        try:
            self._atajo_puesto = keyboard.add_hotkey(combinacion, self._pulsado)
            self.atajo = combinacion
            if hasattr(self, "titulo"):
                self.titulo.configure(text=cfg.ATAJOS.get(combinacion,
                                                          combinacion))
            return True
        except Exception as e:
            messagebox.showwarning(
                cfg.APP,
                "No pude registrar %s.\n\n%s\n\nPuede que otro programa lo "
                "este usando, o que haga falta abrir este como "
                "administrador." % (cfg.ATAJOS.get(combinacion, combinacion),
                                    e))
            return False

    def _pulsado(self):
        """Corre en el hilo del teclado: es el ultimo instante en que la
        ventana del usuario todavia tiene el foco."""
        self.cola.put(win.ventana_activa())

    def _atender_cola(self):
        try:
            while True:
                hwnd = self.cola.get_nowait()
                if self.state() == "withdrawn":
                    self.destino = hwnd
                self.alternar_ventana()
        except queue.Empty:
            pass
        self.after(120, self._atender_cola)

    # ------------------------------------------------------ tamano libre

    def _montar_agarre(self):
        """Triangulito en la esquina inferior derecha para estirar.

        Solo la esquina, no los cuatro bordes: asi no se pisa con los
        clics de la lista ni con los botones del pie.
        """
        self.agarre = tk.Canvas(self.marco, width=cfg.AGARRE,
                                height=cfg.AGARRE, bd=0, highlightthickness=0,
                                bg=C["fondo"], cursor="size_nw_se")
        self.agarre.place(relx=1.0, rely=1.0, anchor="se", x=-5, y=-5)
        for d in (4, 9, 14):
            self.agarre.create_line(cfg.AGARRE - d, cfg.AGARRE,
                                    cfg.AGARRE, cfg.AGARRE - d,
                                    fill=C["borde_claro"], width=1)
        self.agarre.bind("<Button-1>", self._empezar_estirar)
        self.agarre.bind("<B1-Motion>", self._estirar)
        self.agarre.bind("<ButtonRelease-1>", self._soltar_estirar)

    def _empezar_estirar(self, e):
        self._estirando = True
        self._ini = (e.x_root, e.y_root, self.ancho, self.alto)
        self._destino = None
        try:
            c = tk.Toplevel(self)
            c.overrideredirect(True)
            c.configure(bg=C["acento"])
            c.attributes("-topmost", True)
            c.geometry("%dx%d+%d+%d" % (self.ancho, self.alto,
                                        self.winfo_x(), self.winfo_y()))
            c.update_idletasks()
            # Marco hueco: se ve el tamano futuro sin tapar lo de detras.
            win.marco_hueco(win.hwnd_real(c), self.ancho, self.alto,
                            cfg.R_PANEL, 2)
            self._contorno = c
        except Exception:
            self._contorno = None
        return "break"

    def _estirar(self, e):
        """Solo se mueve el contorno. Redimensionar el panel de verdad en
        cada pixel deja franjas del dibujo anterior: sus widgets se
        pintan sobre lienzos propios y no siempre reciben la orden de
        repintarse."""
        if not self._estirando:
            return
        x0, y0, ancho0, alto0 = self._ini
        ancho = max(cfg.MIN_ANCHO, min(cfg.MAX_ANCHO,
                                       ancho0 + e.x_root - x0))
        alto = max(cfg.MIN_ALTO, min(cfg.MAX_ALTO, alto0 + e.y_root - y0))
        self._destino = (ancho, alto)
        if self._contorno is not None:
            try:
                self._contorno.geometry("%dx%d+%d+%d" % (
                    ancho, alto, self.winfo_x(), self.winfo_y()))
                win.marco_hueco(win.hwnd_real(self._contorno), ancho, alto,
                                cfg.R_PANEL, 2)
            except Exception:
                pass
        return "break"

    def _soltar_estirar(self, e):
        if not self._estirando:
            return
        self._estirando = False
        if self._contorno is not None:
            try:
                self._contorno.destroy()
            except Exception:
                pass
            self._contorno = None
        if not self._destino:
            return
        self.ancho, self.alto = self._destino
        self._destino = None
        self.tamano = "libre"
        self.almacen.poner_pref("tamano", "libre")
        self.almacen.poner_pref("ancho", self.ancho)
        self.almacen.poner_pref("alto", self.alto)
        self.geometry("%dx%d" % (self.ancho, self.alto))
        self.reconstruir()
