param(
    [string]$ExecutablePath = 'bin\Release\net10.0-windows\GhostClip.exe'
)

# Verify that Windows extracts the same 32x32 ghost from GhostClip.exe as the
# generated ICO's WPF-rendered 32x32 PNG frame. Windows Shell icon extraction
# is the important test; some managed ICO decoders mishandle PNG frames.

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$sourceIcon = Join-Path $repo 'Assets\GhostClip.ico'

$exe = if ([IO.Path]::IsPathRooted($ExecutablePath)) {
    $ExecutablePath
}
else {
    Join-Path $repo $ExecutablePath
}

if (!(Test-Path -LiteralPath $exe) -or
    !(Test-Path -LiteralPath $sourceIcon)) {
    throw 'Executable or source GhostClip.ico is missing.'
}

Add-Type -AssemblyName System.Drawing

$bytes = [IO.File]::ReadAllBytes($sourceIcon)
if ($bytes.Length -lt 22 -or
    [BitConverter]::ToUInt16($bytes, 0) -ne 0 -or
    [BitConverter]::ToUInt16($bytes, 2) -ne 1) {
    throw 'GhostClip.ico has an invalid icon-directory header.'
}

$count = [BitConverter]::ToUInt16($bytes, 4)
$frame32 = $null
$foundSizes = @()

for ($i = 0; $i -lt $count; $i++) {
    $entry = 6 + 16 * $i

    if ($entry + 16 -gt $bytes.Length) {
        throw 'GhostClip.ico has a truncated icon directory.'
    }

    $size = if ($bytes[$entry] -eq 0) {
        256
    }
    else {
        [int]$bytes[$entry]
    }
    $foundSizes += $size

    if ($size -ne 32) {
        continue
    }

    $length = [BitConverter]::ToUInt32($bytes, $entry + 8)
    $offset = [BitConverter]::ToUInt32($bytes, $entry + 12)

    if ($offset + $length -gt $bytes.Length) {
        throw 'The 32x32 icon frame exceeds the file length.'
    }

    $frame32 = [byte[]]::new($length)
    [Array]::Copy($bytes, $offset, $frame32, 0, $length)
}

$expectedSizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)

if (($foundSizes -join ',') -ne ($expectedSizes -join ',')) {
    throw 'ICO resolution set is incomplete or out of order.'
}

if ($null -eq $frame32) {
    throw 'The 32x32 source frame was not found.'
}

$stream = [IO.MemoryStream]::new($frame32)
$expected = $null
$embeddedIcon = $null
$actual = $null

try {
    $expected = [Drawing.Bitmap]::new($stream)
    $embeddedIcon = [Drawing.Icon]::ExtractAssociatedIcon($exe)

    if ($null -eq $embeddedIcon) {
        throw 'Windows could not extract the executable icon.'
    }

    $actual = $embeddedIcon.ToBitmap()

    if ($expected.Width -ne 32 -or $expected.Height -ne 32 -or
        $actual.Width -ne 32 -or $actual.Height -ne 32) {
        throw 'Expected 32x32 images for comparison.'
    }

    for ($y = 0; $y -lt 32; $y++) {
        for ($x = 0; $x -lt 32; $x++) {
            if ($expected.GetPixel($x, $y).ToArgb() -ne
                $actual.GetPixel($x, $y).ToArgb()) {
                throw ('Embedded executable icon mismatch at ({0},{1}).' -f
                    $x, $y)
            }
        }
    }

    Write-Host 'GhostClip EXE icon pixel-for-pixel verification: PASS'
    Write-Host ('ICO frames: ' + ($foundSizes -join ', '))
    Write-Host ('Embedded icon: ' + $exe)
}
finally {
    if ($null -ne $actual) {
        $actual.Dispose()
    }
    if ($null -ne $embeddedIcon) {
        $embeddedIcon.Dispose()
    }
    if ($null -ne $expected) {
        $expected.Dispose()
    }
    $stream.Dispose()
}
