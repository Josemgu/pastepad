# -*- coding: utf-8 -*-
"""Genera los PNG y los .ico a partir del logo.

Se genera en vez de exportarse a mano porque hay que producir nueve
tamanos distintos y dos variantes del dibujo, y rehacerlo a ojo cada vez
que cambie el logo es como se cuelan las versiones descuadradas.

La geometria es la de docs/logo.svg. No hay rasterizador de SVG en la
maquina, asi que las figuras se dibujan con Pillow: son cinco
rectangulos redondeados, dos giros y una sombra.

    python docs/generar-iconos.py
"""

import os

from PIL import Image, ImageDraw, ImageFilter

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(AQUI)

# Muestreo: se dibuja a este factor y se reduce con LANCZOS. Los bordes
# redondeados y los giros de 2 y 4 grados salen dentados sin esto.
MUESTREO = 8

LIENZO = 256.0

MENTA_ARRIBA = (0x3B, 0xE0, 0xB4)
MENTA_ABAJO = (0x1F, 0xA6, 0x80)
BORDE_MENTA = (0x0F, 0x3D, 0x30)
TINTA = (0x05, 0x2E, 0x23)


def degradado(tam, arriba, abajo):
    """Un degradado vertical del alto pedido."""
    ancho, alto = tam
    tira = Image.new("RGB", (1, alto))
    pinta = ImageDraw.Draw(tira)

    for y in range(alto):
        t = y / max(1, alto - 1)
        pinta.point((0, y), fill=(
            round(arriba[0] + (abajo[0] - arriba[0]) * t),
            round(arriba[1] + (abajo[1] - arriba[1]) * t),
            round(arriba[2] + (abajo[2] - arriba[2]) * t),
        ))

    return tira.resize((ancho, alto), Image.NEAREST)


def tarjeta(lienzo, caja, radio, arriba, abajo, borde, grosor, giro=0.0):
    """Una tarjeta: rectangulo redondeado con degradado, borde y giro.

    Se dibuja en su propia capa y se gira alli; girar sobre el lienzo
    comun arrastraria lo que ya estuviera pintado.
    """
    x, y, ancho, alto = caja

    capa = Image.new("RGBA", lienzo.size, (0, 0, 0, 0))
    mascara = Image.new("L", lienzo.size, 0)
    ImageDraw.Draw(mascara).rounded_rectangle(
        (x, y, x + ancho, y + alto), radius=radio, fill=255)

    relleno = Image.new("RGBA", lienzo.size, (0, 0, 0, 0))
    relleno.paste(degradado(lienzo.size, arriba, abajo), (0, 0))
    capa.paste(relleno, (0, 0), mascara)

    ImageDraw.Draw(capa).rounded_rectangle(
        (x, y, x + ancho, y + alto), radius=radio,
        outline=borde + (255,), width=grosor)

    if giro:
        capa = capa.rotate(
            giro, resample=Image.BICUBIC,
            center=(x + ancho / 2, y + alto / 2))

    lienzo.alpha_composite(capa)


def barra(lienzo, caja, radio, color, opacidad=255):
    x, y, ancho, alto = caja
    capa = Image.new("RGBA", lienzo.size, (0, 0, 0, 0))
    ImageDraw.Draw(capa).rounded_rectangle(
        (x, y, x + ancho, y + alto), radius=radio,
        fill=color + (opacidad,))
    lienzo.alpha_composite(capa)


def completo(lado, encuadrar=False):
    """Las tres tarjetas. El dibujo de docs/logo.svg.

    Con encuadrar=False sale tal cual lo dibuja el SVG, con su aire. Es
    lo que va en los PNG de la documentacion: ahi el logo se ve en una
    pagina y el margen forma parte del dibujo.

    Con encuadrar=True la composicion se escala hasta ocupar el lienzo.
    Es lo que va en el .ico: un icono de escritorio que deja un tercio
    del cuadro vacio se ve mas pequeno que los de al lado. Es encuadre,
    no rediseno: no cambia ninguna proporcion.
    """
    n = lado * MUESTREO
    img = Image.new("RGBA", (n, n), (0, 0, 0, 0))

    if encuadrar:
        # Caja del contenido en el SVG, con sus bordes y sus giros.
        cx0, cy0, cx1, cy1 = 44.0, 60.0, 212.0, 188.0
        escala = (n * 0.92) / max(cx1 - cx0, cy1 - cy0)
        dx = (n - (cx1 - cx0) * escala) / 2 - cx0 * escala
        dy = (n - (cy1 - cy0) * escala) / 2 - cy0 * escala
    else:
        escala = n / LIENZO
        dx = dy = 0.0

    def e(v):
        return v * escala

    def p(x, y):
        return (x * escala + dx, y * escala + dy)

    def caja(x, y, ancho, alto):
        px, py = p(x, y)
        return (px, py, e(ancho), e(alto))

    def grosor(v):
        return max(1, round(e(v)))

    # --- sombra: la silueta de las tres tarjetas, desenfocada
    silueta = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    for x, y, an, al, r, gi in (
        (52, 122, 152, 60, 12, -4),
        (52, 96, 152, 60, 12, 2),
        (48, 64, 160, 66, 13, 0),
    ):
        tarjeta(silueta, caja(x, y, an, al), e(r),
                (0, 0, 0), (0, 0, 0), (0, 0, 0), grosor(4), gi)

    sombra = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    sombra.putalpha(silueta.getchannel("A").point(lambda a: int(a * 0.28)))
    sombra = sombra.filter(ImageFilter.GaussianBlur(e(8)))
    img.alpha_composite(sombra, (0, max(0, round(e(6)))))

    # --- las tres tarjetas, de atras adelante
    tarjeta(img, caja(52, 122, 152, 60), e(12),
            (0xF5, 0xF5, 0xF3), (0xDA, 0xDA, 0xD6),
            (0xC6, 0xC6, 0xC2), grosor(4), -4)

    tarjeta(img, caja(52, 96, 152, 60), e(12),
            (0xFF, 0xFF, 0xFF), (0xED, 0xED, 0xEA),
            (0xD4, 0xD4, 0xD0), grosor(4.5), 2)

    tarjeta(img, caja(48, 64, 160, 66), e(13),
            MENTA_ARRIBA, MENTA_ABAJO, BORDE_MENTA, grosor(5))

    barra(img, caja(68, 82, 90, 9), e(4.5), (0xFF, 0xFF, 0xFF))
    barra(img, caja(68, 100, 60, 7), e(3.5), TINTA, 153)

    return img.resize((lado, lado), Image.LANCZOS)


