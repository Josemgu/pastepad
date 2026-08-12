<p align="center">
  <img src="docs/logo.svg" alt="" width="88">
</p>

<h1 align="center">pastepad</h1>

<p align="center">
  A clipboard manager for Windows.<br>
  Keeps what you copy, stores what you choose to keep,<br>
  and pastes it back where your cursor was.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square" alt="Platform">
  <img src="https://img.shields.io/badge/python-3.10%2B-3776AB?style=flat-square&logo=python&logoColor=white" alt="Python">
  <img src="https://img.shields.io/badge/ui-Flet-02569B?style=flat-square" alt="Flet">
  <img src="https://img.shields.io/badge/license-MIT-22C55E?style=flat-square" alt="License">
</p>

---

Windows keeps 25 clipboard entries and drops them on restart. This keeps
80, plus anything you save into your own folders, which never expires.

Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>V</kbd> anywhere. Type two
letters, hit <kbd>Enter</kbd>, and the text lands in the field you were
working in.

## What it does

**Automatic history.** Everything you copy shows up under Recent — text
and screenshots. Pin the ones you use often and they stay at the top,
safe from the cleanup that trims the rest.

**Your own folders.** Saved holds what you decide to keep, in a
dropdown or as chips — your choice. Rename or delete a folder with
everything inside from the same menu.

**Search that ranks.** Types across both tabs at once. Words come in
any order, accents are ignored, and title matches rank above matches
buried in the body.

**Fill-in templates.** Write `[[anything]]` in a saved text and the app
asks for it before pasting:

```
Hi [[name]], following up on [[topic]] from [[date]].
```

**Live links.** If what you copied is a web address and nothing else,
the row shows a link icon and its domain. One click opens it in the
browser.

**Rich text.** Pastes with font, size, bold and colour into Word and
Outlook. Plain text everywhere else.

**Bulk import.** Paste ten names and choose: one note per line, or all
of it in a single note.

**Respects password managers.** Windows defines
[clipboard formats](https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats)
that let an app say "do not record this". KeePass, Bitwarden, Windows
Credential Manager and Chrome's incognito mode all use them. pastepad
honours all four and doesn't even open the clipboard when one is
present.

## Install

**From source:**

```powershell
pip install -r requirements.txt
python main.py
```

Python 3.10 or newer from [python.org](https://www.python.org/downloads/).
Check **Add python.exe to PATH** on the first installer screen.

**Build an executable:**

```powershell
flet build windows --project pastepad --build-version 3.0.0
```

Or double-click `build.bat`, which runs the tests first and stops if any
fail.

Run it as administrator. Windows blocks global hotkeys from reaching a
normal process while an elevated window has focus.

## Usage

| Key | Action |
|---|---|
| <kbd>Ctrl</kbd> <kbd>Shift</kbd> <kbd>V</kbd> | Open the panel at the cursor |
| Type | Filter as you go |
| <kbd>↑</kbd> <kbd>↓</kbd> | Move through results |
| <kbd>Enter</kbd> | Paste |
| <kbd>Esc</kbd> | Close |

A single click on a row pastes it. The `···` button on the right opens
that entry's menu: open, paste, paste plain, copy, pin, edit, delete.

Six one-handed shortcut combinations are offered in the appearance
dialog, along with the accent colour, panel size and folder style.

Click into the field you want to fill **before** opening the panel. The
app records which window had focus and hands it back before pasting.

## Layout

```
main.py               entry point
pastepad/
  config.py           constants, paths, sizes
  registro.py         the only writer of errores.log
  modelo.py           the data and its rules
  busqueda.py         ranked search with a normalisation cache
  windows.py          clipboard, focus, window, autostart
  estilo.py           live colours and reusable pieces
  filas.py            the list rows
  ventanas.py         the dialogs
  app.py              coordinates the above
prueba.py             19 tests — run with `python prueba.py`
docs/FUNCIONES.md     all 133 functions, three lines each
```

`modelo.py`, `busqueda.py` and `windows.py` import neither Flet nor
tkinter. That's why the tests run without opening a window — and why
migrating the whole UI from tkinter to Flet reused 40% of the code
untouched.

## Where your data lives

Next to the executable, in plain files you can copy or back up:

```
snippets.json     saved texts and folders
historial.json    automatic history
config.json       colour, size, shortcut, folder style
imagenes\         copied screenshots
```

> [!WARNING]
> **The history is stored unencrypted.** Password-manager content is
> excluded automatically, but anything else sensitive you copy during
> the day does get written down. The broom button empties it and the
> pause button stops capture entirely. See [SECURITY.md](SECURITY.md).

> [!TIP]
> **Don't put the folder inside OneDrive.** Sync can lock the JSON
> files mid-write and you lose whatever you just saved.

## Why another one

[Ditto](https://github.com/sabrogden/Ditto) and
[CopyQ](https://github.com/hluk/CopyQ) are excellent and have years of
work behind them. This exists because my daily work needed two things
neither does out of the box: fill-in templates for notes I retype
constantly, and rich-text paste that survives into Outlook.

## Built with

Python and [Flet](https://flet.dev), which renders through Flutter on
the GPU — that's where the rounded corners, shadows and transitions
come from. Plus pywin32 for the clipboard and window handling, and
keyboard for the global shortcut.

Notes on architecture and past decisions are in [CLAUDE.md](CLAUDE.md).

## License

[MIT](LICENSE)
