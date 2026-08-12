# -*- coding: utf-8 -*-
"""Las ventanas que se abren encima del panel.

En Flet son dialogos de la propia pagina, no ventanas del sistema: asi
heredan el tema y las animaciones sin trabajo extra.
"""

import flet as ft

from . import config as cfg
from . import estilo as st
from . import modelo


def _marco(page, titulo, cuerpo, acciones, ancho=460):
    """El armazon comun de todos los dialogos.

    El ancho se recorta al de la ventana. Los valores de las maquetas
    (460, 480) estan pensados para el panel grande; en uno de 380 el
    dialogo se salia y Flutter recortaba el texto de dentro, que es por
    lo que salia "Lista des..." en vez de "Lista desplegable".
    """
    disponible = (page.window.width or ancho) - 2 * st.E4
    return ft.AlertDialog(
        modal=True,
        bgcolor=st.C["elevado"],
        shape=ft.RoundedRectangleBorder(radius=st.R_PANEL),
        title=st.texto(titulo, st.T_TITULO, st.C["texto"],
                       ft.FontWeight.W_500),
        content=ft.Container(content=cuerpo,
                             width=max(240, min(ancho, disponible))),
        actions=acciones,
        actions_alignment=ft.MainAxisAlignment.END,
        content_padding=ft.Padding.symmetric(horizontal=st.E4,
                                             vertical=st.E2))


def abrir(page, dialogo):
    """En esta version de Flet los dialogos se apilan con show_dialog."""
    page.show_dialog(dialogo)


def cerrar(page, dialogo=None):
    page.pop_dialog()


def texto_nuevo(page, carpetas, al_guardar, snippet=None, inicial=""):
    """Crear o editar un texto guardado. Sin campos obligatorios."""
    opciones = list(carpetas) or ["Mis textos"]
    elegida = ft.Dropdown(
        value=snippet["categoria"] if snippet else opciones[0],
        options=[ft.DropdownOption(c) for c in opciones],
        width=200, border_radius=st.R_CONTROL, filled=True,
        bgcolor=st.C["tarjeta"], border_color=ft.Colors.TRANSPARENT,
        focused_border_color=st.C["acento"], text_size=st.T_MENOR,
        color=st.C["texto"],
        content_padding=ft.Padding.symmetric(horizontal=12, vertical=10))

    valor = (modelo.texto_de(snippet["runs"]) if snippet else inicial)
    caja = st.campo("Escribe o pega aqui", valor=valor, lineas=10)

    # Un marcador no es otro tipo de dato: es un guardado cuyo texto es
    # solo una URL. Lo unico que cambia es el formulario, y por eso el
    # titulo si se pide a mano: "https://..." no sirve de nombre.
    enlace_url = st.campo("https://ejemplo.com/pagina")
    enlace_titulo = st.campo("Como quieres llamarlo")
    es_marcador = modelo.es_enlace(valor)
    if es_marcador:
        enlace_url.value = valor.strip()
        enlace_titulo.value = snippet["titulo"] if snippet else ""

    modo = {"enlace": es_marcador}
    zona_texto = ft.Column([caja], tight=True, visible=not es_marcador)
    zona_enlace = ft.Column([
        st.texto("Direccion", st.T_MINI, st.C["medio"]), enlace_url,
        st.texto("Titulo", st.T_MINI, st.C["medio"]), enlace_titulo,
    ], spacing=st.E1, tight=True, visible=es_marcador)

    tipos = ft.Row(spacing=st.E1)

    def pintar_tipos():
        tipos.controls = [
            st.pildora("Texto", lambda e: poner_tipo(False),
                       not modo["enlace"]),
            st.pildora("Enlace", lambda e: poner_tipo(True),
                       modo["enlace"]),
        ]

    def poner_tipo(enlace):
        modo["enlace"] = enlace
        zona_texto.visible = not enlace
        zona_enlace.visible = enlace
        pintar_tipos()
        page.update()

    pintar_tipos()

    def guardar(e):
        carpeta = elegida.value or "Mis textos"
        if modo["enlace"]:
            url = (enlace_url.value or "").strip()
            if url:
                if not modelo.es_enlace(url):
                    url = "https://" + url.lstrip("/")
                titulo = (enlace_titulo.value or "").strip()
                al_guardar({"titulo": titulo or modelo.dominio_de(url),
                            "categoria": carpeta,
                            "runs": [modelo.fragmento(url)]})
        else:
            contenido = (caja.value or "").strip()
            if contenido:
                primera = contenido.splitlines()[0]
                al_guardar({"titulo": modelo.una_linea(primera, 48),
                            "categoria": carpeta,
                            "runs": [modelo.fragmento(caja.value)]})
        cerrar(page)

    cuerpo = ft.Column([
        tipos,
        ft.Row([st.texto("Guardar en", st.T_MENOR, st.C["medio"]), elegida],
               spacing=st.E3),
        zona_texto,
        zona_enlace,
        st.texto("Escribe [[algo]] y el programa te lo preguntara al pegar",
                 st.T_MINI, st.C["tenue"]),
    ], spacing=st.E3, tight=True)

    d = _marco(page, "Nuevo texto" if not snippet else "Editar texto", cuerpo,
               [st.boton("Cancelar", lambda e: cerrar(page)),
                st.boton("Guardar", guardar, "acento")])
    abrir(page, d)
    return d