def simple(lado):
    """Variante para 48 px y por debajo.

    A esos tamanos las tres tarjetas dejan de leerse: la del medio se
    queda en tres pixeles de alto, los bordes de 4 y 5 px del SVG caen
    por debajo de medio pixel y el conjunto es una mancha verde y gris.
    Mirado a tamano real, no ampliado.

    Se queda la tarjeta de menta —que es la que lleva el color de la
    marca y el borde oscuro que la recorta contra cualquier fondo— mas
    grande y con una sola linea de texto. Dos lineas se juntan por
    debajo de 24 px.
    """
    n = lado * MUESTREO
    img = Image.new("RGBA", (n, n), (0, 0, 0, 0))

    margen = n * 0.05
    ancho = n - margen * 2
    alto = ancho * 0.74
    y0 = (n - alto) / 2

    tarjeta(img, (margen, y0, ancho, alto), ancho * 0.19,
            MENTA_ARRIBA, MENTA_ABAJO, BORDE_MENTA,
            max(1, round(n * 0.038)))

    bw = ancho * 0.54
    bh = alto * 0.16
    barra(img, (margen + (ancho - bw) / 2, y0 + alto * 0.42, bw, bh),
          bh / 2, (0xFF, 0xFF, 0xFF))

    return img.resize((lado, lado), Image.LANCZOS)


def dibujar(lado):
    """Que variante toca a cada tamano del .ico."""
    return simple(lado) if lado <= 48 else completo(lado, encuadrar=True)


def escribir(img, ruta):
    img.save(ruta)
    print("  %-46s %5d bytes" % (
        os.path.relpath(ruta, RAIZ).replace("\\", "/"),
        os.path.getsize(ruta)))


def main():
    print("PNG del logo:")
    for lado in (128, 256, 512):
        escribir(completo(lado), os.path.join(AQUI, "logo-%d.png" % lado))

    # Los tamanos que Windows pide de un .ico: 16 y 32 para la bandeja y
    # la barra de tareas, el resto para el explorador y el instalador.
    lados = (16, 20, 24, 32, 48, 64, 128, 256)
    marcos = [dibujar(l) for l in lados]

    print("iconos:")
    for destino in (
        os.path.join(AQUI, "pastepad.ico"),
        os.path.join(RAIZ, "csharp", "Pastepad.App", "Assets", "AppIcon.ico"),
    ):
        marcos[-1].save(destino, format="ICO",
                        sizes=[(l, l) for l in lados],
                        append_images=marcos[:-1])
        print("  %-46s %5d bytes" % (
            os.path.relpath(destino, RAIZ).replace("\\", "/"),
            os.path.getsize(destino)))

    # Hoja de contacto para mirar los tamanos pequenos como se ven de
    # verdad, sin ampliar, sobre fondo claro y sobre fondo oscuro.
    prueba(lados)


def prueba(lados):
    hueco = 12
    ancho = sum(lados) + hueco * (len(lados) + 1)
    alto = max(lados) + hueco * 2

    hoja = Image.new("RGB", (ancho, alto * 2), (0xF6, 0xF6, 0xF4))
    ImageDraw.Draw(hoja).rectangle(
        (0, alto, ancho, alto * 2), fill=(0x1B, 0x1B, 0x1F))

    x = hueco
    for lado in lados:
        icono = dibujar(lado)
        hoja.paste(icono, (x, hueco + (max(lados) - lado)), icono)
        hoja.paste(icono, (x, alto + hueco + (max(lados) - lado)), icono)
        x += lado + hueco

    ruta = os.path.join(AQUI, "iconos-prueba.png")
    hoja.save(ruta)
    print("hoja de contacto: %s" % os.path.relpath(ruta, RAIZ))


if __name__ == "__main__":
    main()
