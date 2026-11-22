# GhostClip v1.0 verification status

This records what was checked before the first public release of GhostClip
for Windows 11 x64. This is not a security certification.

## Verified

- [x] Screenshot-only source: Print Screen is intercepted in Private Mode.
  The v1 source has no Ctrl+C interception or text-copy prototype.
- [x] Release build succeeds with no warnings or errors in the Windows
  development environment, using .NET 10.
- [x] The single-file Windows x64 executable launches and the floating
  widget and Settings window are accessible.
- [x] Settings has a draggable ghost title bar, footer Close button, no
  native Minimise/Maximise/Close caption buttons, and a resizable frame.
- [x] Widget scale 70%, 100% and 150% changes the actual window size,
  not Settings or the screenshot overlay. Scale survives application restart.
- [x] Taskbar left/right positioning, Free-floating alignment and widget
  controls respond after scaling.
- [x] Always on top can be enabled/disabled and persists across launches;
  disabling it stops taskbar Z-order recovery.
- [x] Release packaging uses a clean committed Git worktree, embeds the MIT
  licence, creates a SHA-256 checksum, verifies ZIP extraction and doesn't
  overwrite the Windows startup binary.
- [x] The app includes three Microsoft Windows clipboard history/cloud
  opt-out data formats when publishing its protected image.
- [x] Source and distribution exclude user settings files, image captures,
  text-copy diagnostic logs, debug dumps and experimental Ctrl+C code.
- [x] Local git history and text-copy experiments were retained privately;
  the published source was created as a separate clean repository.

## Still requiring manual confirmation on individual devices

These are important limitations, not claims of completed tests:

- [ ] Capture harmless sample content with Print Screen, paste with Ctrl+V
  and confirm the exact image does not appear in Windows Clipboard History
  (Win+V). Format markers alone do not establish this.
- [ ] Confirm the GhostClip clipboard item expires at the configured timeout.
- [ ] After copying newer content in another application, confirm GhostClip's
  timeout or exit does not remove the newer item.
- [ ] Confirm the actual Windows sign-in startup procedure and monitor layouts.
- [ ] Check clipboard history/cloud-synchronisation settings and third-party
  clipboard managers on each intended computer.

GhostClip cannot guarantee secrecy from other programs that can read the
shared clipboard, image consumers, screen recording tools or OS processes.
Use non-sensitive sample material when evaluating the utility.

## Distribution

- Licence: **MIT**, © 2026 ZeroFlagLabs.
- Windows executable: currently **unsigned**. Windows may display a
  reputation or unsigned-app security warning.
- The public v1.0 repository deliberately excludes experimental text-copy
  work and internal developer investigation history.
