# Changelog

All notable user-facing changes to GhostClip are recorded here. This project
uses semantic versions for published releases.

## [1.0.0] - 22 November 2026

Initial public Windows screenshot-only release.

### Added

- Floating, movable GhostClip widget with optional taskbar-left, taskbar-right
  and free-floating positioning, position lock and custom appearance.
- **Private Mode** Print Screen region capture using in-memory image handling.
- Windows clipboard history/cloud-sync opt-out metadata on screenshot items,
  published together with the image.
- Adjustable 5–300 second lifetime; manual clear, automatic expiry and
  ownership-aware clearing that does not erase newer clipboard items from
  other applications.
- Settings window with the ghost icon, draggable title and footer Close button.
  There are no Minimise, Maximise or native title-bar Close buttons.
- Live **Always on top** preference (enabled by default), including disabling
  the taskbar Z-order recovery when the preference is off.
- Even visual gaps between the three widget-position radio buttons in
  Settings > General, with Free-floating aligned to the clipboard lifetime input.
- Live widget-only scale setting from 70% to 150% (5% increments), 100% by
  default. The Settings window and screenshot region selector retain their
  original sizes; scaling affects the widget's actual layout and click targets.
- Optional per-user Launch with Windows without stealing foreground focus.
- Manual, metadata-only GitHub Releases update check.

### Deliberately excluded

- Ctrl+C interception and text-copy experiments (retained on separate research
  and prototype branches).
- Saving screenshots to disk or maintaining a capture library.
- Automatic cloud upload or automatic update installation.

**Limitations:** Windows Clipboard History (Win+V), cloud clipboard,
and third-party clipboard managers have not been independently certified
for privacy exclusion. Please test with non-sensitive content before relying
on GhostClip in privacy-sensitive workflows.
