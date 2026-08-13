<div align="center">

<img src="docs/logo.svg" alt="" width="120">

# pastepad

A clipboard manager for Windows.

[English](README.md) · [Español](docs/README.es.md) · [MIT](LICENSE)

</div>

Windows keeps 25 clipboard entries and forgets them when you restart.
pastepad keeps 80, plus whatever you file into your own folders, which
never expire.

Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>V</kbd> anywhere. Type two
letters, hit <kbd>Enter</kbd>, and the text lands in the field you were
working in.

<div align="center">
  <img src="docs/capturas/reciente.png" alt="Recent tab" width="270">
  <img src="docs/capturas/guardados.png" alt="Saved tab" width="270">
  <img src="docs/capturas/apariencia.png" alt="Appearance dialog" width="270">
</div>

## What it does

Everything you copy shows up under Recent, text and screenshots alike.
Pin what you use often and it stays on top, out of reach of the trim
that clears the rest.

Saved texts live in folders you name. Write `[[anything]]` in one and
pastepad asks for it before pasting:

```
Hi [[name]], following up on [[topic]] from [[date]].
```

Links get their own treatment. If what you copied is a web address and
nothing else, the row shows a link icon and the domain instead of a
character count, and clicking opens the browser rather than pasting.
Under Saved they sit in a separate collapsible group, because opening a
bookmark and pasting a note are two different gestures and mixing them
means reading the whole list to find either.

Search runs across both tabs at once. Words can come in any order,
accents are ignored, and a match in the title outranks one buried in the
body.

Some things never get recorded. Windows defines four
[clipboard formats](https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats)
an application uses to say "don't store this", and KeePass, Bitwarden,
Windows Credential Manager and Chrome's incognito windows all set them.
pastepad honours all four and drops that content.

## Using it

| Key | Action |
|:--|:--|
| <kbd>Ctrl</kbd> <kbd>Shift</kbd> <kbd>V</kbd> | Open the panel at the cursor |
| Type | Filter as you go |
| <kbd>↑</kbd> <kbd>↓</kbd> | Move through results |
| <kbd>Enter</kbd> | Paste |
| <kbd>Esc</kbd> | Close |

Click into the field you want to fill before opening the panel. pastepad
records which window had focus and hands it back before pasting.

The X only hides the panel. Closing it for good is `taskkill /IM
pastepad.exe /F`, or Exit from the tray icon.

## Installing

> **Pending.** pastepad is being rewritten in C# with WinUI 3, and that
> version has no installer yet. Instructions go here once there is
> something to download.

The last published release is [v3.0.1](https://github.com/Josemgu/pastepad/releases/latest),
built with Python and Flet. The rewrite exists because the global
shortcut in that version stops responding after a few presses, and the
cause turned out to be structural rather than a bug to patch. `PLAN.md`
and `TRASPASO.md` have the reasoning.

## The Windows warning

The first run shows *"Windows protected your PC"*. Choose **More info**,
then **Run anyway**.

An MIT licence and published source do not prevent this. SmartScreen
reads neither. It looks at whether the binary carries a signature and at
how many people have downloaded it without incident, and a new unsigned
executable has neither.

A free certificate from [SignPath](https://signpath.org/) is being
requested for the project, which is what makes the warning go away for
good. Reputation then carries across releases instead of resetting with
each one. Until that lands, these are the options:

| Option | Effect | Cost |
|---|---|---|
| Accept the warning | One click, once | 0 |
| Check the published SHA256 | Verifiable download, warning stays | 0 |
| Free certificate for open source ([SignPath](https://signpath.org/), [OSSign](https://ossign.org/)) | Real signature, reputation accumulates | 0 |
| [Azure Trusted Signing](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options) | Same, run by Microsoft | ~$10/month |
| Microsoft Store | Removes the warning entirely | Developer account |

Self-signing does not help: Windows won't trust a certificate that
doesn't come from a recognised authority. Extended validation
certificates no longer grant instant reputation either — that stopped
being true years ago.

Every release ships a `SHA256.txt` so you can check the download matches
what the build produced.

## Your data

In `%LOCALAPPDATA%\pastepad`, as plain files you can copy or back up:

```
snippets.json     saved texts and folders
historial.json    automatic history
config.json       language, theme, colour, shortcut, size
imagenes\         copied screenshots
```

The history is stored unencrypted. Password-manager content is dropped
automatically, but anything else sensitive you copy does get written
down. The broom in the footer empties the history and the pause button
stops capture. See [SECURITY.md](SECURITY.md).

Keeping the folder inside OneDrive is a bad idea. Sync can lock the JSON
files mid-write.

## Making it yours

Four languages: English, Español, Português, Français. Pick one under
Appearance, and it persists between sessions.

Twelve backgrounds and eighteen accent colours. "Follow Windows" tracks
the system theme and switches live when you change it. Every accent
clears WCAG AA contrast against the text drawn on it, checked by
calculation rather than by eye.

The panel resizes by dragging its edges, between 300×340 and 720×1100,
and remembers where you left it.

## Design

The interface was drawn before it was built. There are 35 SVG mockups in
[docs/mockups](docs/mockups), and
[ESPECIFICACION-UI.md](docs/ESPECIFICACION-UI.md) has the palette, the
measurements and the behaviour behind them, with the exact values.

Two caveats live in that document: mockups 20 and 33 were never checked
against the running program, and 26 and 27 draw Windows' own SmartScreen
dialogs rather than anything pastepad controls.

## Why another one

[Ditto](https://github.com/sabrogden/Ditto) and
[CopyQ](https://github.com/hluk/CopyQ) are good programs with years of
work behind them. This one exists because my daily work needed two
things neither does without setting them up: fill-in templates for notes
I retype constantly, and rich-text paste that survives into Outlook.

---

<div align="center">
<sub>by <a href="https://github.com/Josemgu">Jose Miguel Ortiz</a></sub>
</div>
