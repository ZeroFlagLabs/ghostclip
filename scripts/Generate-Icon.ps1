param(
    [string]$OutputPath = 'Assets\GhostClip.ico'
)

# Regenerate the Windows EXE icon from the exact WPF DrawingImage used by
# GhostClip itself. No separate hand-drawn/approximated artwork is maintained.
#
# Run with Windows PowerShell 5.1 in STA mode:
#   powershell -NoProfile -STA -File .\scripts\Generate-Icon.ps1
#
# Windows 11 supports PNG-compressed ICO frames. Each PNG is rendered from
# the same vector resource at its actual target resolution for crisp scaling.

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$appXaml = Join-Path $repo 'App.xaml'

if (!(Test-Path -LiteralPath $appXaml)) {
    throw 'App.xaml not found.'
}

Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$source = [IO.File]::ReadAllText($appXaml)
$match = [regex]::Match(
    $source,
    '(?s)<DrawingImage\s+x:Key="GhostClipWindowIcon">(?<drawing>.*?)</DrawingImage>')

if (!$match.Success) {
    throw 'GhostClipWindowIcon DrawingImage resource was not found.'
}

$markup = '<DrawingImage xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">' +
    $match.Groups['drawing'].Value + '</DrawingImage>'

$vector = [System.Windows.Markup.XamlReader]::Parse($markup)

if ($vector -isnot [System.Windows.Media.DrawingImage]) {
    throw 'GhostClipWindowIcon resource did not parse as a WPF DrawingImage.'
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$frames = @()

foreach ($size in $sizes) {
    $visual = [System.Windows.Media.DrawingVisual]::new()
    $context = $visual.RenderOpen()

    try {
        $rectangle = [System.Windows.Rect]::new(0, 0, $size, $size)
        $context.DrawImage($vector, $rectangle)
    }
    finally {
        $context.Close()
    }

    $bitmap = [System.Windows.Media.Imaging.RenderTargetBitmap]::new(
        $size, $size, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)

    $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add(
        [System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $pngStream = [IO.MemoryStream]::new()

    try {
        $encoder.Save($pngStream)
        $frames += [pscustomobject]@{
            Size = $size
            Bytes = $pngStream.ToArray()
        }
    }
    finally {
        $pngStream.Dispose()
    }
}

if ([IO.Path]::IsPathRooted($OutputPath)) {
    $destination = $OutputPath
}
else {
    $destination = Join-Path $repo $OutputPath
}

$directory = Split-Path -Parent $destination
if (!(Test-Path -LiteralPath $directory)) {
    [void](New-Item -ItemType Directory -Path $directory -Force)
}

$buffer = [IO.MemoryStream]::new()
$writer = [IO.BinaryWriter]::new($buffer)

try {
    # ICO header: reserved (0), image (1), number of images.
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$frames.Count)

    $offset = 6 + 16 * $frames.Count

    foreach ($frame in $frames) {
        $dimension = if ($frame.Size -eq 256) {
            [byte]0
        }
        else {
            [byte]$frame.Size
        }

        $writer.Write($dimension)
        $writer.Write($dimension)
        $writer.Write([byte]0)     # palette colours
        $writer.Write([byte]0)     # reserved
        $writer.Write([uint16]1)  # planes
        $writer.Write([uint16]32) # bits per pixel
        $writer.Write([uint32]$frame.Bytes.Length)
        $writer.Write([uint32]$offset)

        $offset += $frame.Bytes.Length
    }

    foreach ($frame in $frames) {
        $writer.Write([byte[]]$frame.Bytes)
    }

    $writer.Flush()
    [IO.File]::WriteAllBytes($destination, $buffer.ToArray())
}
finally {
    $writer.Dispose()
    $buffer.Dispose()
}

Write-Host ('Source resource: App.xaml / GhostClipWindowIcon')
Write-Host ('Windows icon:    ' + $destination)
Write-Host ('ICO frames:      ' + ($sizes -join ', '))
Write-Host ('ICO SHA-256:     ' +
    (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash)
