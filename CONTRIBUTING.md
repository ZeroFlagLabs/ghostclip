# Contributing to GhostClip

GhostClip is a focused Windows screenshot utility, not a general clipboard
manager. Contributions should preserve its core promise: **do not write
screenshot files**, and do not clear clipboard data belonging to another app.

## Development environment

- Windows 11 and the .NET 10 SDK
- WPF support (included in the Windows desktop SDK)
- Visual Studio or an editor of your choice

Run from the project root:

```powershell
dotnet restore
dotnet build -c Release --no-restore
```

If you update the ghost in `App.xaml`, regenerate and verify the matching
native EXE icon on Windows:

```powershell
powershell -NoProfile -STA -File .\scripts\Generate-Icon.ps1
powershell -NoProfile -File .\scripts\Test-EmbeddedIcon.ps1
```

Rebuild GhostClip before running the second command. The icon verification
also runs in GitHub Actions.

For a clean, committed-source Windows x64 package:

```powershell
powershell -NoProfile -File .\scripts\Publish-Release.ps1 -ReleaseLabel v1.0.1-dev
```

Build output, local settings and diagnostics are ignored by Git. The script
packages only files explicitly chosen for distribution.

## Change guidelines

- Keep v1.0 scoped to Print Screen image capture. Discuss new clipboard
  interception paths separately; an unsupported fallback to ordinary Ctrl+C
  would violate the intended privacy model.
- Prefer small, well-commented changes to keyboard hooks, clipboard ownership,
  OS session handling or window Z-order logic. Explain the Windows API
  assumptions and why each privacy-sensitive branch is safe.
- Validate changes with **synthetic, non-sensitive** sample content. Test
  clipboard replacement by other apps and confirm the expiry timer does not
  erase their newer content.
- Include a before/after description and the exact Windows/.NET versions
  tested when opening a pull request.
- Do not submit screenshots, crash dumps, clipboard data, registry exports or
  logs containing private content. The app deliberately does not log captures.

Automated GitHub Actions builds check packaging and compilation. Native
keyboard, taskbar and clipboard privacy acceptance remains a manual Windows
test. See [release checklist](docs/RELEASE_V1_CHECKLIST.md).

## Licence

Contributions are accepted on the basis that they can be distributed under
the project's [MIT Licence](LICENSE). Please do not submit code you do not
have permission to redistribute. Avoid committing personal settings,
credentials, screenshots or diagnostic data.
