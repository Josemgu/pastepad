# Security

## What this app stores, and where

Everything lives in plain files next to the executable:

| File | Contents |
|---|---|
| `historial.json` | Everything you copied, in plain text |
| `snippets.json` | Texts you chose to save |
| `imagenes/` | Screenshots you copied, as BMP |

**None of it is encrypted.** Anyone with access to that folder, or to a
backup of it, can read everything you have copied.

## What is *not* stored

Windows defines
[clipboard formats](https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats)
that let an application mark content as private. pastepad honours all
four and does not even open the clipboard when one is present:

- `Clipboard Viewer Ignore`
- `ExcludeClipboardContentFromMonitorProcessing`
- `CanIncludeInClipboardHistory` set to 0
- `CanUploadToCloudClipboard` set to 0

KeePass, Bitwarden, Windows Credential Manager and Chrome's incognito
mode all set these. So passwords copied from a password manager do not
end up in the history.

A password you type by hand and copy yourself is *not* covered — nothing
marks it as private. The pause button exists for those moments.

## Recommendations

- Don't put the folder in OneDrive, Dropbox or any sync service. A
  plain-text record of everything you copy does not belong in the cloud.
- Empty the history regularly if you share the machine.
- Pause capture before working with anything sensitive.
- On a managed or work computer, check with whoever handles security
  policy before installing this.

## About the executable

The `.exe` is not code-signed — a certificate costs several hundred
dollars a year. Windows Defender will warn on first run; this is a
[known PyInstaller false positive](https://github.com/pyinstaller/pyinstaller/issues/6754).

If you would rather not trust a binary from the internet: the release
workflow builds it on a clean GitHub runner from the source in this
repository, the build log is public, and every release ships a SHA256
checksum. Or build it yourself with `build.bat` — it takes two minutes.

## Reporting a problem

Open an issue. If it is something you would rather not post publicly,
say so in the issue without details and I will follow up.