def una_linea(page, titulo, etiqueta, al_aceptar, valor=""):
    # La etiqueta va encima y no dentro como marcador: al renombrar, el
    # campo llega con el nombre viejo escrito y un marcador no se veria.
    caja = st.campo("", valor=valor)

    def aceptar(e):
        v = (caja.value or "").strip()
        cerrar(page)
        if v:
            al_aceptar(v)

    caja.on_submit = aceptar
    cuerpo = ft.Column([st.texto(etiqueta, st.T_MINI, st.C["medio"]), caja],
                       spacing=st.E1, tight=True)
    d = _marco(page, titulo, cuerpo,
               [st.boton("Cancelar", lambda e: cerrar(page)),
                st.boton("Aceptar", aceptar, "acento")], 360)
    abrir(page, d)
    return d


def campos(page, nombres, al_aceptar):
    """Rellena los [[campos]] de una plantilla antes de pegar."""
    cajas = {n: st.campo(n) for n in nombres}

    def aceptar(e):
        valores = {n: (c.value or "") for n, c in cajas.items()}
        cerrar(page)
        al_aceptar(valores)

    cuerpo = ft.Column(
        [ft.Column([st.texto(n, st.T_MINI, st.C["medio"]), cajas[n]],
                   spacing=st.E1, tight=True) for n in nombres],
        spacing=st.E3, tight=True, scroll=ft.ScrollMode.AUTO)
    d = _marco(page, "Completar antes de pegar", cuerpo,
               [st.boton("Cancelar", lambda e: cerrar(page)),
                st.boton("Pegar", aceptar, "acento")], 380)
    abrir(page, d)
    return d


def lista_masiva(page, carpeta, al_aceptar):
    """Pega varias cosas: una nota por linea, o todo junto."""
    caja = st.campo("Pega aqui tu lista", lineas=9)
    modo = ft.RadioGroup(
        value="separado",
        content=ft.Column([
            ft.Radio(value="separado", label="Una nota por cada linea",
                     fill_color=st.C["acento"], label_style=ft.TextStyle(
                         size=st.T_MENOR, color=st.C["texto"])),
            ft.Radio(value="junto", label="Todo junto en una sola nota",
                     fill_color=st.C["acento"], label_style=ft.TextStyle(
                         size=st.T_MENOR, color=st.C["texto"])),
        ], spacing=0, tight=True))
    limpiar = ft.Checkbox(
        label="Quitar numeracion y vinetas", value=True,
        fill_color=st.C["acento"],
        label_style=ft.TextStyle(size=st.T_MENOR, color=st.C["medio"]))

    def sin_vineta(t):
        for marca in ("- ", "* ", "\u2022 ", "\u2013 "):
            if t.startswith(marca):
                return t[len(marca):].strip()
        i = 0
        while i < len(t) and t[i].isdigit():
            i += 1
        if i and i < len(t) and t[i] in ".)-":
            return t[i + 1:].strip()
        return t

    def extraer():
        """Las lineas utiles de la caja, ya limpias."""
        lineas = []
        for linea in (caja.value or "").splitlines():
            t = linea.strip()
            if not t:
                continue
            if limpiar.value:
                t = sin_vineta(t)
            if t:
                lineas.append(t)
        return lineas

    contador = st.texto("0 notas", st.T_MINI, st.C["tenue"])

    def contar(e=None):
        """Dice cuantas notas van a salir, antes de pulsar Agregar."""
        n = 1 if modo.value == "junto" and extraer() else len(extraer())
        contador.value = "%d nota%s" % (n, "" if n == 1 else "s")
        contador.update()

    caja.on_change = contar
    modo.on_change = contar
    limpiar.on_change = contar

    def aceptar(e):
        lineas = extraer()
        cerrar(page)
        if lineas:
            al_aceptar(["\n".join(lineas)] if modo.value == "junto"
                       else lineas)

    cuerpo = ft.Column([caja, modo, limpiar], spacing=st.E2, tight=True)
    d = _marco(page, "Agregar a " + carpeta, cuerpo,
               [contador, ft.Container(expand=True),
                st.boton("Cancelar", lambda e: cerrar(page)),
                st.boton("Agregar", aceptar, "acento")], 480)
    abrir(page, d)
    return d


