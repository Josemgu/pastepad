# -*- coding: utf-8 -*-
"""Las ventanas que se abren encima del panel.

En Flet son dialogos de la propia pagina, no ventanas del sistema: asi
heredan el tema y las animaciones sin trabajo extra.
"""

import flet as ft

from . import config as cfg
from . import estilo as st
from . import modelo


def _marco(titulo, cuerpo, acciones, ancho=460):
    return ft.AlertDialog(
        modal=True,
        bgcolor=st.C["elevado"],
        shape=ft.RoundedRectangleBorder(radius=st.R_PANEL),
        title=st.texto(titulo, st.T_TITULO, st.C["texto"],
                       ft.FontWeight.W_500),
        content=ft.Container(content=cuerpo, width=ancho),
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

    def guardar(e):
        contenido = (caja.value or "").strip()
        if contenido:
            primera = contenido.splitlines()[0]
            al_guardar({"titulo": modelo.una_linea(primera, 48),
                        "categoria": elegida.value or "Mis textos",
                        "runs": [modelo.fragmento(caja.value)]})
        cerrar(page)

    cuerpo = ft.Column([
        ft.Row([st.texto("Guardar en", st.T_MENOR, st.C["medio"]), elegida],
               spacing=st.E3),
        caja,
        st.texto("Escribe [[algo]] y el programa te lo preguntara al pegar",
                 st.T_MINI, st.C["tenue"]),
    ], spacing=st.E3, tight=True)

    d = _marco("Nuevo texto" if not snippet else "Editar texto", cuerpo,
               [st.boton("Cancelar", lambda e: cerrar(page)),
                st.boton("Guardar", guardar, "acento")])
    abrir(page, d)
    return d


def una_linea(page, titulo, etiqueta, al_aceptar, valor=""):
    caja = st.campo(etiqueta, valor=valor)

    def aceptar(e):
        v = (caja.value or "").strip()
        cerrar(page)
        if v:
            al_aceptar(v)

    caja.on_submit = aceptar
    d = _marco(titulo, ft.Column([caja], tight=True),
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
    d = _marco("Completar antes de pegar", cuerpo,
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

    def aceptar(e):
        lineas = []
        for linea in (caja.value or "").splitlines():
            t = linea.strip()
            if not t:
                continue
            if limpiar.value:
                t = sin_vineta(t)
            if t:
                lineas.append(t)
        cerrar(page)
        if lineas:
            al_aceptar(["\n".join(lineas)] if modo.value == "junto"
                       else lineas)

    cuerpo = ft.Column([caja, modo, limpiar], spacing=st.E2, tight=True)
    d = _marco("Agregar a " + carpeta, cuerpo,
               [st.boton("Cancelar", lambda e: cerrar(page)),
                st.boton("Agregar", aceptar, "acento")], 480)
    abrir(page, d)
    return d


def apariencia(page, acento, tamano, atajo, carpetas, al_aplicar):
    elegido = {"acento": acento, "tamano": tamano, "carpetas": carpetas}

    bolas = ft.Row(spacing=st.E2)

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

    medidas = ft.Row(spacing=st.E1)

    def pintar_medidas():
        medidas.controls = [
            st.pildora(n.capitalize(), lambda e, m=n: poner_medida(m),
                       n == elegido["tamano"])
            for n in cfg.TAMANOS]

    def poner_medida(nombre):
        elegido["tamano"] = nombre
        pintar_medidas()
        page.update()

    estilos = ft.Row(spacing=st.E1)

    def pintar_estilos():
        estilos.controls = [
            st.pildora(texto, lambda e, v=valor: poner_estilo(v),
                       valor == elegido["carpetas"])
            for valor, texto in ((cfg.CARPETAS_MENU, "Lista desplegable"),
                                 (cfg.CARPETAS_FICHAS, "Fichas en fila"))]

    def poner_estilo(valor):
        elegido["carpetas"] = valor
        pintar_estilos()
        page.update()

    combinaciones = ft.Dropdown(
        value=cfg.ATAJOS.get(atajo, cfg.ATAJOS[cfg.ATAJO_DEF]),
        options=[ft.DropdownOption(v) for v in cfg.ATAJOS.values()],
        border_radius=st.R_CONTROL, filled=True, bgcolor=st.C["tarjeta"],
        border_color=ft.Colors.TRANSPARENT,
        focused_border_color=st.C["acento"], text_size=st.T_MENOR,
        color=st.C["texto"],
        content_padding=ft.Padding.symmetric(horizontal=12, vertical=10))

    pintar_bolas()
    pintar_medidas()
    pintar_estilos()

    def aplicar(e):
        atajo_nuevo = atajo
        for clave, texto in cfg.ATAJOS.items():
            if texto == combinaciones.value:
                atajo_nuevo = clave
                break
        cerrar(page)
        al_aplicar(elegido["acento"], elegido["tamano"], atajo_nuevo,
                   elegido["carpetas"])

    def seccion(titulo, control):
        return ft.Column([st.texto(titulo, st.T_MINI, st.C["medio"]), control],
                         spacing=st.E2, tight=True)

    cuerpo = ft.Column([
        seccion("Color", bolas),
        seccion("Tamano del panel", medidas),
        seccion("Carpetas", estilos),
        seccion("Atajo para abrir", combinaciones),
    ], spacing=st.E4, tight=True)

    d = _marco("Apariencia", cuerpo,
               [st.boton("Cancelar", lambda e: cerrar(page)),
                st.boton("Aplicar", aplicar, "acento")], 400)
    abrir(page, d)
    return d


def confirmar(page, mensaje, al_confirmar, peligro=True):
    def si(e):
        cerrar(page)
        al_confirmar()

    d = _marco("Confirmar",
               ft.Column([st.texto(mensaje, st.T_CUERPO, st.C["texto"],
                                   lineas=4)], tight=True),
               [st.boton("Cancelar", lambda e: cerrar(page)),
                st.boton("Si, borrar" if peligro else "Aceptar", si,
                         "peligro" if peligro else "acento")], 360)
    abrir(page, d)
    return d
