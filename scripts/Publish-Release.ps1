param(
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]*$')]
    [string]$ReleaseLabel = 'v1.0.1',
    [string]$OutputDirectory = 'dist'
)

# Publish only committed source in a temporary detached Git worktree.
# This never overwrites the executable registered for Windows startup.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
[void](Get-Command git -ErrorAction Stop)
[void](Get-Command dotnet -ErrorAction Stop)

$dirty = @(git -C $repo status --porcelain)
if ($LASTEXITCODE -ne 0 -or $dirty.Count -gt 0) {
    throw 'Commit changes before building a reproducible release package.'
}

$commit = (git -C $repo rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $commit.Length -ne 40) {
    throw 'Cannot resolve the source commit.'
}

$destination = if ([IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory
} else {
    Join-Path $repo $OutputDirectory
}

$packageName = 'GhostClip-{0}-win-x64' -f $ReleaseLabel
$package = Join-Path $destination $packageName
$zip = Join-Path $destination ($packageName + '.zip')
$shaFile = $zip + '.sha256'

foreach ($target in @($package, $zip, $shaFile)) {
    if (Test-Path -LiteralPath $target) {
        throw ('Output already exists: ' + $target +
            '. Use a new release label instead of overwriting it.')
    }
}

$worktree = Join-Path $env:TEMP (
    'GhostClip-Publish-{0}' -f [guid]::NewGuid().ToString('N'))
$added = $false

try {
    & git -C $repo worktree add --detach $worktree $commit
    if ($LASTEXITCODE -ne 0) { throw 'Git worktree creation failed.' }
    $added = $true

    # Fail if experimental text-copy interception slipped into this
    # screenshot-only release branch.
    $sources = @(Get-ChildItem -LiteralPath $worktree -Filter '*.cs' -File)
    $unexpected = @(
        $sources | Select-String -Pattern 'CapturePrivateTextAsync|SelectedTextReader')
    if ($unexpected.Count -gt 0) {
        throw 'Experimental private Ctrl+C code is not allowed in v1.0.'
    }

    $publish = Join-Path $worktree 'publish-output'

    & dotnet publish (Join-Path $worktree 'GhostClip.csproj') `
        -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=None -o $publish
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

    $exe = Join-Path $publish 'GhostClip.exe'
    $published = @(Get-ChildItem -LiteralPath $publish -File)
    if ($published.Count -ne 1 -or $published[0].Name -ne 'GhostClip.exe') {
        throw 'Expected exactly one self-contained GhostClip.exe.'
    }

    [void](New-Item -ItemType Directory -Path $package -Force)
    $included = @()
    Copy-Item -LiteralPath $exe -Destination (Join-Path $package 'GhostClip.exe')
    $included += (Join-Path $package 'GhostClip.exe')

    foreach ($entry in @(
        @{ Source = 'README.md'; Target = 'README.md' },
        @{ Source = 'CHANGELOG.md'; Target = 'CHANGELOG.md' },
        @{ Source = 'docs\RELEASE_NOTES_v1.0.1.md'; Target = 'RELEASE_NOTES.md' }
    )) {
        $from = Join-Path $worktree $entry.Source
        if (!(Test-Path -LiteralPath $from)) {
            throw ('Missing release document: ' + $entry.Source)
        }
        $to = Join-Path $package $entry.Target
        Copy-Item -LiteralPath $from -Destination $to
        $included += $to
    }

    # Do not invent a licence: copy only one chosen by the project owner.
    $licence = Join-Path $worktree 'LICENSE'
    if (Test-Path -LiteralPath $licence) {
        $to = Join-Path $package 'LICENSE'
        Copy-Item -LiteralPath $licence -Destination $to
        $included += $to
    }

    $exeHash = (Get-FileHash -LiteralPath (
        Join-Path $package 'GhostClip.exe') -Algorithm SHA256).Hash
    $info = @(
        ('GhostClip: ' + $ReleaseLabel),
        'Scope: screenshot-only; Ctrl+C is not intercepted',
        ('Git commit: ' + $commit),
        'Windows x64; .NET 10 self-contained, single-file',
        ('GhostClip.exe SHA-256: ' + $exeHash),
        'No Git remote, tag or online release is created by this script.',
        'Manual Win+V, clipboard expiry and sign-in checks remain required.'
    ) -join [Environment]::NewLine
    $buildInfo = Join-Path $package 'BUILD_INFO.txt'
    [IO.File]::WriteAllText(
        $buildInfo, $info, [Text.UTF8Encoding]::new($false))
    $included += $buildInfo

    Compress-Archive -LiteralPath $included `
        -DestinationPath $zip -CompressionLevel Optimal
    if (!(Test-Path -LiteralPath $zip)) { throw 'Release ZIP missing.' }

    # Verify the exact executable bytes survive ZIP extraction before
    # writing the checksum used by a future GitHub release.
    $verify = Join-Path $env:TEMP (
        'GhostClip-ZipVerify-{0}' -f [guid]::NewGuid().ToString('N'))
    try {
        Expand-Archive -LiteralPath $zip -DestinationPath $verify
        $extracted = Join-Path $verify 'GhostClip.exe'
        $insideHash = (Get-FileHash -LiteralPath $extracted -Algorithm SHA256).Hash
        if ($insideHash -ne $exeHash) {
            throw 'ZIP contains an executable that does not match the build.'
        }
    }
    finally {
        if (Test-Path -LiteralPath $verify) {
            Remove-Item -LiteralPath $verify -Recurse -Force
        }
    }

    $zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
    $shaLine = '{0} *{1}' -f $zipHash, [IO.Path]::GetFileName($zip)
    [IO.File]::WriteAllText(
        $shaFile, $shaLine + [Environment]::NewLine, [Text.Encoding]::ASCII)

    Write-Host ('Source:     ' + $commit)
    Write-Host ('ZIP:        ' + $zip)
    Write-Host ('ZIP SHA256: ' + $zipHash)
    Write-Host ('EXE SHA256: ' + $exeHash)
}
finally {
    if ($added) {
        & git -C $repo worktree remove --force $worktree
        if ($LASTEXITCODE -ne 0) {
            Write-Warning ('Temporary worktree cleanup needs review: ' + $worktree)
        }
    }
}