NOMBRES_TEMA = {"auto": "Segun Windows", "oscuro": "Oscuro",
                "medianoche": "Medianoche", "grafito": "Grafito",
                "bosque": "Bosque", "claro": "Claro", "papel": "Papel",
                "niebla": "Niebla", "arena": "Arena", "lila": "Lila",
                "salvia": "Salvia", "rubor": "Rubor"}


def apariencia(page, acento, atajo, al_aplicar, tema="auto"):
    """Fondo, color de acento y atajo.

    Ya no hay selector de tamano: la ventana se estira arrastrando sus
    bordes, como cualquier otra, y el tamano se recuerda solo. Tampoco
    hay estilo de carpetas: el modo "fichas" se retiro por no usarse.
    """
    elegido = {"acento": acento, "tema": tema}

    temas = ft.Row(spacing=st.E2, run_spacing=st.E2, wrap=True)

    def muestra(nombre):
        """Un tema son sus dos colores de fondo, asi que se enseñan.

        Media pastilla con el color del panel y media con el de las
        tarjetas. Con el nombre escrito no se sabia que iba a salir; con
        los colores delante, si.
        """
        paleta = st.TEMAS[nombre]
        puesto = nombre == elegido["tema"]
        if paleta is None:                    # "auto": mitad y mitad
            izq, der = st.OSCURA["fondo"], st.CLARA["fondo"]
        else:
            izq, der = paleta["fondo"], paleta["tarjeta"]
        return ft.Container(
            width=44, height=36, border_radius=10, ink=True,
            tooltip=NOMBRES_TEMA[nombre],
            on_click=lambda e, t=nombre: poner_tema(t),
            border=ft.Border.all(2, st.C["acento"] if puesto
                                 else st.C["borde"]),
            content=ft.Stack([
                ft.Row([ft.Container(bgcolor=izq, expand=True),
                        ft.Container(bgcolor=der, expand=True)],
                       spacing=0, expand=True),
                ft.Container(
                    content=ft.Icon(ft.Icons.CHECK, size=16,
                                    color=st.C["acento"]),
                    alignment=ft.Alignment.CENTER, expand=True)
                if puesto else ft.Container(),
            ], expand=True),
            clip_behavior=ft.ClipBehavior.ANTI_ALIAS,
            animate=ft.Animation(120, ft.AnimationCurve.EASE_OUT))

    def pintar_temas():
        # Tamano fijo, sin pastillas de texto: una fila con wrap necesita
        # anchos intrinsecos, y las pastillas no los dan. Eso dejaba el
        # dialogo en un bloque gris.
        temas.controls = [muestra(n) for n in st.TEMAS]

    def poner_tema(nombre):
        elegido["tema"] = nombre
        pintar_temas()
        page.update()

    # wrap: con doce colores no caben en una linea de 400.
    bolas = ft.Row(spacing=st.E2, run_spacing=st.E2, wrap=True)

    def pintar_bolas():
        bolas.controls = [
            ft.Container(
                width=36, height=36, border_radius=18, bgcolor=color,
                content=ft.Icon(ft.Icons.CHECK, size=17, color="#FFFFFF")
                if nombre == elegido["acento"] else None,
                alignment=ft.Alignment.CENTER, ink=True,
                on_click=lambda e, n=nombre: poner_acento(n),
                animate=ft.Animation(120, ft.AnimationCurve.EASE_OUT))
            for nombre, (color, _) in st.ACENTOS.items()]

    def poner_acento(nombre):
        elegido["acento"] = nombre
        pintar_bolas()
        page.update()

    combinaciones = ft.Dropdown(
        value=cfg.ATAJOS.get(atajo, cfg.ATAJOS[cfg.ATAJO_DEF]),
        options=[ft.DropdownOption(v) for v in cfg.ATAJOS.values()],
        border_radius=st.R_CONTROL, filled=True, bgcolor=st.C["tarjeta"],
        border_color=ft.Colors.TRANSPARENT,
        focused_border_color=st.C["acento"], text_size=st.T_MENOR,
        color=st.C["texto"],
        content_padding=ft.Padding.symmetric(horizontal=12, vertical=10))

    pintar_temas()
    pintar_bolas()

    def aplicar(e):
        atajo_nuevo = atajo
        for clave, texto in cfg.ATAJOS.items():
            if texto == combinaciones.value:
                atajo_nuevo = clave
                break
        cerrar(page)
        al_aplicar(elegido["acento"], atajo_nuevo, elegido["tema"])

    def seccion(titulo, control):
        return ft.Column([st.texto(titulo, st.T_MINI, st.C["medio"]), control],
                         spacing=st.E2, tight=True)

    # Con doce temas y dieciocho colores el dialogo pasa de alto: sin
    # scroll, el atajo quedaba fuera de la ventana y no se alcanzaba.
    cuerpo = ft.Column([
        seccion("Fondo", temas),
        seccion("Color de acento", bolas),
        seccion("Atajo para abrir", combinaciones),
    ], spacing=st.E4, tight=True, scroll=ft.ScrollMode.AUTO,
        height=min(420, max(260, (page.window.height or 560) - 200)))

    d = _marco(page, "Apariencia", cuerpo,
               [st.boton("Cancelar", lambda e: cerrar(page)),
                st.boton("Aplicar", aplicar, "acento")], 400)
    abrir(page, d)
    return d


