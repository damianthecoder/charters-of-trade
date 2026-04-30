$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$env:DOTNET_CLI_HOME = Join-Path $root ".dotnet_home"
$env:NUGET_PACKAGES = Join-Path $root ".nuget_packages"
$env:NUGET_HTTP_CACHE_PATH = Join-Path $root ".nuget_http_cache"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

dotnet restore ChartersOfTrade.sln -p:RestoreBuildInParallel=false -m:1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build ChartersOfTrade.sln --no-restore -m:1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet run --project tests/ChartersOfTrade.Tests/ChartersOfTrade.Tests.csproj --no-build --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$godotSmokeOutput = & (Join-Path $PSScriptRoot "godot.ps1") --headless --path .\src\ChartersOfTrade.Godot --scene res://scenes/InteractionSmoke.tscn --quit-after 240 2>&1
$godotSmokeExitCode = $LASTEXITCODE
$godotSmokeText = $godotSmokeOutput -join [Environment]::NewLine
if ($godotSmokeText.Length -gt 0) { Write-Host $godotSmokeText }
if ($godotSmokeExitCode -ne 0) { exit $godotSmokeExitCode }
if (-not $godotSmokeText.Contains("INTERACTION_SMOKE PASS")) {
    Write-Host "Godot interaction smoke did not report INTERACTION_SMOKE PASS."
    exit 1
}

if ($env:COT_SKIP_VISUAL_SMOKE -ne "1") {
    $visualSmokeOutput = & (Join-Path $PSScriptRoot "visual-smoke.ps1") 2>&1
    $visualSmokeExitCode = $LASTEXITCODE
    $visualSmokeText = $visualSmokeOutput -join [Environment]::NewLine
    if ($visualSmokeText.Length -gt 0) { Write-Host $visualSmokeText }
    if ($visualSmokeExitCode -ne 0) { exit $visualSmokeExitCode }
    if (-not $visualSmokeText.Contains("VISUAL_SMOKE PASS")) {
        Write-Host "Godot visual smoke did not report VISUAL_SMOKE PASS."
        exit 1
    }
}
else {
    Write-Host "Skipping visual smoke because COT_SKIP_VISUAL_SMOKE=1."
}
