$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$outDir = Join-Path $root "artifacts\godot-smoke"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$moviePath = Join-Path $outDir "visual-smoke-$stamp.png"

& (Join-Path $PSScriptRoot "godot.ps1") --path .\src\ChartersOfTrade.Godot --write-movie $moviePath --fixed-fps 12 --quit-after 3
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$stem = [System.IO.Path]::GetFileNameWithoutExtension($moviePath)
$frame = Get-ChildItem -Path $outDir -Filter "$stem*.png" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $frame) {
    Write-Output "Visual smoke did not produce a PNG frame."
    exit 1
}

Add-Type -AssemblyName System.Drawing
$image = [System.Drawing.Bitmap]::FromFile($frame.FullName)
$width = $image.Width
$height = $image.Height
try {
    if ($width -ne 1920 -or $height -ne 1080) {
        Write-Output "Visual smoke expected 1920x1080, got ${width}x${height}."
        exit 1
    }

    $colors = New-Object 'System.Collections.Generic.HashSet[int]'
    $lit = 0
    for ($y = 0; $y -lt $image.Height; $y += 90) {
        for ($x = 0; $x -lt $image.Width; $x += 120) {
            $pixel = $image.GetPixel($x, $y)
            [void]$colors.Add($pixel.ToArgb())
            if ($pixel.R -gt 36 -or $pixel.G -gt 36 -or $pixel.B -gt 36) {
                $lit++
            }
        }
    }

    if ($colors.Count -lt 8 -or $lit -lt 12) {
        Write-Output "Visual smoke frame looked blank or too flat: colors=$($colors.Count), lit=$lit."
        exit 1
    }
}
finally {
    $image.Dispose()
}

Write-Output "VISUAL_SMOKE PASS $($frame.FullName) ${width}x${height}"
