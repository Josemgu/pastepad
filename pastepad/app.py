# -*- coding: utf-8 -*-
"""El panel: junta el modelo, la lista y los dialogos.

No toca el disco ni habla con Windows directamente; para eso estan
modelo.py y windows.py, que se reutilizan tal cual de la version
anterior con sus pruebas.
"""

import queue
import threading
import time
import webbrowser

import flet as ft
import pyperclip

from . import config as cfg
from . import estilo as st
from . import idiomas as idi
from . import filas
from . import modelo
from . import registro
from . import ventanas as vt
from . import windows as win
from .busqueda import Indice


class App:

    def __init__(self, page: ft.Page, almacen):
        self.page = page
        self.almacen = almacen
        self.indice = Indice(almacen)

        # --- estado
        self.pestana = "reciente"
        self.carpeta = None
        self.visibles = []
        self.tipos = []
        self.sel = 0
        self.marcando = False
        self.marcados = set()
        self.destino = None
        self.ultimo_texto = None
        self.visible = True
        self._seq = win.secuencia()
        self._atajo_global = None
        self.cola = queue.Queue()
        # Contadores para el latido: ver _latido().
        self._pulsadas = 0
        self._atendidas = 0
        self._ultimo_latido = None
        self.grupos_abiertos = {"marcadores": True, "notas": True}

        self.atajo = almacen.pref("atajo", cfg.ATAJO_DEF)
        if self.atajo not in cfg.ATAJOS:
            self.atajo = cfg.ATAJO_DEF
        self.pausado = bool(almacen.pref("pausado", False))
        self.idioma = idi.poner(almacen.pref("idioma", idi.IDIOMA_DEF))
        self.tema = almacen.pref("tema", st.TEMA_DEF)
        if self.tema not in st.TEMAS:
            self.tema = st.TEMA_DEF
        self.tamano = almacen.pref("tamano", cfg.TAMANO_DEF)
        if self.tamano not in cfg.TAMANOS:
            self.tamano = cfg.TAMANO_DEF

        self._preparar_ventana()
        self._construir()
        self.refrescar()
        self.registrar_atajo()

        page.run_thread(self._vigilar)
        page.run_thread(self._atender_cola)

    # ------------------------------------------------------ ventana

    def _medidas(self):
        """El tamano guardado, o el del preset si nunca se arrastro.

        Lo arrastrado manda sobre el preset: si el usuario se molesto en
        ajustar la ventana a mano, es lo que quiere ver al abrirla.
        """
        ancho, alto = cfg.TAMANOS[self.tamano]
        ancho = self.almacen.pref("ancho") or ancho
        alto = self.almacen.pref("alto") or alto
        return (max(cfg.MIN_ANCHO, min(int(ancho), cfg.MAX_ANCHO)),
                max(cfg.MIN_ALTO, min(int(alto), cfg.MAX_ALTO)))

    def _preparar_ventana(self):
        p = self.page
        ancho, alto = self._medidas()
        p.title = cfg.APP
        p.window.frameless = True
        p.window.title_bar_hidden = True
        p.window.always_on_top = True
        p.window.skip_task_bar = True
        p.window.resizable = True
        p.window.width, p.window.height = ancho, alto
        p.window.min_width, p.window.min_height = cfg.MIN_ANCHO, cfg.MIN_ALTO
        p.window.max_width, p.window.max_height = cfg.MAX_ANCHO, cfg.MAX_ALTO
        p.window.bgcolor = ft.Colors.TRANSPARENT
        p.bgcolor = ft.Colors.TRANSPARENT
        p.padding = 0
        p.spacing = 0
        p.theme_mode = st.C["modo"]
        p.window.on_event = self._al_evento_ventana
        p.on_keyboard_event = self._al_teclado

    def _al_evento_ventana(self, e):
        if e.type == ft.WindowEventType.BLUR and self.visible:
            # Un clic fuera cierra el panel, como hace Win+V.
            self.ocultar()
        elif e.type == ft.WindowEventType.RESIZED:
            self._guardar_medidas()
        elif e.type == ft.WindowEventType.CLOSE:
            # Ultima oportunidad de bajar a disco lo que quede pendiente.
            self.almacen.volcar(True)

    def _guardar_medidas(self):
        """Recuerda el tamano al que se dejo la ventana.

        Se engancha a RESIZED y no a RESIZE: el primero llega al soltar
        el borde, el segundo en cada pixel del arrastre, y eso serian
        cincuenta escrituras del JSON por segundo.
        """
        p = self.page
        ancho, alto = p.window.width, p.window.height
        if not ancho or not alto:
            return
        ancho, alto = int(ancho), int(alto)
        if (ancho, alto) == (self.almacen.pref("ancho"),
                             self.almacen.pref("alto")):
            return
        self.almacen.poner_pref("ancho", ancho)
        self.almacen.poner_pref("alto", alto)

    def _al_teclado(self, e: ft.KeyboardEvent):
        if e.key == "Escape":
            self.ocultar()
        elif e.key == "Arrow Down":
            self._mover(1)
        elif e.key == "Arrow Up":
            self._mover(-1)

    def mostrar(self):
        """Saca el panel junto al puntero y le da el foco.

        self.visible se pone en la ultima linea a proposito. Estaba
        arriba, antes de refrescar() y de los dos update(), que son las
        tres cosas que pueden lanzar; si alguna lo hacia, quien llamo se
        comia la excepcion y la bandera quedaba en True con la ventana
        sin abrir. A partir de ahi alternar() leia True y llamaba a
        ocultar() en cada pulsacion: el atajo respondia una sola vez y
        no volvia a abrir nunca. Dejandola al final, un fallo la deja en
        False y la siguiente pulsacion vuelve a intentarlo.
        """
        if self.destino is None:
            self.destino = win.ventana_activa()
        x, y = self._sitio()
        p = self.page
        p.window.left, p.window.top = x, y
        p.window.visible = True
        self.buscador.value = ""
        self.refrescar()
        p.window.focused = True
        p.update()
        self.buscador.focus()
        self.visible = True

    def _sitio(self):
        """Junto al puntero, sin salirse de la pantalla ni tapar la
        barra de tareas."""
        ancho = self.page.window.width or 380
        alto = self.page.window.height or 560
        px, py = win.puntero()
        ancho_p, alto_p = win.pantalla()
        izq, arr, der, aba = win.area_util(px, py, ancho_p, alto_p)
        x = px + 14 if px + 14 + ancho <= der else px - ancho - 14
        y = py + 18 if py + 18 + alto <= aba else py - alto - 18
        return (max(izq + 8, min(int(x), der - ancho - 8)),
                max(arr + 8, min(int(y), aba - alto - 8)))

    def ocultar(self):
        self.visible = False
        self.destino = None
        self.page.window.visible = False
        self.page.update()

    def alternar(self):
        self.mostrar() if not self.visible else self.ocultar()

    # ------------------------------------------------------ interfaz

    def _construir(self):
        self.buscador = st.campo(idi.t("Buscar en todo"), self._al_buscar,
                                 self._al_enviar,
                                 icono_nombre=ft.Icons.SEARCH,
                                 alto=st.ALTO_BUSCADOR)
        self.lista = ft.ListView(
            spacing=st.SEP_FILA,
            padding=ft.Padding.symmetric(horizontal=st.E4, vertical=st.E1),
            expand=True, auto_scroll=False)

        self.tab_reciente = st.pildora(idi.t("Reciente"),
                                       lambda e: self.cambiar("reciente"),
                                       True)
        self.tab_guardados = st.pildora(idi.t("Guardados"),
                                        lambda e: self.cambiar("guardados"))
        self.barra_carpetas = ft.Container(height=0, animate=ft.Animation(
            160, ft.AnimationCurve.EASE_OUT))
        self.pie = ft.Container()

        self.aviso_pausa = st.texto("● En pausa", st.T_MINI,
                                    filas.ROJO, ft.FontWeight.W_500)
        self.aviso_pausa.visible = self.pausado
        self.boton_pausa = st.icono(
            ft.Icons.PLAY_ARROW if self.pausado else ft.Icons.PAUSE,
            lambda e: self.alternar_pausa(), 17,
            filas.ROJO if self.pausado else None,
            idi.t("Reanudar la captura") if self.pausado else idi.t("Pausar la captura"),
            True)

        cabecera = ft.WindowDragArea(
            ft.Container(
                content=ft.Row([
                    st.texto(cfg.ATAJOS.get(self.atajo, self.atajo),
                             st.T_MINI, st.C["tenue"]),
                    ft.Container(width=st.E3),
                    self.aviso_pausa,
                    ft.Container(expand=True),
                    self.boton_pausa,
                    st.icono(ft.Icons.PALETTE_OUTLINED,
                             lambda e: self.abrir_apariencia(), 17, None,
                             idi.t("Apariencia"), True),
                    st.icono(ft.Icons.CLOSE, lambda e: self.ocultar(), 17,
                             None, idi.t("Cerrar"), True),
                ], spacing=st.E1),
                padding=ft.Padding.only(left=st.E4, right=st.E2, top=st.E2)),
            maximizable=False)

        self.raiz = ft.Container(
            content=ft.Column([
                cabecera,
                ft.Container(content=self.buscador,
                             padding=ft.Padding.symmetric(horizontal=st.E4,
                                                          vertical=st.E2)),
                ft.Container(content=ft.Row([self.tab_reciente,
                                             self.tab_guardados],
                                            spacing=st.E1),
                             padding=ft.Padding.only(left=st.E4, right=st.E4)),
                self.barra_carpetas,
                self.lista,
                self.pie,
            ], spacing=0, expand=True),
            bgcolor=st.C["fondo"], border_radius=st.R_PANEL,
            border=ft.Border.all(1, st.C["borde"]), expand=True)

        self.page.add(self.raiz)
        self._pintar_pie()

    def _pintar_pie(self):
        # Con la ventana estrecha no caben icono + dos botones con texto:
        # el ultimo se salia por la derecha y quedaba cortado. Al apretar,
        # "Nuevo" se queda solo con el signo mas.
        apretado = self._compacta()
        etiqueta_nuevo = "" if apretado else idi.t("Nuevo")
        lado = 30 if apretado else 36
        if self.marcando:
            hijos = [
                st.boton(idi.t("Todos"), lambda e: self.marcar_todos()),
                st.boton("Borrar (%d)" % len(self.marcados),
                         lambda e: self.borrar_marcados(), "peligro"),
                ft.Container(expand=True),
                st.boton(idi.t("Cancelar"), lambda e: self.alternar_marcado()),
            ]
        elif self.pestana == "guardados":
            hijos = [
                st.icono(ft.Icons.CREATE_NEW_FOLDER,
                         lambda e: self.nueva_carpeta(), 18, filas.AMBAR,
                         idi.t("Nueva carpeta"), False, lado),
                st.icono(ft.Icons.PLAYLIST_ADD,
                         lambda e: self.agregar_lista(), 18, None,
                         idi.t("Agregar una lista"), False, lado),
            ]
            # En mini no cabe: la maqueta 16 deja solo el icono y Nuevo.
            if not apretado:
                hijos.append(st.boton(idi.t("Seleccionar"),
                                      lambda e: self.alternar_marcado()))
            hijos += [
                ft.Container(expand=True),
                st.boton(etiqueta_nuevo, lambda e: self.nuevo(), "acento",
                         ft.Icons.ADD),
            ]
        else:
            hijos = [
                st.icono(ft.Icons.CLEANING_SERVICES_OUTLINED,
                         lambda e: self.vaciar(), 18, None,
                         idi.t("Vaciar el historial"), False, lado),
            ]
            if not apretado:
                hijos.append(st.boton(idi.t("Seleccionar"),
                                      lambda e: self.alternar_marcado()))
            hijos += [
                ft.Container(expand=True),
                st.boton(etiqueta_nuevo, lambda e: self.nuevo(), "acento",
                         ft.Icons.ADD),
            ]
        self.pie.content = ft.Column([
            # La linea de las maquetas: separa el pie de la lista sin
            # tener que meterle un fondo distinto.
            ft.Container(height=1, bgcolor=st.C["borde"],
                         margin=ft.Margin.only(left=st.E4, right=st.E4,
                                               bottom=st.E3)),
            ft.Row(hijos, spacing=st.E2,
                   vertical_alignment=ft.CrossAxisAlignment.CENTER),
        ], spacing=0, tight=True)
        self.pie.padding = ft.Padding.only(left=st.E4, right=st.E4,
                                           bottom=st.E3)

    def _pintar_carpetas(self):
        if self.pestana != "guardados":
            self.barra_carpetas.height = 0
            self.barra_carpetas.content = None
            return
        # 58 y no 46: los 12 de arriba son el aire que separa la barra de
        # las pestanas. Pegadas parecian un solo bloque.
        self.barra_carpetas.height = 58
        self.barra_carpetas.content = self._carpetas_menu()

    def _carpetas_menu(self):
        opciones = [filas._item(idi.t("Todas las carpetas"), ft.Icons.FOLDER_OPEN,
                                lambda e: self.elegir_carpeta(None))]
        if self.almacen.carpetas:
            opciones.append(ft.PopupMenuItem())
        for nombre in self.almacen.carpetas:
            opciones.append(filas._item(
                nombre,
                ft.Icons.CHECK if nombre == self.carpeta else ft.Icons.FOLDER,
                lambda e, n=nombre: self.elegir_carpeta(n)))
        opciones.append(ft.PopupMenuItem())
        opciones.append(filas._item(idi.t("Nueva carpeta..."),
                                    ft.Icons.CREATE_NEW_FOLDER_OUTLINED,
                                    lambda e: self.nueva_carpeta()))
        if self.carpeta:
            opciones.append(filas._item("Editar %s..." % self.carpeta,
                                        ft.Icons.TUNE,
                                        lambda e: self.editar_carpeta()))
            opciones.append(filas._item("Renombrar %s" % self.carpeta,
                                        ft.Icons.DRIVE_FILE_RENAME_OUTLINE,
                                        lambda e: self.renombrar_carpeta()))
            opciones.append(filas._item("Eliminar %s y su contenido"
                                        % self.carpeta,
                                        ft.Icons.FOLDER_DELETE_OUTLINED,
                                        lambda e: self.borrar_carpeta()))
        etiqueta = self.carpeta or idi.t("Todas las carpetas")
        return ft.Container(
            content=ft.PopupMenuButton(
                items=opciones,
                content=ft.Container(
                    content=ft.Row([
                        # La carpeta va siempre en ambar, encendida o no:
                        # es la senia por la que se reconoce de un vistazo.
                        ft.Icon(ft.Icons.FOLDER, size=15,
                                color=filas.AMBAR),
                        st.texto(etiqueta, st.T_MENOR,
                                 st.C["sobre"] if self.carpeta
                                 else st.C["medio"]),
                        ft.Container(expand=True),
                        ft.Icon(ft.Icons.EXPAND_MORE, size=17,
                                color=st.C["sobre"] if self.carpeta
                                else st.C["medio"]),
                    ], spacing=st.E2),
                    bgcolor=st.C["acento"] if self.carpeta
                    else st.C["tarjeta"],
                    padding=ft.Padding.symmetric(horizontal=14, vertical=8),
                    border_radius=10),
                bgcolor=st.C["elevado"],
                shape=ft.RoundedRectangleBorder(radius=12)),
            padding=ft.Padding.only(left=st.E4, right=st.E4,
                                    top=st.E3, bottom=st.E2))

    # ------------------------------------------------------ lista

    def cambiar(self, cual):
        self.pestana = cual
        self._pintar_pestanas()
        if self.marcando:
            self.marcando = False
            self.marcados.clear()
        self.refrescar()

    def _pintar_pestanas(self):
        """Colorea la pestana activa.

        Buscando no se resalta ninguna: la busqueda cruza las dos, y
        dejar una encendida hacia creer que solo miraba ahi.
        """
        buscando = bool((self.buscador.value or "").strip())
        reciente = self.pestana == "reciente"
        for tab, activa in ((self.tab_reciente, reciente and not buscando),
                            (self.tab_guardados,
                             not reciente and not buscando)):
            tab.bgcolor = st.C["acento"] if activa else st.C["tarjeta"]
            tab.content.color = st.C["sobre"] if activa else st.C["medio"]

    def elegir_carpeta(self, nombre):
        self.carpeta = nombre
        self.refrescar()

    def _al_buscar(self, e):
        self.refrescar()

    def _al_enviar(self, e):
        self.pegar()

    def refrescar(self):
        consulta = (self.buscador.value or "").strip()
        if consulta:
            items = self.indice.buscar(consulta)
            aviso, icono = idi.t("Nada coincide con esa busqueda"), ft.Icons.SEARCH
        elif self.pestana == "guardados":
            items = [(s, "g") for s in self.almacen.snippets
                     if not self.carpeta or s["categoria"] == self.carpeta]
            aviso = idi.t("Vacio. Usa Nuevo para guardar un texto")
            icono = ft.Icons.FOLDER_OPEN_OUTLINED
        else:
            items = [(h, "h") for h in self.almacen.hist_ordenado()]
            aviso = idi.t("Copia algo y aparecera aqui")
            icono = ft.Icons.CONTENT_PASTE_OFF

        self.visibles = [d for d, _ in items]
        self.tipos = [t for _, t in items]
        if self.sel >= len(items):
            self.sel = 0

        compacta = self._compacta()

        def tarjeta(i, d, t):
            return filas.Fila(d, t, i == self.sel, self.marcando,
                              id(d) in self.marcados, self._accion, compacta)

        if not items:
            self.lista.controls = [filas.vacio(aviso, icono)]
        elif self.pestana == "guardados" and not consulta:
            self.lista.controls = self._lista_agrupada(items, tarjeta)
        else:
            self.lista.controls = [tarjeta(i, d, t)
                                   for i, (d, t) in enumerate(items)]

        self._pintar_pestanas()
        self._pintar_carpetas()
        self._pintar_pie()
        self.page.update()

    def _lista_agrupada(self, items, tarjeta):
        """Guardados en dos grupos plegables: marcadores y notas.

        Un marcador se abre en el navegador y una nota se pega: son dos
        gestos distintos y mezclarlos obligaba a leer la lista entera
        para encontrar cualquiera de los dos.
        """
        marcadores, notas = [], []
        for i, (d, t) in enumerate(items):
            texto = modelo.texto_de(d.get("runs", []))
            (marcadores if modelo.es_enlace(texto) else notas).append(
                (i, d, t))

        controles = []
        for clave, etiqueta, grupo, icono, color in (
                ("marcadores", idi.t("Marcadores"), marcadores,
                 ft.Icons.BOOKMARK, st.C["acento"]),
                ("notas", idi.t("Notas"), notas,
                 ft.Icons.STICKY_NOTE_2_OUTLINED, None)):
            if not grupo:
                continue
            # Con un solo grupo la cabecera sobra: no hay nada que separar.
            if marcadores and notas:
                abierto = self.grupos_abiertos.get(clave, True)
                controles.append(filas.cabecera_grupo(
                    etiqueta, len(grupo), abierto,
                    lambda e, c=clave: self._alternar_grupo(c),
                    icono, color))
                if not abierto:
                    continue
            controles += [tarjeta(i, d, t) for i, d, t in grupo]
        return controles

    def _alternar_grupo(self, clave):
        self.grupos_abiertos[clave] = not self.grupos_abiertos.get(clave,
                                                                   True)
        self.refrescar()

    def _compacta(self):
        """True cuando la ventana esta tan estrecha que toca apretar.

        Se mira el ancho real y no la preferencia de tamano: desde que
        se puede arrastrar el borde, el preset ya no dice la verdad.
        """
        return (self.page.window.width or cfg.TAMANOS[self.tamano][0]) < 340

    def _mover(self, paso):
        if not self.visibles:
            return
        self.sel = max(0, min(len(self.visibles) - 1, self.sel + paso))
        self.refrescar()

    def actual(self):
        if self.sel < len(self.visibles):
            return self.visibles[self.sel], self.tipos[self.sel]
        return None, None

    def _accion(self, que, dato):
        if que == "elegir":
            self.sel = self.visibles.index(dato)
            if self.marcando:
                self.marcados.symmetric_difference_update({id(dato)})
                self.refrescar()
            else:
                self.pegar()
        elif que == "abrir":
            self.abrir_enlace(dato)
        elif que == "pegar":
            self.sel = self.visibles.index(dato)
            self.pegar()
        elif que == "pegar_plano":
            self.sel = self.visibles.index(dato)
            self.pegar(True)
        elif que == "copiar":
            self.copiar(dato)
        elif que == "fijar":
            self.almacen.fijar(dato)
            self.indice.invalidar()
            self.refrescar()
        elif que == "editar":
            self.editar(dato)
        elif que == "borrar":
            self.almacen.borrar(dato)
            self.indice.invalidar()
            self.refrescar()

    # ------------------------------------------------------ pegar

    def _texto_de(self, dato, tipo):
        return (dato.get("texto", "") if tipo == "h"
                else modelo.texto_de(dato.get("runs", [])))

    def abrir_enlace(self, dato):
        tipo = "h" if dato.get("tipo") else "g"
        texto = self._texto_de(dato, tipo)
        self.ocultar()
        try:
            webbrowser.open(modelo.url_de(texto))
        except Exception:
            registro.fallo("abrir_enlace")

    def copiar(self, dato):
        """Deja el texto en el portapapeles sin pegarlo."""
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

    def pegar(self, sin_formato=False):
        dato, tipo = self.actual()
        if dato is None:
            return
        texto = self._texto_de(dato, tipo)
        if modelo.es_enlace(texto):
            # Un enlace se abre, que es lo que espera cualquiera al
            # hacerle clic. Para pegarlo esta el menu.
            self.abrir_enlace(dato)
            return

        if tipo == "h":
            if dato.get("tipo") == "imagen":
                if not win.copiar_imagen(dato.get("ruta", "")):
                    return
                self.ultimo_texto = None
            else:
                pyperclip.copy(texto)
                self.ultimo_texto = texto
        else:
            fragmentos = dato["runs"]
            campos = modelo.campos_de(modelo.texto_de(fragmentos))
            if campos:
                self.pedir_campos(dato, fragmentos, campos, sin_formato)
                return
            win.copiar(fragmentos, modelo.texto_de, sin_formato)
            self.ultimo_texto = modelo.texto_de(fragmentos)

        self._seq = win.secuencia()
        destino = self.destino
        self.ocultar()
        threading.Thread(target=self._enviar, args=(destino,),
                         daemon=True).start()

    @staticmethod
    def _enviar(destino):
        """Devuelve el foco a la ventana de antes y manda Ctrl+V.

        Sin lo primero el pegado se va al vacio: al abrirse el panel, el
        campo donde estaba el cursor perdio el foco.
        """
        time.sleep(0.10)
        win.devolver_foco(destino)
        time.sleep(0.16)
        if not win.pegar_con_teclado():
            registro.anotar("no se pudo mandar Ctrl+V", "_enviar")

    # ------------------------------------------------------ vigilancia

    def alternar_pausa(self):
        self.pausado = not self.pausado
        self.almacen.poner_pref("pausado", self.pausado)
        if not self.pausado:
            self._seq = win.secuencia()
        self._pintar_pausa()

    def _pintar_pausa(self):
        """Refleja el estado de pausa en la cabecera.

        Hay que tocar las propiedades del boton una a una: se creo con
        el icono ya resuelto, y un page.update() a secas no lo recalcula.
        Antes solo se hacia eso, y el boton se quedaba siempre en pausa.
        """
        self.boton_pausa.icon = (ft.Icons.PLAY_ARROW if self.pausado
                                 else ft.Icons.PAUSE)
        self.boton_pausa.icon_color = filas.ROJO if self.pausado \
            else st.C["medio"]
        self.boton_pausa.tooltip = (idi.t("Reanudar la captura") if self.pausado
                                    else idi.t("Pausar la captura"))
        self.aviso_pausa.visible = self.pausado
        self.page.update()

    def _vigilar(self):
        """Mira el portapapeles, pero solo de verdad cuando cambio.

        Corre en su propio hilo: el contador de Windows es barato, abrir
        el portapapeles no.

        Los fallos se anotan aqui dentro y no via threading.excepthook:
        page.run_thread entrega esto a un ThreadPoolExecutor, que guarda
        la excepcion en su Future y no la deja llegar a ningun gancho.
        """
        while True:
            time.sleep(0.7)
            if cfg.TRAZA_ATAJO:
                self._latido()
            # Aqui se paga la escritura diferida, fuera del camino de la
            # copia: anotar() ya no toca el disco.
            self.almacen.volcar()
            if self.pausado:
                continue
            try:
                seq = win.secuencia()
                if seq is not None and seq == self._seq:
                    continue
                self._seq = seq
                tipo, dato = win.leer()
                if tipo == "privado":
                    continue          # alguien pidio que no se guarde
                if tipo == "texto" and dato and dato.strip():
                    if len(dato) > cfg.MAX_CARACTERES:
                        dato = dato[:cfg.MAX_CARACTERES]
                    if dato != self.ultimo_texto:
                        self.ultimo_texto = dato
                        if self.almacen.anotar({"tipo": "texto",
                                                "texto": dato}):
                            self._tras_anotar()
                elif tipo == "imagen" and dato:
                    marca = "img%d" % len(dato)
                    if marca != self.ultimo_texto:
                        self.ultimo_texto = marca
                        if self.almacen.guardar_imagen(dato, win.dib_a_bmp):
                            self._tras_anotar()
            except Exception:
                # Sin repetir: este bucle reintenta cada 0,7 s y un fallo
                # persistente escribiria cinco mil copias en una tarde.
                registro.fallo("_vigilar", repetir=False)

    def _latido(self):
        """Testigo del hilo del atajo, escrito desde el vigilante.

        _vigilar corre cada 0,7 s y no toca la interfaz, asi que sigue
        vivo aunque _atender_cola se cuelgue: por eso es el que puede
        contarlo. Solo escribe cuando algun contador cambia.

        Como leerlo cuando el atajo deja de responder:

        - pulsadas sube y atendidas se queda quieta -> alternar() no
          volvio. El hilo esta colgado dentro de Flet.
        - no sube ninguna de las dos -> WM_HOTKEY no esta llegando: mira
          si otro programa se quedo con la combinacion.
        - suben las dos y aun asi no se ve el panel -> el problema esta
          en la ventana, no en el atajo.

        Con esto se cerro el fallo del atajo que moria tras unas pocas
        pulsaciones: las dos subian a la par y luego se paraban de golpe,
        lo que descarto la interfaz y senalo al hook de keyboard.
        """
        estado = (self._pulsadas, self._atendidas)
        if estado == self._ultimo_latido:
            return
        self._ultimo_latido = estado
        registro.anotar(
            "pulsadas=%d  atendidas=%d  en_cola=%d  visible=%s"
            % (self._pulsadas, self._atendidas, self.cola.qsize(),
               self.visible), "_latido")

    def _tras_anotar(self):
        self.indice.invalidar()
        if self.visible and self.pestana == "reciente" \
                and not (self.buscador.value or "").strip():
            self.refrescar()

    # ------------------------------------------------------ atajo

    def registrar_atajo(self, combinacion=None):
        """Pone el atajo global. False si Windows no lo acepta.

        Devolver False importa: es lo que impide guardar como preferencia
        una combinacion que otro programa ya tiene cogida.
        """
        combinacion = combinacion or self.atajo
        if self._atajo_global is None:
            self._atajo_global = win.AtajoGlobal(self._pulsado,
                                                 registro.anotar)
        if self._atajo_global.poner(combinacion):
            self.atajo = combinacion
            registro.anotar("atajo registrado: %s" % combinacion,
                            "registrar_atajo")
            return True
        registro.anotar(
            "Windows rechazo '%s'. Suele ser que otro programa ya lo "
            "tiene cogido." % combinacion, "registrar_atajo")
        return False

    def _pulsado(self):
        """Corre dentro del hilo del teclado.

        Aqui no se hace nada mas que anotar el aviso. Si este callback
        tarda, la libreria que escucha el teclado se atasca y el atajo
        deja de responder despues de la primera vez.
        """
        # Solo un contador en memoria: nada de escribir al log aqui. Este
        # callback tiene que salir en milisegundos o Windows desengancha
        # el hook de teclado, que es una de las cosas que se investigan.
        self._pulsadas += 1
        try:
            self.cola.put_nowait(win.ventana_activa())
        except Exception:
            registro.fallo("_pulsado")

    def _atender_cola(self):
        """Recoge los avisos del atajo y actua, ya fuera de ese hilo.

        Igual que _vigilar: corre en el executor de Flet, asi que si algo
        revienta hay que anotarlo aqui o no queda rastro en ningun sitio.
        """
        while True:
            try:
                hwnd = self.cola.get()
                if cfg.TRAZA_ATAJO:
                    # La comparacion que importa: si self.visible dice una
                    # cosa y la ventana real dice otra, la bandera se
                    # desincronizo y el panel ya no vuelve a abrirse.
                    registro.anotar(
                        "pulsacion: self.visible=%s  window.visible=%s  "
                        "destino=%s" % (self.visible,
                                        self.page.window.visible, hwnd),
                        "_atender_cola")
                if not self.visible:
                    self.destino = hwnd
                self.alternar()
                # Solo se incrementa si alternar() VOLVIO. Si el log
                # muestra pulsadas subiendo y atendidas clavada, es que
                # se quedo colgado ahi dentro.
                self._atendidas += 1
            except Exception:
                registro.fallo("_atender_cola")
                time.sleep(0.2)

    # ------------------------------------------------------ acciones

    def nuevo(self):
        vt.texto_nuevo(self.page, self.almacen.carpetas, self._guardar_nuevo)

    def _guardar_nuevo(self, snippet):
        self.almacen.anadir_snippet(snippet)
        self.indice.invalidar()
        self.cambiar("guardados")

    def editar(self, dato):
        if dato.get("tipo") == "texto":
            # Editar algo del historial lo pasa a los guardados.
            vt.texto_nuevo(self.page, self.almacen.carpetas,
                           self._guardar_nuevo, inicial=dato["texto"])
            return

        def reemplazar(nuevo):
            self.almacen.reemplazar_snippet(dato, nuevo)
            self.indice.invalidar()
            self.refrescar()
        vt.texto_nuevo(self.page, self.almacen.carpetas, reemplazar, dato)

    def pedir_campos(self, dato, fragmentos, campos, sin_formato):
        def con_valores(valores):
            listos = modelo.rellenar(fragmentos, valores)
            win.copiar(listos, modelo.texto_de, sin_formato)
            self.ultimo_texto = modelo.texto_de(listos)
            self._seq = win.secuencia()
            destino = self.destino
            self.ocultar()
            threading.Thread(target=self._enviar, args=(destino,),
                             daemon=True).start()
        vt.campos(self.page, campos, con_valores)

    def vaciar(self):
        def hacer():
            self.almacen.vaciar_historial()
            self.indice.invalidar()
            self.refrescar()
        vt.confirmar(self.page,
                     idi.t("Vaciar el historial? Los fijados se quedan."), hacer)

    def nueva_carpeta(self):
        def crear(nombre):
            self.almacen.crear_carpeta(nombre)
            self.carpeta = nombre
            self.pestana = "guardados"
            self.cambiar("guardados")
            self.agregar_lista(nombre)
        vt.una_linea(self.page, idi.t("Nueva carpeta"), idi.t("Nombre de la carpeta"),
                     crear)

    def renombrar_carpeta(self):
        if not self.carpeta:
            return
        viejo = self.carpeta

        def renombrar(nuevo):
            if self.almacen.renombrar_carpeta(viejo, nuevo):
                self.carpeta = nuevo
                self.indice.invalidar()
                self.refrescar()
        vt.una_linea(self.page, idi.t("Renombrar carpeta"), idi.t("Nuevo nombre"),
                     renombrar, viejo)

    def editar_carpeta(self):
        """Renombrar y limpiar la carpeta en un solo paso."""
        if not self.carpeta:
            return
        viejo = self.carpeta
        contenido = self.almacen.contenido_de(viejo)

        def aplicar(nuevo, fuera):
            if fuera:
                self.almacen.borrar_varios(fuera)
            if nuevo and nuevo != viejo:
                if self.almacen.renombrar_carpeta(viejo, nuevo):
                    self.carpeta = nuevo
            self.indice.invalidar()
            self.refrescar()
        vt.editar_carpeta(self.page, viejo, contenido, aplicar)

    def borrar_carpeta(self):
        if not self.carpeta:
            return
        nombre = self.carpeta
        cuantos = len(self.almacen.contenido_de(nombre))
        aviso = ("Eliminar la carpeta %s y sus %d texto%s? Esto no se puede "
                 "deshacer." % (nombre, cuantos, "" if cuantos == 1 else "s")
                 if cuantos else "Eliminar la carpeta %s?" % nombre)

        def hacer():
            self.almacen.borrar_carpeta(nombre)
            self.carpeta = None
            self.indice.invalidar()
            self.refrescar()
        vt.confirmar(self.page, aviso, hacer)

    def agregar_lista(self, carpeta=None):
        carpeta = carpeta or self.carpeta
        if not carpeta:
            vt.confirmar(self.page, idi.t("Elige primero una carpeta."),
                         lambda: None, False)
            return

        def anadir(lineas):
            for texto in lineas:
                primera = texto.strip().splitlines()[0]
                self.almacen.anadir_snippet({
                    "titulo": modelo.una_linea(primera, 48),
                    "categoria": carpeta,
                    "runs": [modelo.fragmento(texto)]})
            self.indice.invalidar()
            self.cambiar("guardados")
        vt.lista_masiva(self.page, carpeta, anadir)

    # ------------------------------------------------------ marcado

    def alternar_marcado(self):
        self.marcando = not self.marcando
        self.marcados.clear()
        self.refrescar()

    def marcar_todos(self):
        if len(self.marcados) == len(self.visibles):
            self.marcados.clear()
        else:
            self.marcados = {id(d) for d in self.visibles}
        self.refrescar()

    def borrar_marcados(self):
        elegidos = [d for d in self.visibles if id(d) in self.marcados]
        if not elegidos:
            return

        def hacer():
            self.almacen.borrar_varios(elegidos)
            self.indice.invalidar()
            self.marcando = False
            self.marcados.clear()
            self.refrescar()
        vt.confirmar(self.page, "Borrar %d elemento%s? Esto no se puede "
                                "deshacer." % (len(elegidos),
                                               "" if len(elegidos) == 1
                                               else "s"), hacer)

    # ------------------------------------------------------ apariencia

    def abrir_apariencia(self):
        vt.apariencia(self.page, st.C.get("nombre", "menta"),
                      self.atajo, self._aplicar_apariencia,
                      self.tema, self.idioma)

    def _aplicar_apariencia(self, acento, atajo, tema="auto",
                            idioma="es"):
        self.almacen.poner_pref("acento", acento)
        if tema != self.tema:
            self.tema = tema
            self.almacen.poner_pref("tema", tema)
        if idioma != self.idioma:
            self.idioma = idi.poner(idioma)
            self.almacen.poner_pref("idioma", self.idioma)
        st.aplicar(acento, self.tema)
        self.page.theme_mode = st.C["modo"]

        if atajo != self.atajo and self.registrar_atajo(atajo):
            self.almacen.poner_pref("atajo", atajo)

        # Los colores viven dentro de los controles, asi que hay que
        # rehacerlos: se destruye el arbol y se vuelve a montar.
        self.page.controls.clear()
        self._construir()
        self.refrescar()
