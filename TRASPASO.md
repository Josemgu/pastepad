# Traspaso — reescritura de pastepad

Documento para arrancar la reescritura sin repetir lo ya aprendido.
Lee esto antes de escribir una línea.

---

## Qué es y qué tiene que hacer

Un gestor de portapapeles para Windows. Reemplaza a `Win+V`, que solo
guarda 25 entradas y las pierde al reiniciar.

**Requisitos, en orden de importancia:**

1. **El atajo global tiene que funcionar siempre.** Sin esto, nada más
   importa. Es el requisito que la versión en Python nunca cumplió.
2. **Abrir el panel debe ser instantáneo.** Es una herramienta de
   productividad: si tarda, no se usa.
3. **Poco consumo.** Debe poder vivir residente todo el día en una
   máquina de 4 GB sin que se note.
4. Aspecto limpio y minimalista, al nivel de `Win+V`.

---

## Por qué se abandona la versión en Python + Flet

**Medido, no supuesto:**

| | |
|---|---|
| Memoria total | 207 MB (2 procesos) |
| — motor de Flutter | 177 MB, intocable |
| — nuestra interfaz entera | 26 MB |
| Abrir el panel (residente) | 12–184 ms |
| Arranque en frío | 1–2 s |
| Procesos | 2 (python.exe + flet.exe) |
| Hilos | 13 + 55 |

Una app Flet **vacía** ya son 177 MB. Quitar sombras, animaciones o
temas no baja de ahí: es el motor arrancando, no el diseño.

**Y el fallo que no se resolvió:** el atajo global responde unas dos o
tres veces y después deja de hacerlo, sin excepción y sin rastro.

Se intentaron dos implementaciones:

1. **Librería `keyboard`** (hook `WH_KEYBOARD_LL`). Windows desengancha
   el hook en silencio si el callback tarda más de
   `LowLevelHooksTimeout` (300 ms). Descartada.
2. **`RegisterHotKey`** de la API de Windows, en un hilo propio con su
   bomba de mensajes. Probada en aislamiento con 45 pulsaciones
   sintéticas: 45 recibidas, 0 perdidas. **Pero dentro de la aplicación
   sigue muriendo.**

**La sospecha que quedó sin confirmar:** el problema no es el registro
del atajo sino qué pasa después. `page.run_thread()` de Flet no crea un
hilo, entrega el trabajo a un `ThreadPoolExecutor` compartido. La app
le da dos bucles `while True` que nunca retornan (`_vigilar` y
`_atender_cola`), ocupando dos trabajadores para siempre. Si ese pool
se agota o el hilo se bloquea en un `page.update()` sobre una ventana
oculta, las pulsaciones se encolan y nadie las atiende.

**En una reescritura esto desaparece solo** si el atajo y la ventana
viven en el mismo hilo de interfaz, que es como funciona una aplicación
nativa de Windows.

---

## El lenguaje: C# con WinUI 3

**Decidido tras evaluarlo, no por costumbre.** Microsoft lo señala como
la vía preferente para aplicaciones nuevas y nativas de Windows; WPF
queda para bases de código ya existentes.

| Por qué | |
|---|---|
| **Aspecto** | Mica y Acrílico nativos. Son los materiales de Fluent Design que usa `Win+V`: no hay que imitarlos, ya están |
| **Escalado DPI** | Automático y por monitor. WPF, Win32 y WinForms **no** lo hacen: sin trabajo extra salen borrosos |
| **Tipografía e iconos** | `Segoe UI Variable` y `Segoe Fluent Icons`, los de Windows 11, de serie |
| **Tema claro/oscuro** | Cambia solo, sin código |
| **Memoria** | 15–20% por debajo de WPF en apps equivalentes. Contra los 207 MB de Flet, entre 40 y 60 |
| **Atajo global** | `RegisterHotKey` sobre el `HWND` de la ventana. Sin hilos, sin colas, sin pools — ahí es donde se perdió la versión anterior |
| **Portapapeles** | `AddClipboardFormatListener` avisa **por evento**: se acaba el sondeo cada 0,7 s |
| **Native AOT** | Su compatibilidad va por delante de la de WPF, que arrastra reflexión en el binding |
| **Firma** | El certificado gratuito de SignPath aplica igual |

**El riesgo, dicho claro:** WinUI 3 tiene ecosistema menos maduro que
WPF —menos controles de terceros, menos respuestas para casos raros—.

Se acepta por un motivo concreto: **la parte que puede romperse (el
atajo y el portapapeles) es Win32 puro y es idéntica en los dos.** La
inmadurez de WinUI no toca lo arriesgado; solo afecta a lo visual, que
es justo donde WinUI gana.

**Alternativas evaluadas y descartadas:**

- **WPF** — más maduro, pero el aspecto de Windows 11 hay que
  construirlo a mano y el DPI exige trabajo extra. Es la red de
  seguridad si WinUI 3 estorbara.
- **C++/Win32** — lo más ligero (~15 MB) y máximo control, pero el
  acabado se dibuja a mano y son meses.
- **Rust/Tauri o Go/Wails** — la interfaz es web: vuelve el peso (~80 MB)
  y el arranque lento que se acaba de dejar atrás.
- **Avalonia o MAUI** — multiplataforma que no se necesita, y no tienen
  el aspecto de Windows 11.
- **CustomTkinter** — bajaría a ~60 MB, pero se deforma al redimensionar
  y los bordes salen rasposos. Ya se abandonó una vez por eso.

---

## Lo que SÍ funciona y hay que conservar

No empieces de cero. Estas decisiones están probadas y costaron
encontrarlas:

**Formatos privados del portapapeles.** Windows define cuatro con los
que un programa dice «no guardes esto»: `Clipboard Viewer Ignore`,
`ExcludeClipboardContentFromMonitorProcessing`,
`CanIncludeInClipboardHistory`, `CanUploadToCloudClipboard`. Los usan
KeePass, Bitwarden, el Administrador de credenciales y el incógnito de
Chrome. Ver `pastepad/windows.py`, función `contenido_privado()`.

**Devolver el foco antes de pegar.** Al abrirse el panel, el campo donde
estaba el cursor lo pierde, y un `Ctrl+V` a secas se va al vacío. Hay
que guardar el `hwnd` de la ventana activa en el instante del atajo y
devolvérselo con `AttachThreadInput` antes de pegar — es el truco que
Windows exige para que `SetForegroundWindow` funcione desde otro
proceso. Ver `devolver_foco()`.

**Soltar Shift y Alt antes del pegado.** Si el atajo los lleva y siguen
pulsados, el destino recibe `Ctrl+Shift+V` en vez de `Ctrl+V`.

**Leer el portapapeles solo cuando cambia.** `GetClipboardSequenceNumber`
sube con cada copia y cuesta una llamada; abrir el portapapeles cuesta
muchísimo más. En C# esto mejora: `AddClipboardFormatListener` avisa por
evento y elimina el sondeo.

**Guardar diferido.** Escribir el JSON entero en cada copia costaba
7,8 ms y más de un megabyte por `Ctrl+C`. Acumular en memoria y volcar
cada pocos segundos lo dejó en 0,020 ms. Lo que el usuario hace a
propósito (fijar, borrar, vaciar) sí debe escribirse al instante.

**Un enlace se abre, no se pega.** Solo si el texto entero es una URL;
un párrafo que la menciona de pasada no cuenta.

**Instalar en `%LOCALAPPDATA%`, no en Archivos de programa.** Ahí
Windows bloquea la escritura sin avisar: el programa arrancaría pero no
guardaría nada.

**Una sola instancia.** Dos procesos se pelean por el atajo global y el
que pierde queda como una ventana muda. Fue la causa de que el programa
pareciera roto durante días.

---

## Lógica reutilizable

Estos módulos no importan ninguna librería gráfica. Son 1.208 líneas de
Python que traducir, no que rediseñar:

```
pastepad/modelo.py      los datos y sus reglas, con 19 pruebas
pastepad/busqueda.py    ranking: sin tildes, palabras en cualquier
                        orden, el título pesa más que el cuerpo
pastepad/windows.py     portapapeles, foco, atajo, autoarranque
pastepad/idiomas.py     los textos en 4 idiomas
pastepad/config.py      límites y rutas
```

**Las 19 pruebas de `prueba.py` son la especificación ejecutable del
modelo.** Tradúcelas primero y tendrás la lógica verificada antes de
dibujar nada.

---

## Formato de datos, para no perder lo guardado

En `%LOCALAPPDATA%\pastepad\`:

```jsonc
// snippets.json
{
  "categorias": ["Trabajo", "Notas"],
  "snippets": [
    { "titulo": "Plantilla de correo",
      "categoria": "Trabajo",
      "runs": [ { "t": "Hola [[nombre]]", "f": "Calibri", "s": 11,
                  "b": 0, "i": 0, "u": 0, "c": "#000000" } ] }
  ]
}

// historial.json — lista, los fijados primero
[ { "tipo": "texto", "texto": "...", "pin": true },
  { "tipo": "imagen", "ruta": "C:\\...\\imagenes\\img_1699.bmp" } ]

// config.json
{ "idioma": "es", "tema": "auto", "acento": "menta",
  "atajo": "ctrl+shift+v", "ancho": 380, "alto": 560,
  "pausado": false, "autoarranque": "si" }
```

Un `runs` es una lista de fragmentos con formato; las claves van de una
letra para que el JSON no engorde. Conservar el formato permite que
quien ya use pastepad no pierda nada.

---

## Diseño

Ya está definido y no hay que reinventarlo:

- **`docs/mockups/`** — 35 maquetas SVG de cada pantalla y estado
- **`docs/ESPECIFICACION-UI.md`** — paleta, medidas, tipografía y
  comportamiento, con los valores exactos

Cuidado: las maquetas **20 y 33** no están verificadas contra la
aplicación, y la **26 y 27** son diálogos de Windows, no nuestros.

Paleta base: fondo `#0B0B0D`, tarjeta `#1B1B1F`, acento `#2DD4A7`.
Filas de 56 px con 6 de separación. Panel de 380×560 por defecto.

---

## Errores que no hay que repetir

1. **Ningún `catch` mudo.** El fallo del atajo tardó días en
   diagnosticarse porque cada hilo se tragaba su excepción en silencio.
   Todo lo que se capture, se registra.
2. **Registrar los errores de todos los hilos.** En Python,
   `sys.excepthook` solo cubre el principal. Cualquier lenguaje tiene su
   equivalente; hay que enganchar los dos.
3. **Medir antes de decidir.** La sospecha de que el programa era lento
   resultó falsa: abría en 12–184 ms. Lo lento era el arranque en frío,
   y lo roto eran dos instancias peleándose.
4. **La versión sale del tag, no escrita a mano.** Se publicó una
   release con binarios viejos por tenerla clavada en doce sitios.
5. **Probar el ejecutable, no solo el código fuente.** El error de
   `python311.dll` solo aparecía al separar el `.exe` de su carpeta.

---

## Estado al momento del traspaso

- Rama `main`, tag `v3.0.1` publicado con instalador y portable
- 19 pruebas en verde, sin código muerto ni referencias rotas
- README en inglés y español, 35 maquetas, especificación de interfaz
- **El atajo global sigue sin ser fiable. Es el motivo de la
  reescritura.**