def editar_carpeta(page, carpeta, elementos, al_aplicar):
    """Renombrar la carpeta y limpiar lo que ya no sirve, de una vez.

    Antes habia que borrar cada texto desde su propio menu y renombrar
    aparte. Aqui se ve todo el contenido junto, que es cuando de verdad
    se decide que sobra.

    al_aplicar recibe (nombre_nuevo, lista_de_elementos_a_borrar).
    """
    nombre = st.campo("", valor=carpeta)
    condenados = set()
    filas_ui = ft.Column(spacing=st.E1, tight=True,
                         scroll=ft.ScrollMode.AUTO,
                         height=min(240, 44 * max(1, len(elementos))))

    def pintar():
        filas_ui.controls = []
        for elemento in elementos:
            fuera = id(elemento) in condenados
            filas_ui.controls.append(ft.Container(
                content=ft.Row([
                    st.texto(elemento.get("titulo", "-"), st.T_MENOR,
                             st.C["tenue"] if fuera else st.C["texto"]),
                    ft.Container(expand=True),
                    st.icono(ft.Icons.UNDO if fuera
                             else ft.Icons.DELETE_OUTLINE,
                             lambda e, x=elemento: alternar(x), 16,
                             st.C["medio"] if fuera else st.ROJO,
                             "Recuperar" if fuera else "Quitar", False, 30),
                ], spacing=st.E2,
                    vertical_alignment=ft.CrossAxisAlignment.CENTER),
                bgcolor=st.C["tarjeta"], border_radius=st.R_CONTROL,
                padding=ft.Padding.only(left=st.E3, right=st.E1),
                height=38, opacity=0.45 if fuera else 1.0))
        if not elementos:
            filas_ui.controls.append(
                st.texto("La carpeta esta vacia", st.T_MINI, st.C["tenue"]))

    def alternar(elemento):
        # Marcar y desmarcar en vez de borrar al momento: nada se pierde
        # hasta pulsar Guardar, asi un clic de mas no cuesta nada.
        condenados.symmetric_difference_update({id(elemento)})
        pintar()
        page.update()

    pintar()

    def guardar(e):
        fuera = [x for x in elementos if id(x) in condenados]
        nuevo = (nombre.value or "").strip()
        cerrar(page)
        al_aplicar(nuevo, fuera)

    cuerpo = ft.Column([
        st.texto("Nombre de la carpeta", st.T_MINI, st.C["medio"]),
        nombre,
        st.texto("Contenido", st.T_MINI, st.C["medio"]),
        filas_ui,
    ], spacing=st.E2, tight=True)

    d = _marco(page, "Editar carpeta", cuerpo,
               [st.boton("Cancelar", lambda e: cerrar(page)),
                st.boton("Guardar", guardar, "acento")], 400)
    abrir(page, d)
    return d


def confirmar(page, mensaje, al_confirmar, peligro=True):
    def si(e):
        cerrar(page)
        al_confirmar()

    d = _marco(page, "Confirmar",
               ft.Column([st.texto(mensaje, st.T_CUERPO, st.C["texto"],
                                   lineas=4)], tight=True),
               [st.boton("Cancelar", lambda e: cerrar(page)),
                st.boton("Si, borrar" if peligro else "Aceptar", si,
                         "peligro" if peligro else "acento")], 360)
    abrir(page, d)
    return d
