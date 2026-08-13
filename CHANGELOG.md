# Changelog

All notable changes to this project are documented here.
This project follows [Semantic Versioning](https://semver.org/).

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
