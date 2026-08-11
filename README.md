<p align="center">
  <img src="docs/banner.svg" alt="pastepad" width="880">
</p>

<p align="center">
  <b>A clipboard manager for Windows.</b><br>
  Keeps what you copy, stores what you choose to keep,<br>
  and pastes it back where your cursor was.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square" alt="Platform">
  <img src="https://img.shields.io/badge/python-3.10%2B-3776AB?style=flat-square&logo=python&logoColor=white" alt="Python">
  <img src="https://img.shields.io/badge/license-MIT-22C55E?style=flat-square" alt="License">
</p>

---

Windows keeps 25 clipboard entries and drops them on restart. This keeps
80, plus anything you save into your own folders, which never expires.

Press <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>V</kbd> anywhere. Type two
letters, hit <kbd>Enter</kbd>, and the text lands in the field you were
working in.

<p align="center">
  <img src="docs/screenshot.png" alt="The panel, open next to the cursor" width="380">
</p>

## What it does

<table>
<tr>
<td width="50%" valign="top">

### Automatic history

Everything you copy shows up in **Recent** — text and screenshots. Pin
the ones you use often and they stay at the top, safe from the cleanup
that trims the rest.

</td>
<td width="50%" valign="top">

### Your own folders

**Saved** holds what you decide to keep, organized however you want.
Right-click a folder chip to rename it, or delete it along with
everything inside. Nothing here expires.

</td>
</tr>
<tr>
<td valign="top">

### Search that ranks

Type across both tabs at once. Words come in any order — `fin quest`
finds `LMS-FINQ: Finance Questions`. Accents are ignored, so
`informacion` matches `información`. Title matches rank above matches
buried in the body.

</td>
<td valign="top">

### Rich text

Pastes with font, size, bold and color into Word and Outlook. Plain text
everywhere else. <kbd>Ctrl</kbd> + <kbd>Enter</kbd> forces plain.

</td>
</tr>
</table>

### Fill-in templates

