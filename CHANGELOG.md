# Changelog

All notable changes to this project are documented here.
This project follows [Semantic Versioning](https://semver.org/).

## [4.3.0] - 2026-08-13

### Added
- **Any saved text can be given a name**, not just bookmarks. Leaving it
  blank behaves as before and the first line is used, so saving two
  sentences still takes no extra typing. It matters when you build a
  library rather than jot things down: five email bodies that all open
  with "Hi team," are five identical rows, and the folder tells you they
  are bodies without telling you which is which.

### Fixed
- **The Save and Cancel buttons could end up half below the fold** in the
  New dialog, at the default panel size, once the name field appeared —
  which until now only happened when what you typed was a link. The
  dialog was handing the leftover height to the text box first and the
  buttons last. Measured on 4.2.0: 19 pixels of button instead of 32.
  Making the name field permanent would have turned that from a corner
  case into the normal one.

### Changed
- The writing box in the New dialog shows 140 pixels at a time instead of
  180. That is what the name field costs, and the box scrolls inside
  itself, so it is a change in how much you see at once rather than in
  how much you can write. Measured at four panel heights: the buttons now
  stay whole at every size except the 340-pixel minimum, where they sit
  below the fold exactly as they already did.
- 78 tests to 79.

## [4.2.0] - 2026-08-13

**Updating pastepad no longer costs you the clipboard you just copied,
and no longer leaves it closed.** Both of those were happening, and
neither said anything.

### Fixed
- **pastepad did not come back after being updated.** Installing over a
  running copy left it installed and shut down — no shortcut, no capture,
  nothing on screen to say so. Windows will only reopen a program that
  asked to be reopened, and pastepad never asked. Measured with the same
  API the installer uses: `bRestartable` was `False`.
- **Up to three seconds of copies were lost every time.** The history is
  written every few seconds rather than on every copy, and the only thing
  that forced it out was quitting from the tray. An installer closing
  pastepad — or Windows shutting down — took whatever had not been
  written yet. It now saves when Windows warns it, which it did not hear
  before: the window that handles the shortcut is a message-only window,
  and those cannot be enumerated, so the warning never reached it. A
  second window, never drawn and never in Alt+Tab, listens for it.
- What happened on an update depended on **whether the panel happened to
  be open**. With it tucked in the tray Windows classified pastepad as a
  program with no top-level window; with it open, as a normal one. Same
  program, two behaviours, decided by where you left it.
- **`--datos` rewrote the real autostart entry.** The option exists to
  test without touching your installation, and it kept every test run's
  data separate while pointing Windows' startup entry at a build that
  would be gone the next day.

### Changed
- 74 tests to 78.

## [4.1.0] - 2026-08-13

**This is the last version you have to install by hand.** From here on
pastepad checks for new releases and tells you inside the panel. If you
are coming from 4.0.0, you are carrying six faults, two of which
destroyed text without saying so.

### Added
- **A whole folder can be edited at once**, as one note per line, in the
  same box used to add them. Removing a hundred notes out of three
  thousand took a hundred trips through search, menu, edit and save; now
  it is one edit and one Save. Lines you did not touch keep their
  original note, so a bookmark keeps the name you gave it and a
  formatted note keeps its formatting. Notes that span several lines are
  left out and left whole, and the dialog says so. Before saving you are
  told how many notes will disappear, and backing out returns you to
  what you had typed.
- **A formatting bar for saved texts**: font, size, bold, italic, ten
  text colours, bullets, numbering, indent and clear formatting. The
  data format already carried all of it and the RTF that reaches Outlook
  and Word already understood it — there had simply never been a way to
  set it.
- **A notice when a new version is out**, shown inside the panel with a
  link to the release. It can be turned off under Appearance. Nothing is
  downloaded or installed on its own: that stays a decision you make.

### Fixed
- Text fields drew their focus underline in Windows' accent colour
  rather than the one you picked. A mint-accented app was showing 491
  pixels of orange.
- Colours were lost when reopening a formatted text, because the editor
  was loaded before the dialog was on screen.
- Right-clicking a folder did nothing on the drop-down and offered only
  rename and delete on the chips.

### Changed
- 51 tests to 74.

## [4.0.1] - 2026-08-13

The first day of real use turned up three faults, and measuring them
turned up three more. Two of the six destroyed text without saying so.

### Fixed
- **Editing a saved multi-line text kept only the first line.** Opening
  the editor and pressing Save — without touching anything — cut a
  100-line note from 5290 characters to 77. The cause was the order of
  two lines in an object initialiser: a WinUI `TextBox` with
  `AcceptsReturn` still `false` truncates whatever is assigned to
  `Text`, and `Text` came first. This is also the whole of the reported
  "a long text isn't visible when you edit it".
- **Saving recalculated the title from the first line**, so opening and
  saving a bookmark wiped the name it had been given.
- **Line breaks were read as `\n` when a `TextBox` returns `\r`.**
  Pasting 60 lines and choosing "one note per line" produced a single
  note holding all of them, and saved text reached other applications
  as one line.
- **Long text pushed the dialog buttons off screen** with no way to
  scroll to them. The body is now a grid: the footer keeps its place
  and the text box scrolls inside itself.
- **Folders could not be edited.** The Python version had that dialog
  and it was not ported. Rename and Delete were also unreachable while
  "All folders" was selected, which is the default.
- **A secondary button was white on white in the light theme**, since
  `Elevado` and `Tarjeta` are both `#FFFFFF` there.

### Added
- Bookmarks can be given a name. The field appears only when what you
  typed is a link, and left blank the URL is still used, so nothing
  ends up untitled. Searching a bookmark by URL was the only option
  before.
- Saved items carry a `{}` marker when they hold `[[fields]]`.
- Saved splits into Bookmarks, Templates and Notes by what the content
  is rather than where it was filed. The groups start collapsed and
  remember their state between sessions.
- "Edit the contents of \<folder\>…" in the folder menu, listing its
  texts with a pencil on each.

### Changed
- 45 tests to 51.

## [4.0.0] - 2026-08-13

Rewritten in C# with WinUI 3. Same program, same data, same shortcut —
but the shortcut now answers every time, which is the reason the rewrite
happened at all.

### Changed
- **The global shortcut sits on a message-only window** that lives as
  long as the process, not as long as the panel. The Python version
  registered a thread hotkey and pumped it from a worker; a message with
  no window attached is never dispatched to a window procedure, so it
  answered a few times and then stopped. Measured at 100 presses out of
  100, and again at 30 out of 30 on the installed build.
- **The clipboard reports by event** instead of being polled every 0.7
  seconds. `WM_CLIPBOARDUPDATE` arrives more than once per copy, so a
  sequence-number filter separates a real copy from a repeat.
- **One process instead of two.** Working set went from 207 MB to
  120 MB installed; the panel opens in 16–25 ms.
- The theme follows Windows and switches while the panel is open. Mica
  shows through unless one of the twelve backgrounds is picked.
- Settings rows stack instead of splitting into two columns. Two columns
  need a 380-pixel panel to be 455 wide, and the dropdown truncated from
  the left without ellipsis — `Ctrl + Shift + V` read as
  `l + Shift + V`, which looks like a different shortcut rather than a
  cut one.
- The program installs to `%LOCALAPPDATA%\Programs\pastepad` and keeps
  its data in `%LOCALAPPDATA%\pastepad`. Uninstalling can no longer
  touch the history, by construction rather than by asking first.

### Added
- An installer: one 47 MB `.exe`, no administrator prompt, an entry in
  Add or remove programs, and its own uninstaller.
- Language picker for the four translations that already existed but
  had no way to be chosen.
- Accessible names on the icon buttons and the eighteen accent swatches,
  which had none.

### Fixed
- Accents and opening punctuation across all four languages. The Spanish
  read `Como se enseñan` and `Si, borrar`; French had none at all.
- Every accent colour now clears WCAG AA contrast, checked by
  calculation. The subtitle colour failed on all twelve backgrounds.
- Saved titles are stored whole. A one-line summary was being written to
  `snippets.json` with its ellipsis inside the data.
- A file that exists but cannot be read is marked and never overwritten.
  `File.Exists` returns false when permission is denied, so an
  unreadable history could pass for a first run and get replaced with an
  empty one.

### Removed
- The Python implementation. It lives in the `ultima-version-python`
  tag and in `v3.0.1`.
- The window size presets. The panel resizes by dragging and remembers
  where it was left.

## [2.0.0] - 2026-08-12

### Changed
- **Rewritten as a package.** One 2,300-line file with a 950-line class
  became eight modules with one job each: `modelo` owns the data and
  never imports tkinter, `windows` holds everything that talks to the
  OS, `busqueda` does the ranking, `tema` holds the colours and the
  reusable pieces, `lista` draws the rows, `dialogos` has the popups,
  and `panel` only coordinates them.
- Saving is atomic — writes go to a temporary file and get moved into
  place, so a crash mid-write can no longer leave a truncated JSON.
- Roomier layout: bigger rows, a real type scale, spacing on a 4px grid,
  and a palette with mid-tones instead of jumping from card to text.

### Added
- 15 tests covering folders, pinning, trimming, templates and search
  ranking. They run without opening a window, and the release workflow
  runs them before building.

## [1.2.0] - 2026-08-12

### Added
- The shortcut is configurable, and now defaults to Ctrl+Shift+V, which
  the left hand reaches without letting go of the mouse
- Its own icon, bundled into the executable and shown on dialogs
- Error log: an unhandled failure now writes to `errores.log` and says so,
  instead of the window vanishing with no trace
- Illustrations in the README, drawn as SVG so they follow GitHub's theme

### Fixed
- Rounded corners actually apply: the region was being set on Tk's inner
  widget rather than the window Windows knows about

### Changed
- The build embeds version metadata, so the file has a name and an author
  in Properties instead of showing up blank

## [1.1.0] - 2026-08-11

### Added
- Delete a folder and everything inside it, from a labelled button or
  the folder chip's right-click menu
- Rename folders — saved texts move with them
- Multi-select mode: tick several rows and delete them together
- Four panel sizes — mini, small, medium, large — from the appearance dialog.
  Free edge-dragging was tried and dropped: CustomTkinter does not
  repaint its widgets reliably when a window resizes dozens of times a
  second, which left stripes of the previous frame on screen and made
  the whole panel sluggish. Applying a size once does neither.
- Honours the Windows clipboard formats that password managers use to
  mark content as private — that content is never even read
- Pause button that stops capture without closing the app

### Changed
- The clipboard is only read when Windows' sequence number changes,
  instead of every 900 ms regardless
- Search index caches normalised text per entry; rebuilding it dropped
  from ~27 ms to ~0.02 ms
- The resize cursor only updates when the edge zone actually changes
- Hover and selection recolour two rows instead of redrawing the whole
  canvas — with a large window that alone was eating most of a core


## [1.0.0] - 2026-08-11

### Added
- Automatic clipboard history with pinning, text and images
- Saved texts organised into folders
- Ranked search across both tabs, accent-insensitive, words in any order
- Fill-in templates using `[[field]]` placeholders
- Rich text paste into Word and Outlook; plain text with Ctrl+Enter
- Bulk import: one note per line, or everything in a single note
- Six accent colours; follows the Windows light/dark theme
- Starts with Windows
