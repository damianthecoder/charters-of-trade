$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$outRoot = Join-Path $root "artifacts\godot-visual-qa"
New-Item -ItemType Directory -Force -Path $outRoot | Out-Null

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outDir = Join-Path $outRoot "visual-qa-$stamp"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$env:COT_VISUAL_QA_DIR = $outDir
try {
    $output = & (Join-Path $PSScriptRoot "godot.ps1") --path .\src\ChartersOfTrade.Godot --scene res://scenes/VisualQa.tscn --quit-after 180 2>&1
    $exitCode = $LASTEXITCODE
}
finally {
    Remove-Item Env:\COT_VISUAL_QA_DIR -ErrorAction SilentlyContinue
}

$text = $output -join [Environment]::NewLine
if ($text.Length -gt 0) { Write-Host $text }
if ($exitCode -ne 0) { exit $exitCode }
if (-not $text.Contains("VISUAL_QA PASS")) {
    Write-Host "Visual QA did not report VISUAL_QA PASS."
    exit 1
}

$frames = Get-ChildItem -Path $outDir -Filter "*.png" | Sort-Object Name
if ($frames.Count -ne 15) {
    Write-Host "Visual QA expected 15 PNG captures, got $($frames.Count)."
    exit 1
}

Write-Output "VISUAL_QA PASS $outDir $($frames.Count) captures"
