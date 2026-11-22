# GhostClip release and verification

The public `main` branch contains the **screenshot-only v1.x** source. It
intentionally excludes all experimental Ctrl+C/text-capture implementations.
The release is published under the MIT Licence, copyright 2026 ZeroFlagLabs.

## Build and package

Requirements: Windows, the .NET 10 SDK and Git.

From a clean, committed checkout:

```powershell
dotnet restore
dotnet build -c Release --no-restore -warnaserror
powershell -NoProfile -File .\scripts\Publish-Release.ps1 -ReleaseLabel v1.0.1
```

The packaging script checks out the current committed revision in a temporary
detached Git worktree, publishes a self-contained single-file win-x64
`GhostClip.exe`, copies the README, changelog, release notes and MIT licence,
and generates:

- `dist/GhostClip-v1.0.1-win-x64.zip`
- `dist/GhostClip-v1.0.1-win-x64.zip.sha256`

It verifies the executable's SHA-256 after extraction, then cleans up the
temporary worktree. It never overwrites the user's installed or Windows-startup
executable. Output is ignored by Git.

The GitHub Actions Windows workflow runs builds and uploads CI artefacts.
It does not create tags or publish releases automatically.

## Native Windows checks

See [verification status](RELEASE_V1_CHECKLIST.md) for completed checks and
important remaining tests. Always use non-sensitive sample screenshots.

The app sets clipboard-history/cloud exclusion formats and clears only its
own current clipboard item, but cannot control third-party managers or the
clipboard's behaviour in every Windows installation. The current release is
**not independently security-certified**.

## Releasing a future version

1. Make source and documentation changes on a branch; complete local and
   Windows testing.
2. Update version in `GhostClip.csproj`, `CHANGELOG.md`, release notes
   and user-facing documentation. If the vector ghost changes, regenerate
   `Assets/GhostClip.ico` using `scripts/Generate-Icon.ps1` on Windows.
   Update the release-notes filename in the packaging script as appropriate.
3. Confirm the Git working tree is clean, no sensitive files are tracked,
   no experimental text-copy code was introduced unintentionally and the
   build passes on Windows.
4. Run the packaging script with a **new** version label; it refuses to
   overwrite an existing release package.
5. Tag the approved source commit and attach the ZIP, checksum and release
   notes using GitHub Releases.
6. Verify the release link and in-app update-check metadata. Sign the
   executable if a suitable code-signing certificate becomes available.

The public source and packaged binaries are licensed under MIT. The portable
Windows executable is not currently Authenticode-signed.
