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
