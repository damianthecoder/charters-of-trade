$ErrorActionPreference = "Stop"

$godot = "C:\Users\damia\OneDrive\Pulpit\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe"
if (-not (Test-Path -LiteralPath $godot)) {
    throw "Godot .NET console executable was not found at: $godot"
}

$root = Split-Path -Parent $PSScriptRoot
$godotUserRoot = Join-Path $root ".godot_user"
$appData = Join-Path $godotUserRoot "AppData\Roaming"
$localAppData = Join-Path $godotUserRoot "AppData\Local"
New-Item -ItemType Directory -Force -Path $appData, $localAppData | Out-Null
$env:APPDATA = $appData
$env:LOCALAPPDATA = $localAppData

$previousErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = "Continue"
& $godot @args
$godotExitCode = $LASTEXITCODE
$ErrorActionPreference = $previousErrorActionPreference
$global:LASTEXITCODE = $godotExitCode
if ($godotExitCode -ne 0) {
    Write-Host "Godot exited with code $godotExitCode."
}
