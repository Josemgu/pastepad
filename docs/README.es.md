<div align="center">

<img src="banner.svg" alt="pastepad — gestor de portapapeles para Windows" width="880">

<br>

[![Plataforma](https://img.shields.io/badge/Windows-10%20%7C%2011-2DD4A7?style=for-the-badge&logo=windows&logoColor=white&labelColor=0B0B0D)](#instalar)
[![Python](https://img.shields.io/badge/Python-3.10+-2DD4A7?style=for-the-badge&logo=python&logoColor=white&labelColor=0B0B0D)](https://www.python.org/downloads/)
[![Licencia](https://img.shields.io/badge/Licencia-MIT-2DD4A7?style=for-the-badge&labelColor=0B0B0D)](../LICENSE)
[![Idiomas](https://img.shields.io/badge/Idiomas-ES%20·%20EN%20·%20PT%20·%20FR-2DD4A7?style=for-the-badge&labelColor=0B0B0D)](#idiomas)

**Windows guarda 25 cosas en el portapapeles y las pierde al reiniciar.**
<br>
pastepad guarda 80, más lo que archives en tus carpetas, y no se pierde nunca.

</div>

**[English](../README.md)** · Español

---

<div align="center">

### Pulsa <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>V</kbd> donde estés

Escribe dos letras · <kbd>Enter</kbd> · el texto aparece donde estaba el cursor

</div>

<br>

<div align="center">
  <img src="capturas/reciente.png" alt="Pestaña Reciente" width="300">
  <img src="capturas/guardados.png" alt="Guardados, con marcadores y notas separados" width="300">
  <img src="capturas/apariencia.png" alt="Diálogo de apariencia" width="300">
</div>

<div align="center">
  <sub><b>Reciente</b> — todo lo que copias &nbsp;·&nbsp; <b>Guardados</b> — marcadores y notas, aparte &nbsp;·&nbsp; <b>Apariencia</b> — 12 fondos, 18 acentos</sub>
</div>

<br>

## Qué hace

<div align="center">
  <img src="caracteristicas.svg" alt="Historial, plantillas, marcadores, búsqueda, formato y privacidad" width="880">
</div>

<br>

**Plantillas con huecos.** Escribe `[[algo]]` en un texto guardado y el
programa te lo pregunta antes de pegar:

```
Hola [[nombre]], te escribo sobre [[tema]] del día [[fecha]].
```

**Marcadores aparte de las notas.** En Guardados, los enlaces van en su
propio grupo plegable. No es decoración: un marcador se abre en el
navegador y una nota se pega. Son dos gestos distintos, y mezclados
obligan a leer la lista entera.

**Respeta los gestores de contraseñas.** Windows define
[formatos de portapapeles](https://learn.microsoft.com/es-es/windows/win32/dataxchg/clipboard-formats)
con los que un programa dice «esto no lo guardes». KeePass, Bitwarden,
el Administrador de credenciales y el incógnito de Chrome los usan.
pastepad honra los cuatro y descarta ese contenido.

## Instalar

Descarga la carpeta `instalador` y doble clic en **`instalar.bat`**.

No pide administrador. Se instala en `%LOCALAPPDATA%\pastepad`, arranca
con Windows y crea un acceso en el menú inicio.

> [!NOTE]
> Se instala ahí y no en *Archivos de programa* a propósito: pastepad
> guarda sus datos junto al ejecutable, y en *Archivos de programa*
> Windows bloquea la escritura **sin avisar**. Arrancaría pero no podría
> guardar nada.

<details>
<summary><b>Desde el código fuente</b></summary>

<br>

```powershell
pip install -r requirements.txt
python main.py
```

Python 3.10 o superior desde [python.org](https://www.python.org/downloads/).
Marca **Add python.exe to PATH** en la primera pantalla.

</details>

<details>
<summary><b>Compilar el ejecutable</b></summary>

<br>

```powershell
.\build.bat
```

Corre las pruebas, empaqueta con `flet pack` y calcula el SHA256. Usa
`flet pack` y no `flet build` porque el segundo descarga el SDK de
Flutter entero, más de un giga.

</details>

<details>
<summary><b>Windows Defender lo marca — ¿por qué?</b></summary>

<br>

La primera vez saldrá *"Windows protegió su PC"*. **Más información →
Ejecutar de todas formas.**

Que quede claro: **la licencia MIT y ser código abierto no evitan este
aviso.** SmartScreen no mira la licencia ni el código. Solo mira si el
binario está firmado y cuánta gente lo ha descargado sin problemas.

| Opción | Efecto | Coste |
|---|---|---|
| Aceptar el aviso | Un clic, una vez | 0 |
| Publicar el hash SHA256 | Verificable, el aviso sigue | 0 |
| Certificado de firma | Lo reduce, no lo elimina de golpe | 200–400 USD/año |
| Microsoft Store | Lo quita del todo | Cuenta de desarrollador |

Los certificados EV ya **no** dan reputación instantánea. Eso dejó de
funcionar hace años.

</details>

## Uso

| Tecla | Acción |
|:--|:--|
| <kbd>Ctrl</kbd> <kbd>Shift</kbd> <kbd>V</kbd> | Abre el panel junto al cursor |
| Escribir | Filtra sobre la marcha |
| <kbd>↑</kbd> <kbd>↓</kbd> | Mueve la selección |
| <kbd>Enter</kbd> | Pega |
| <kbd>Esc</kbd> | Cierra |

Haz clic en el campo que quieres rellenar **antes** de abrir el panel.
pastepad recuerda qué ventana tenía el foco y se lo devuelve antes de
pegar.

**Para cerrarlo del todo** (la X solo lo esconde):

```powershell
taskkill /IM pastepad.exe /F
```

## Idiomas

Español · English · Português · Français

Se elige en **Apariencia → Idioma** y se guarda entre sesiones.

Añadir uno es añadir un diccionario en
[`pastepad/idiomas.py`](../pastepad/idiomas.py). La clave es el texto en
español, no un identificador inventado: el código se sigue leyendo sin
ir a buscar qué significa `btn.paste.plain`, y lo que falte por traducir
sale en español en vez de dejar un hueco.

## Dónde viven tus datos

En `%LOCALAPPDATA%\pastepad`, en archivos planos que puedes copiar:

```
snippets.json     textos guardados y carpetas
historial.json    historial automático
config.json       idioma, tema, color, atajo, tamaño
imagenes\         capturas copiadas
```

> [!WARNING]
> **El historial se guarda sin cifrar.** El contenido de los gestores de
> contraseñas se descarta automáticamente, pero cualquier otra cosa
> sensible que copies sí queda escrita. La escoba lo vacía y el botón de
> pausa detiene la captura. Ver [SECURITY.md](../SECURITY.md).

## Decisiones que quizá sorprendan

<details>
<summary><b>El atajo global no usa la librería <code>keyboard</code></b></summary>

<br>

Usa `RegisterHotKey` de la API de Windows. La librería instala un hook
`WH_KEYBOARD_LL`, y Windows lo desengancha **en silencio** si el callback
tarda más de 300 ms (`LowLevelHooksTimeout`): el atajo respondía unas
cuantas veces y después moría sin dejar rastro ni excepción.

Con `RegisterHotKey` no hay hook, y además Windows avisa si otro programa
ya tiene la combinación — cosa que la librería nunca reportaba.

</details>

<details>
<summary><b>Guardar es diferido</b></summary>

<br>

La captura automática acumula en memoria y baja a disco cada 3 segundos.
Antes cada copia reescribía el JSON entero: **7,8 ms y más de un megabyte
por cada <kbd>Ctrl</kbd>+<kbd>C</kbd>**, y en el peor caso 16 MB.

Ahora son 0,020 ms. Lo que haces a propósito —fijar, borrar, vaciar— sí
se escribe al instante: diferir eso sería perderlo si el programa muere.

</details>

<details>
<summary><b>El portapapeles solo se lee cuando cambia</b></summary>

<br>

Windows tiene un contador (`GetClipboardSequenceNumber`) que sube con
cada copia. Leerlo cuesta una llamada; abrir el portapapeles cuesta
muchísimo más.

</details>

<details>
<summary><b>Un enlace se abre, no se pega</b></summary>

<br>

Si la entrada es solo una dirección web, el clic abre el navegador. Un
párrafo que menciona una URL de paso no cuenta — ver `modelo.es_enlace()`,
tiene pruebas.

</details>

## Estructura

```
main.py               arranque — llama a ft.run()
pastepad/
  config.py           constantes, límites y rutas     [sin flet]
  idiomas.py          los textos en 4 idiomas         [sin flet]
  registro.py         errores.log — el único que escribe
  modelo.py           los datos y sus reglas          [sin flet]
  busqueda.py         ranking con caché               [sin flet]
  windows.py          portapapeles, foco, atajo global
  estilo.py           colores, medidas y piezas
  filas.py            las tarjetas de la lista
  ventanas.py         los diálogos
  app.py              coordina todo lo anterior
prueba.py             19 pruebas — corren sin abrir ventana
instalador/           instalar.bat, desinstalar.bat
docs/
  ESPECIFICACION-UI.md    la interfaz al detalle
  FUNCIONES.md            cada función, tres líneas
  mockups/               las 20 maquetas SVG de referencia
```

Los cinco módulos marcados no importan ninguna librería gráfica. Por eso
las pruebas corren sin abrir ventana, y por eso migrar de tkinter a Flet
reutilizó el 40% del código sin tocarlo.

## Por qué otro más

[Ditto](https://github.com/sabrogden/Ditto) y
[CopyQ](https://github.com/hluk/CopyQ) son excelentes y llevan años de
trabajo detrás. Este existe porque mi día a día necesitaba dos cosas que
ninguno da de serie: plantillas con huecos para notas que reescribo
constantemente, y pegado con formato que sobreviva hasta Outlook.

## Hecho con

Python y [Flet](https://flet.dev), que dibuja a través de Flutter por
GPU — de ahí las esquinas suaves, las sombras y las transiciones. Más
pywin32 para el portapapeles, las ventanas y el atajo global.

Notas de arquitectura en [CLAUDE.md](../CLAUDE.md).

<div align="center">
<br>
<sub>

**[MIT](../LICENSE)** · Hecho por [Jose Miguel Ortiz](https://github.com/Josemgu)

</sub>
</div>
