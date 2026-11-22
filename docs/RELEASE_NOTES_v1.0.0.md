# GhostClip v1.0.0: screenshot-only release

Publisher: ZeroFlagLabs
Target: Windows 11 x64, portable
Source: public `main` branch, tag `v1.0.0`

## Included

- Print Screen region capture in Private Mode; screenshot pixels held in memory and copied to the Windows clipboard, not saved as image files by GhostClip.
- Opt-out markers for Windows clipboard history and cloud clipboard, attached before publishing the image.
- Automatic clipboard countdown (5 to 300 seconds), manual clear, ownership-aware expiry and attempt to clear on Private Mode OFF or exit.
- Release of Print Screen interception when Private Mode is OFF.
- Floating, movable, taskbar-adjacent widget with live custom appearance, visibility and colour controls.
- Optional Always on top preference (ON by default); OFF allows ordinary applications to cover the widget and disables taskbar-overlay Z-order recovery. The setting persists across launches.
- Widget-position options evenly spaced by visual gap across General, with Free-floating aligned to the left edge of the clipboard-lifetime input.
- Appearance > Widget scale adjusts only the floating GhostClip bar between 70% and 150% (default 100%), keeping the Settings window and snipping overlay unscaled. Widget hit targets and docking geometry follow the selected size.
- Four Settings tabs: How to Use, General, Appearance, About. Settings uses a compact, draggable custom title bar that restores the GhostClip ghost icon but omits Minimise, Maximise and ×; use the bottom Close button. The frame stays resizable.
- Per-user Launch with Windows, with non-activating startup mode.
- Metadata-only GitHub release check; does not send image or clipboard content, and never auto-installs.

## Intentionally excluded

- Ordinary Ctrl+C private text capture, files or rich content (prototypes preserved on separate branches).
- Automatic screenshot files, screenshot library, cloud storage or account integration.
- Automatic update download/installation.

## Important limitations

Windows itself and other apps can still access the clipboard while an image is present.
The history/cloud exclusion formats are present, but their effect in Windows
Clipboard History (Win+V) and cloud sync has not been independently certified.
Use synthetic test content when evaluating privacy. The executable is
currently unsigned and may trigger Windows security warnings.

## Licence

MIT Licence. Copyright © 2026 ZeroFlagLabs.

The absence of independent privacy certification is not a guarantee of
absolute clipboard secrecy. See the [security guidance](../SECURITY.md).
