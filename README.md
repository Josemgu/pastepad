# pastepad

A clipboard manager for Windows. Keeps what you copy, stores what you
choose to keep, and pastes it back where your cursor was.

Windows keeps 25 clipboard entries and drops them on restart. This keeps
80, plus anything you save into your own folders, which never expires.

Press `Ctrl+Alt+V` anywhere. Type two letters, hit Enter, and the text
lands in the field you were working in.

---

## What it does

**Automatic history.** Everything you copy shows up in the Recent tab —
text and screenshots. Pin the ones you use often and they stay at the
top, safe from the cleanup that trims the rest.

**Your own folders.** The Saved tab holds what you decide to keep,
organized however you want. Nothing here expires.

**Search that ranks.** Type across both tabs at once. Words can come in
any order — `fin quest` finds `LMS-FINQ: Finance Questions`. Accents are
ignored, so `informacion` matches `información`. Matches in the title
rank above matches buried in the body.

**Fill-in templates.** Write `[[anything]]` in a saved text and the app
asks for it before pasting:

```
The leader from [[unit]] reported [[issue]] on [[date]].
```

Pick that entry and you get three fields to fill, then the finished
sentence goes straight into your form.

**Rich text.** Pastes with font, size, bold and color into Word and
Outlook. Plain text everywhere else. `Ctrl+Enter` forces plain.

**Bulk import.** Paste a list of ten names and choose: one note per
line, or all of it in a single note. Numbering and bullets get stripped
if you want.

---

## Install

You need Python 3.10 or newer. Get it from
[python.org](https://www.python.org/downloads/) and check
**Add python.exe to PATH** on the first installer screen — nothing works
without it.

```
pip install customtkinter keyboard pyperclip pywin32
python snippets_v13.pyw
```

To build a standalone `.exe`:

```
pip install pyinstaller
pyinstaller --onefile --noconsole --name Snippets --collect-all customtkinter snippets_v13.pyw
```

The result lands in `dist\`. Move it wherever you want it to live and
run it once from there — the app registers its own startup entry using
whatever path it was launched from.

Run it as administrator. Windows blocks global hotkeys from reaching a
normal process while an elevated window has focus, so without this the
shortcut fails in exactly the apps where you need it most.

A full walkthrough for people who have never installed Python is in
[COMO-INSTALAR.txt](COMO-INSTALAR.txt) (Spanish).

---

## Usage

| Key | Action |
|---|---|
| `Ctrl+Alt+V` | Open the panel at the cursor |
| Type | Filter as you go |
| `↑` `↓` | Move through results |
| `Enter` | Paste |
| `Ctrl+Enter` | Paste as plain text |
| `Esc` | Close |

A single click on a row pastes it. The three icons on the right — pin,
edit, delete — act on that row instead.

Click into the field you want to fill *before* opening the panel. The
app records which window had focus and hands it back before sending the
paste.

---

## Where your data lives

Next to the executable, in plain files you can copy or back up:

```
snippets.json     saved texts and folders
historial.json    automatic history
config.json       accent color
imagenes\         copied screenshots
```

Move that folder to another machine and everything comes with it.

---

## Notes

**The history stores everything you copy**, unencrypted, on your own
disk. That includes passwords and personal data if you copy them during
the day. The broom button empties it. Worth knowing before you install
this on a shared machine.

**Defender will flag the executable** the first time. PyInstaller output
isn't code-signed, and a signing certificate costs a few hundred dollars
a year. Click *More info* → *Run anyway*.

**Don't put the folder inside OneDrive.** Sync can lock the JSON files
mid-write and you lose whatever you just saved.

---

## Built with

Python, [CustomTkinter](https://github.com/TomSchimansky/CustomTkinter),
pywin32 and keyboard.

The list is drawn on a canvas rather than built from widgets. Rendering
one widget per row meant rebuilding hundreds of them on every keystroke,
which made the search unusable once the history filled up.

## License

MIT
