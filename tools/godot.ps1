$ErrorActionPreference = "Stop"

$godot = "C:\Users\damia\OneDrive\Pulpit\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe"
if (-not (Test-Path -LiteralPath $godot)) {
    throw "Godot .NET console executable was not found at: $godot"
}

& $godot @args

