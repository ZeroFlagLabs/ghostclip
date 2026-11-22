# GhostClip

**Lightweight, temporary screenshot copying for Windows.**

GhostClip is a small floating Windows widget for capturing a selected screen
region and pasting it elsewhere, without GhostClip saving a screenshot file.
It focuses on one task rather than becoming a general clipboard manager.

**Latest release: v1.0.0.** The Windows x64 application is distributed as
a portable, currently unsigned executable. Please see the
[privacy limitations](#privacy-model-and-limits): Windows Clipboard History,
cloud clipboard and third-party clipboard managers are not fully controlled
or independently certified by GhostClip.

## Features

- **Print Screen** captures a region when Private Mode is ON; **Esc** cancels.
- Clipboard image is published with Windows history/cloud exclusion formats.
- Automatic expiry after a configurable **5–300 seconds**, with manual clear.
- Ownership-aware clearing: GhostClip does not intentionally remove a newer
  clipboard item copied by another application.
- Small movable widget: **taskbar left**, **taskbar right** or **free-floating**.
- **Always on top**, enabled by default but optional. Disable it to let
  ordinary application windows cover GhostClip.
- Adjustable widget colours, opacity, shape, icons and position locking.
- **Widget scale** slider in Appearance: 70%–150% in 5% steps (default
  100%). Changes the floating widget and its clickable area, not Settings or
  the screenshot region selector.
- Optional **Launch with Windows** without stealing foreground focus.
- Four Settings tabs and a draggable custom title bar with GhostClip's ghost
  icon. The bottom **Close** button dismisses Settings.
- Optional update check against public GitHub Releases **metadata only**.
  GhostClip does not automatically download or install updates.

## Getting started

**Requirements:** Windows 11 x64. Use the portable Windows x64 release ZIP.

1. Extract the ZIP to a permanent folder. Don't run it directly from inside
   the ZIP, especially if you intend to enable Launch with Windows.
2. Run `GhostClip.exe`. There is no installer and no admin access is needed
   for the optional per-user startup entry.
3. Switch the widget to **PRIVATE ON**, press **Print Screen**, then drag over
   the area you want. On some keyboards, use **Fn + PrtSc**.
4. Paste into an image-capable application with **Ctrl+V** before the
   countdown expires. Press **Esc** to abandon a capture.
5. Click the countdown to clear the temporary item immediately. The app
   attempts to clear its own current item automatically at expiry or exit.

While PRIVATE OFF, GhostClip releases the Print Screen hook. **Ctrl+C is
never intercepted by v1.0.** Experimental text copying is deliberately excluded
from this release.

### Settings

**General** controls clipboard lifetime, start-in-private preference,
Launch with Windows, Always on top, positioning and position lock.
**Appearance** controls widget scale, colours, opacity, visibility and corner radii.
**How to Use** explains capture and paste. **About** contains version
information and a metadata-only update check.

The floating widget may be docked or placed freely; dragging a docked widget
switches it to Free-floating unless its position is locked. If Always on top
is unticked, GhostClip behaves as an ordinary window and may be covered by
other applications, including while placed near the taskbar.

## Privacy model and limits

GhostClip processes captured pixels in memory; **it does not write screenshot
images to files or upload captured pixels**. It puts the image onto the shared
Windows clipboard using a non-persistent data object and Windows-provided
opt-out formats for clipboard history and cloud clipboard.

GhostClip checks clipboard ownership before clearing its item, so it avoids
deleting newer content copied by another application. Saved preferences live
in `%LOCALAPPDATA%\ZeroFlagLabs\GhostClip\settings.json`, with no image
pixels in that file.

**The clipboard is not a secure vault.** Other applications may inspect its
contents while an image is available. Windows history/cloud opt-out must be
verified on the target machine, and third-party clipboard managers,
receiving applications and operating-system facilities may retain data
independently. The final acceptance checklist includes Win+V testing.
Do not use sensitive content until those checks are satisfied.

## Build from source

Requirements: Windows with the **.NET 10 SDK**.

```powershell
dotnet restore
dotnet build -c Release --no-restore
```

Create a repeatable portable ZIP from a **clean, committed source revision**:

```powershell
powershell -NoProfile -File .\scripts\Publish-Release.ps1 -ReleaseLabel v1.0.0
```

Output is written to `dist/`, with a SHA-256 checksum. Packaging never
replaces the app registered for Windows startup. GitHub Actions builds on a
Windows runner and retains the package as a CI artefact; it does not publish
a GitHub release.

## Project documentation

- [Release notes](docs/RELEASE_NOTES_v1.0.0.md) and [changelog](CHANGELOG.md)
- [Release verification checklist](docs/RELEASE_V1_CHECKLIST.md)
- [Release and packaging procedure](docs/RELEASING.md)
- [Developer guide](CONTRIBUTING.md) and [security reporting](SECURITY.md)

## Licence

GhostClip is released under the [MIT Licence](LICENSE).
Copyright © 2026 ZeroFlagLabs.

[Download releases](https://github.com/ZeroFlagLabs/ghostclip/releases) ·
[Source repository](https://github.com/ZeroFlagLabs/ghostclip)
