# Changelog

All notable changes to this project are documented here.
This project follows [Semantic Versioning](https://semver.org/).

## [1.2.0] - 2026-08-12

### Added
- Its own icon, bundled into the executable and shown on dialogs
- Error log: an unhandled failure now writes to `errores.log` and says so,
  instead of the window vanishing with no trace
- Illustrations in the README, drawn as SVG so they follow GitHub's theme

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
