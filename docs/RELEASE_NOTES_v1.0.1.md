# GhostClip v1.0.1: matching Windows executable icon

Release date: 22 November 2026
Platform: Windows 11 x64 · Portable · Self-contained

## What changed

- GhostClip.exe now includes the **same ghost artwork** that appears in the
  application's Settings title bar.
- The icon is embedded as a proper Windows application icon, so it is visible
  in File Explorer, desktop shortcuts, Windows search and other shell views.
- It includes nine resolutions: 16, 20, 24, 32, 40, 48, 64, 128 and
  256 pixels.
- A reproducible icon-generation script is included in source control:
  `scripts/Generate-Icon.ps1` renders the existing
  `GhostClipWindowIcon` resource from `App.xaml`. The vector and EXE icon
  are therefore based on a single artwork source.

## What did not change

- No changes to screenshot capture, Print Screen handling, private-mode
  behaviour, clipboard expiry, Windows clipboard history/cloud opt-out
  formats, widget docking, sizing, or Settings.
- Experimental Ctrl+C text copying remains excluded from the public release.
- Existing preferences remain compatible; no migration is required.

## Installation

Extract the Windows x64 ZIP to a permanent folder and run `GhostClip.exe`.
No .NET installation is needed. If you use **Launch with Windows**, update
the startup entry through GhostClip Settings after moving or upgrading the EXE.

The application is unsigned. Windows may display a security warning. The
clipboard is shared with other applications; history/cloud exclusion and
third-party clipboard handling are not independently security-certified.
See the [verification checklist](RELEASE_V1_CHECKLIST.md).

## Licence

MIT Licence. Copyright (c) 2026 ZeroFlagLabs.
