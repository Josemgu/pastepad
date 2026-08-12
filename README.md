<div align="center">

<img src="docs/banner.svg" alt="pastepad — everything you copy, one shortcut away" width="900">

[![Platform](https://img.shields.io/badge/Windows-10%20%7C%2011-2DD4A7?style=for-the-badge&logo=windows&logoColor=white&labelColor=0B3B2E)](#get-it-running)
[![Python](https://img.shields.io/badge/Python-3.10+-2DD4A7?style=for-the-badge&logo=python&logoColor=white&labelColor=0B3B2E)](https://www.python.org/downloads/)
[![Licence](https://img.shields.io/badge/Licence-MIT-2DD4A7?style=for-the-badge&labelColor=0B3B2E)](LICENSE)
[![Release](https://img.shields.io/github/v/release/Josemgu/pastepad?style=for-the-badge&color=2DD4A7&labelColor=0B3B2E)](https://github.com/Josemgu/pastepad/releases/latest)

**English** · [Español](docs/README.es.md)

</div>

<br>

Windows keeps 25 clipboard entries and forgets them when you restart.
pastepad keeps 80 — plus whatever you file into your own folders, which
never expire.

Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>V</kbd> anywhere. Type two
letters, hit <kbd>Enter</kbd>, and the text lands in the field you were
working in.

<div align="center">
  <img src="docs/capturas/reciente.png" alt="Recent tab" width="290">
  <img src="docs/capturas/guardados.png" alt="Saved tab" width="290">
  <img src="docs/capturas/apariencia.png" alt="Appearance dialog" width="290">
  <br>
  <sub><b>Recent</b> — everything you copy &nbsp;·&nbsp; <b>Saved</b> — bookmarks and notes, apart &nbsp;·&nbsp; <b>Appearance</b> — 12 backgrounds, 18 accents</sub>
</div>

<br>

## Get it running

**[Download the latest release](https://github.com/Josemgu/pastepad/releases/latest)**,
unzip it, and double-click `instalar.bat`.

No administrator needed. It installs into `%LOCALAPPDATA%\pastepad`,
starts with Windows, and adds a Start menu entry. There's also a
single-file portable build if you'd rather not install anything.

> [!NOTE]
> It installs there and not into *Program Files* on purpose: pastepad
> keeps its data next to the executable, and *Program Files* blocks
> writes **without saying so**. It would start but never save anything.

<details>
<summary>Run from source</summary>

<br>

```powershell
pip install -r requirements.txt
python main.py
```

Python 3.10 or newer from [python.org](https://www.python.org/downloads/).
Tick **Add python.exe to PATH** on the first installer screen.

</details>

<details>
<summary>"python311.dll was not found"</summary>

<br>

You copied `pastepad.exe` **on its own**, without the `_internal` folder
that sits beside it. The full Python interpreter lives in there:
pastepad does not need Python installed, but it does need that folder.

Either copy the whole folder, or use the **portable** build — one file
with everything inside, impossible to break that way.

Verified: the executable starts on a machine with no Python and no
Python on `PATH`.

</details>

<details>
<summary>Windows Defender flags it — why?</summary>

<br>

The first run shows *"Windows protected your PC"*. **More info → Run
anyway.**

To be clear: **an MIT licence and being open source do not prevent this
warning.** SmartScreen looks at neither. It looks at whether the binary
is signed, and how many people have downloaded it without incident.

| Option | Effect | Cost |
|---|---|---|
| Accept the warning | One click, once | 0 |
| Publish the SHA256 | Verifiable, warning stays | 0 |
| Free OSS certificate ([SignPath](https://signpath.org/), [OSSign](https://ossign.org/)) | Real certificate, reputation carries across releases | 0 for open source |
| [Azure Trusted Signing](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options) | Same, Microsoft-run | ~$10/month |
| Microsoft Store | Removes it entirely | Developer account |

A **self-signed** certificate does not help: Windows doesn't trust a
certificate that isn't from a recognised authority. And EV certificates
no longer grant instant reputation — that stopped years ago.

Every release ships a `SHA256.txt` so you can check the download is
exactly what the build produced.

</details>

## What it does

<div align="center">
  <img src="docs/caracteristicas.svg" alt="History, templates, bookmarks, search, formatting and privacy" width="880">
</div>

<br>

Each feature links the mockup that illustrates it.

**Automatic history.** Everything you copy shows up under Recent — text
and screenshots. Pin what you use often and it stays on top, safe from
the trim that clears the rest.
&nbsp;·&nbsp; [01](docs/mockups/01_panel_reciente.svg) [19](docs/mockups/19_pausa_activa.svg)

**Fill-in templates.** Write `[[anything]]` in a saved text and pastepad
asks for it before pasting:

```
Hi [[name]], following up on [[topic]] from [[date]].
```

&nbsp;·&nbsp; [06](docs/mockups/06_dialogo_campos.svg) [21](docs/mockups/21_editando_texto.svg)

**Live links.** If what you copied is a web address and nothing else,
the row shows a link icon and its domain instead of a character count.
One click opens it in the browser.
&nbsp;·&nbsp; [13](docs/mockups/13_fila_enlace.svg)

**Bookmarks apart from notes.** Under Saved, links live in their own
collapsible group. That's not decoration: a bookmark opens in the
browser and a note gets pasted. Two different gestures — mixed together,
you have to read the whole list to find either.
&nbsp;·&nbsp; [02](docs/mockups/02_panel_guardados.svg) [34](docs/mockups/34_carpeta_con_muchos_textos.svg)

**Search that ranks.** Types across both tabs at once. Words come in any
order, accents are ignored, and title matches rank above matches buried
in the body.
&nbsp;·&nbsp; [12](docs/mockups/12_busqueda_sin_resultados.svg)

**Your own folders.** Rename a folder, or clear out what no longer
serves you, from the same menu.
&nbsp;·&nbsp; [15](docs/mockups/15_menu_carpetas.svg) [05](docs/mockups/05_dialogo_carpeta.svg)

**Bulk import.** Paste ten names and choose: one note per line, or all
of it in a single note.
&nbsp;·&nbsp; [07](docs/mockups/07_dialogo_lista.svg) [23](docs/mockups/23_arrastrando_lista.svg)

**Respects password managers.** Windows defines
[clipboard formats](https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats)
an app uses to say "don't record this". KeePass, Bitwarden, Windows
Credential Manager and Chrome's incognito all use them. pastepad honours
all four and drops that content.

## Using it

| Key | Action |
|:--|:--|
| <kbd>Ctrl</kbd> <kbd>Shift</kbd> <kbd>V</kbd> | Open the panel at the cursor |
| Type | Filter as you go |
| <kbd>↑</kbd> <kbd>↓</kbd> | Move through results |
| <kbd>Enter</kbd> | Paste |
| <kbd>Esc</kbd> | Close |

Click into the field you want to fill **before** opening the panel.
pastepad records which window had focus and hands it back before
pasting.

The X only hides the panel. To close it for good:

```powershell
taskkill /IM pastepad.exe /F
```

## Making it yours

**Languages** — English, Español, Português, Français. Pick one under
**Appearance → Language**; it persists between sessions.

**Themes** — 12 backgrounds (4 dark, 2 light, 5 pastel, plus "follow
Windows") and 18 accent colours. Every combination clears WCAG AA
contrast.

**Size** — drag the window edges. It remembers where you left it.

<details>
<summary>Adding a language</summary>

<br>

Add a dictionary to [`pastepad/idiomas.py`](pastepad/idiomas.py). The
key is the Spanish source string, not an invented identifier: the code
still reads on its own without looking up what `btn.paste.plain` means,
and anything left untranslated falls back to Spanish instead of leaving
a hole.

</details>

## Your data

In `%LOCALAPPDATA%\pastepad`, as plain files you can copy or back up:

```
snippets.json     saved texts and folders
historial.json    automatic history
config.json       language, theme, colour, shortcut, size
imagenes\         copied screenshots
```

> [!WARNING]
> **The history is stored unencrypted.** Password-manager content is
> dropped automatically, but anything else sensitive you copy does get
> written down. The broom empties it and the pause button stops capture.
> See [SECURITY.md](SECURITY.md).

> [!TIP]
> Don't keep the folder inside OneDrive. Sync can lock the JSON files
> mid-write.

## Design

The three captures at the top are the running app. These are the SVG
references it was built against.

<table>
<tr>
<td align="center" width="33%">
  <img src="docs/mockups/01_panel_reciente.svg" width="250" alt="Recent tab"><br>
  <sub><b>Recent</b><br>History, pinned entry on top</sub>
</td>
<td align="center" width="33%">
  <img src="docs/mockups/02_panel_guardados.svg" width="250" alt="Saved tab"><br>
  <sub><b>Saved</b><br>Folder dropdown</sub>
</td>
<td align="center" width="33%">
  <img src="docs/mockups/08_dialogo_apariencia.svg" width="250" alt="Appearance dialog"><br>
  <sub><b>Appearance</b><br>Colour, size, shortcut</sub>
</td>
</tr>
<tr>
<td align="center">
  <img src="docs/mockups/14_menu_tres_puntos.svg" width="250" alt="Row menu"><br>
  <sub><b>Row menu</b><br>Paste, pin, edit, delete</sub>
</td>
<td align="center">
  <img src="docs/mockups/15_menu_carpetas.svg" width="250" alt="Folder menu"><br>
  <sub><b>Folder menu</b><br>Switch, rename, delete</sub>
</td>
<td align="center">
  <img src="docs/mockups/18_tema_claro.svg" width="250" alt="Light theme"><br>
  <sub><b>Light theme</b><br>Full light palette</sub>
</td>
</tr>
</table>

<details>
<summary><b>See all 35 mockups</b></summary>

<br>

The full map — what each one shows and whether the code implements it —
is in [ESPECIFICACION-UI.md](docs/ESPECIFICACION-UI.md), which covers
01–20. Mockups 21–35 are later additions and are not in it yet.

**Panel states**

| | | |
|---|---|---|
| [01 Recent](docs/mockups/01_panel_reciente.svg) | [02 Saved](docs/mockups/02_panel_guardados.svg) | [03 Multi-select](docs/mockups/03_panel_seleccionar.svg) |
| [10 Empty, Recent](docs/mockups/10_estado_vacio_reciente.svg) | [11 Empty, Saved](docs/mockups/11_estado_vacio_guardados.svg) | [12 No search results](docs/mockups/12_busqueda_sin_resultados.svg) |
| [33 Full panel](docs/mockups/33_panel_lleno.svg) ⚠️ | [34 Crowded folder](docs/mockups/34_carpeta_con_muchos_textos.svg) | |

**Dialogs**

| | | |
|---|---|---|
| [04 New text](docs/mockups/04_dialogo_texto_nuevo.svg) | [05 Folder name](docs/mockups/05_dialogo_carpeta.svg) | [06 Template fields](docs/mockups/06_dialogo_campos.svg) |
| [07 Bulk import](docs/mockups/07_dialogo_lista.svg) | [08 Appearance](docs/mockups/08_dialogo_apariencia.svg) | [09 Confirm delete](docs/mockups/09_dialogo_confirmar.svg) |
| [21 Editing a text](docs/mockups/21_editando_texto.svg) | [22 Picking an accent](docs/mockups/22_apariencia_arrastrando.svg) | [23 Typing a list](docs/mockups/23_arrastrando_lista.svg) |

**Behaviour**

| | | |
|---|---|---|
| [13 Link row](docs/mockups/13_fila_enlace.svg) | [14 Row menu](docs/mockups/14_menu_tres_puntos.svg) | [15 Folder menu](docs/mockups/15_menu_carpetas.svg) |
| [20 Resizing](docs/mockups/20_estirando_ventana.svg) ⚠️ | | |

**Sizes and themes**

| | | |
|---|---|---|
| [16 Mini, 300×380](docs/mockups/16_tamano_mini.svg) | [17 Large, 470×700](docs/mockups/17_tamano_grande.svg) | [18 Light theme](docs/mockups/18_tema_claro.svg) |
| [19 Capture paused](docs/mockups/19_pausa_activa.svg) | | |

**System notices**

| | | |
|---|---|---|
| [24 Save failed](docs/mockups/24_error_guardado.svg) | [25 Shortcut taken](docs/mockups/25_atajo_en_uso.svg) | |
| [26 SmartScreen block](docs/mockups/26_defender_bloqueo.svg) 🪟 | [27 SmartScreen, run anyway](docs/mockups/27_defender_ejecutar.svg) 🪟 | |

**Worked example — copy a link, open it**

| | | |
|---|---|---|
| [28 Copy](docs/mockups/28_flujo_1_copiar.svg) | [29 Shortcut](docs/mockups/29_flujo_2_atajo.svg) | [30 Panel opens](docs/mockups/30_flujo_3_panel.svg) |
| [31 One click](docs/mockups/31_flujo_4_click.svg) | [32 Browser opens](docs/mockups/32_flujo_5_abierto.svg) | |

**Icons**

[35 System icons](docs/mockups/35_iconos_sistema.svg) — every icon used
in the interface, enlarged and named.

<br>

⚠️ **Not verified against the running app.** `20` is flagged as
speculative in ESPECIFICACION-UI.md — it sketches how native Flet
resizing *should* look, but nobody has watched it run. `33` is a later
addition never checked either, and it draws rows past its own canvas, so
it renders clipped. Treat both as intent, not confirmed behaviour.

🪟 **Not pastepad's interface.** `26` and `27` are Windows' own
SmartScreen dialogs, drawn here so you know what to expect the first
time you run an unsigned executable. pastepad cannot change or suppress
them.

</details>

## Under the hood

<details>
<summary>The global shortcut doesn't use the <code>keyboard</code> library</summary>

<br>

It uses `RegisterHotKey` from the Windows API. The library installs a
`WH_KEYBOARD_LL` hook, and Windows **silently unhooks it** if the
callback takes longer than 300 ms (`LowLevelHooksTimeout`): the shortcut
would answer a few times and then die, leaving no error and no trace.

With `RegisterHotKey` there is no hook, and Windows tells you when
another program already owns the combination — something the library
never reported.

</details>

<details>
<summary>Saving is deferred</summary>

<br>

Automatic capture accumulates in memory and hits disk every 3 seconds.
Each copy used to rewrite the whole JSON: **7.8 ms and over a megabyte
per <kbd>Ctrl</kbd>+<kbd>C</kbd>**, up to 16 MB worst case. It is
0.020 ms now.

What you do on purpose — pin, delete, clear — still writes immediately:
deferring that would mean losing it if the program dies.

</details>

<details>
<summary>The clipboard is only read when it changes</summary>

<br>

Windows has a counter (`GetClipboardSequenceNumber`) that ticks with
every copy. Reading it costs one call; opening the clipboard costs far
more.

</details>

<details>
<summary>A link opens, it doesn't paste</summary>

<br>

If the entry is nothing but a web address, clicking opens the browser. A
paragraph that merely mentions a URL doesn't count — see
`modelo.es_enlace()`, it has tests.

</details>

<details>
<summary>Project layout</summary>

<br>

```
main.py               entry point — calls ft.run()
pastepad/
  config.py           constants, limits and paths      [no flet]
  idiomas.py          the UI strings in 4 languages    [no flet]
  registro.py         errores.log — the only writer
  modelo.py           the data and its rules           [no flet]
  busqueda.py         ranked search with a cache       [no flet]
  windows.py          clipboard, focus, global hotkey
  estilo.py           colours, sizes and pieces
  filas.py            the list rows
  ventanas.py         the dialogs
  app.py              coordinates the above
prueba.py             19 tests — run without opening a window
instalador/           instalar.bat, desinstalar.bat
docs/                 spec, function reference, mockups
```

The five marked modules import no graphics library. That's why the tests
run without opening a window, and why migrating from tkinter to Flet
reused 40% of the code untouched.

Every function is documented in three lines each in
[FUNCIONES.md](docs/FUNCIONES.md).

</details>

<details>
<summary>Building it yourself</summary>

<br>

```powershell
.\build.bat
```

Runs the tests, packages with `flet pack`, and prints the SHA256. It
builds both the installable folder and the single-file portable `.exe`.

It uses `flet pack` rather than `flet build` because the latter
downloads the whole Flutter SDK — over a gigabyte.

</details>

## Why another one

[Ditto](https://github.com/sabrogden/Ditto) and
[CopyQ](https://github.com/hluk/CopyQ) are excellent and have years of
work behind them. This exists because my daily work needed two things
neither does out of the box: fill-in templates for notes I retype
constantly, and rich-text paste that survives into Outlook.

---

<div align="center">
<sub>

Built with Python and [Flet](https://flet.dev) · **[MIT](LICENSE)** · by [Jose Miguel Ortiz](https://github.com/Josemgu)

</sub>
</div>