Write `[[anything]]` in a saved text and the app asks for it before
pasting:
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 880 240" width="880" height="240" role="img" aria-label="pastepad - clipboard manager for Windows">
  <title>pastepad</title>
  <defs>
    <style>
      .bg    { fill: #161616; }
      .card  { fill: #242424; }
      .accent{ fill: #3B82F6; }
      .t     { fill: #F0F0F0; font-family: 'Segoe UI', system-ui, sans-serif; }
      .dim   { fill: #8E8E8E; font-family: 'Segoe UI', system-ui, sans-serif; }
      .on    { fill: #FFFFFF; font-family: 'Segoe UI', system-ui, sans-serif; }
      .mono  { font-family: 'Cascadia Code', Consolas, monospace; }
    </style>
  </defs>

  <rect class="bg" width="880" height="240" rx="14"/>

  <text class="t" x="56" y="88" font-size="46" font-weight="600">pastepad</text>
  <text class="dim" x="56" y="122" font-size="17">A clipboard manager for Windows</text>

  <rect class="card" x="56" y="150" width="230" height="34" rx="8"/>
  <text class="dim mono" x="72" y="172" font-size="14">Ctrl</text>
  <text class="dim" x="98" y="172" font-size="14">+</text>
  <text class="dim mono" x="110" y="172" font-size="14">Alt</text>
  <text class="dim" x="136" y="172" font-size="14">+</text>
  <text class="dim mono" x="148" y="172" font-size="14">V</text>
  <text class="dim" x="176" y="172" font-size="13">anywhere</text>

  <g transform="translate(560, 34)">
    <rect class="card" width="268" height="172" rx="12" fill="#1F1F1F"/>
    <rect class="card" x="12" y="14" width="244" height="26" rx="7"/>
    <circle cx="30" cy="27" r="5" fill="none" stroke="#8E8E8E" stroke-width="1.6"/>
    <line x1="34" y1="31" x2="38" y2="35" stroke="#8E8E8E" stroke-width="1.6" stroke-linecap="round"/>
    <text class="dim" x="48" y="32" font-size="11">Search</text>

    <rect class="accent" x="12" y="50" width="118" height="20" rx="10"/>
    <text class="on" x="47" y="64" font-size="10">Recent</text>
    <rect class="card" x="138" y="50" width="118" height="20" rx="10"/>
    <text class="dim" x="169" y="64" font-size="10">Saved</text>

    <rect class="accent" x="12" y="80" width="244" height="30" rx="8"/>
    <text class="on" x="24" y="93" font-size="10">LMS-FINQ: Finance Questions</text>
    <text class="on" x="24" y="105" font-size="9" opacity="0.75">27 characters</text>

    <rect class="card" x="12" y="116" width="244" height="30" rx="8"/>
    <text class="t" x="24" y="129" font-size="10">The leader from [[unit]] reported...</text>
    <text class="dim" x="24" y="141" font-size="9">Cases</text>

    <rect class="card" x="12" y="152" width="244" height="12" rx="6"/>
  </g>
</svg>
```
The leader from [[unit]] reported [[issue]] on [[date]].
```

Pick that entry, fill three fields, and the finished sentence goes
straight into your form.

### Bulk import

Paste a list of ten names and choose: one note per line, or all of it in
a single note. Numbering and bullets get stripped if you want.

## Install

<details open>
<summary><b>Quick start</b></summary>

<br>

You need Python 3.10 or newer from
[python.org](https://www.python.org/downloads/). Check **Add python.exe
to PATH** on the first installer screen — nothing works without it.

```powershell
pip install -r requirements.txt
python pastepad.pyw
```

</details>

<details>
<summary><b>Build a standalone .exe</b></summary>

<br>

```powershell
pip install pyinstaller
pyinstaller --onefile --noconsole --name pastepad --collect-all customtkinter pastepad.pyw
```

Or just double-click `build.bat`.

The result lands in `dist\`. Move it wherever you want it to live and run
it once from there — the app registers its own startup entry using
whatever path it was launched from.

Run it as administrator. Windows blocks global hotkeys from reaching a
normal process while an elevated window has focus, so without this the
shortcut fails in exactly the apps where you need it most.

</details>

<details>
<summary><b>Never installed Python before?</b></summary>

<br>

There's a full step-by-step walkthrough in Spanish:
[COMO-INSTALAR.txt](COMO-INSTALAR.txt)

</details>

## Usage

| Key | Action |
|---|---|
| <kbd>Ctrl</kbd> <kbd>Alt</kbd> <kbd>V</kbd> | Open the panel at the cursor |
| Type | Filter as you go |
| <kbd>↑</kbd> <kbd>↓</kbd> | Move through results |
| <kbd>Enter</kbd> | Paste |
| <kbd>Ctrl</kbd> <kbd>Enter</kbd> | Paste as plain text |
| <kbd>Esc</kbd> | Close |

A single click on a row pastes it. The three icons on the right — pin,
edit, delete — act on that row instead.

Click into the field you want to fill **before** opening the panel. The
app records which window had focus and hands it back before sending the
paste.

## Where your data lives

Next to the executable, in plain files you can copy or back up:

```
snippets.json     saved texts and folders
historial.json    automatic history
config.json       accent color
imagenes\         copied screenshots
```

Move that folder to another machine and everything comes with it.

## Notes

> [!WARNING]
> **The history stores everything you copy**, unencrypted, on your own
> disk. That includes passwords and personal data if you copy them
> during the day. The broom button empties it. Worth knowing before you
> install this on a shared machine.

> [!NOTE]
> **Defender will flag the executable** the first time. PyInstaller
> output isn't code-signed, and a certificate costs a few hundred
> dollars a year. Click *More info* → *Run anyway*.

> [!TIP]
> **Don't put the folder inside OneDrive.** Sync can lock the JSON files
> mid-write and you lose whatever you just saved.

## Why another one

[Ditto](https://github.com/sabrogden/Ditto) and
[CopyQ](https://github.com/hluk/CopyQ) are excellent and have years of
work behind them. I built this because my daily work needed two things
neither one does out of the box: fill-in templates for repetitive case
notes, and rich-text paste that survives into Outlook.

## Built with

Python, [CustomTkinter](https://github.com/TomSchimansky/CustomTkinter),
pywin32 and keyboard.

The list is drawn on a canvas rather than built from widgets. Rendering
one widget per row meant rebuilding hundreds of them on every keystroke,
which made search unusable once the history filled up.

## License

[MIT](LICENSE)
