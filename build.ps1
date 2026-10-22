# Build and deploy script for EconomyOverhaul (Phase 1).
# Usage:  powershell -ExecutionPolicy Bypass -File build.ps1
# Steps:  backup existing DLL -> dotnet build -> copy DLL into the game Package.

param(
    [string]$ProjectDir = "D:\CarpetaDeJuan\Game Modding\Elin\EconomyOverhaul",
    [string]$GamePath = "C:\Program Files (x86)\Steam\steamapps\common\Elin"
)

$ErrorActionPreference = "Stop"
$src = Join-Path $ProjectDir "src"
$pkg = Join-Path $GamePath "Package\EconomyOverhaulMod"
$bak = Join-Path $ProjectDir "backup"

if (-not (Test-Path $src)) { throw "Source directory not found: $src" }

# 1. Backup the currently installed DLL (timestamped).
New-Item -ItemType Directory -Force -Path $bak | Out-Null
$existing = Join-Path $pkg "EconomyOverhaul.dll"
if (Test-Path $existing) {
    $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
    Copy-Item $existing (Join-Path $bak "EconomyOverhaul_$stamp.dll") -Force
    Write-Host "Backed up existing DLL to backup\EconomyOverhaul_$stamp.dll"
}

# 2. Build.
Push-Location $src
try {
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }

    # 3. Deploy.
    $dll = Join-Path $src "bin\Release\netstandard2.0\EconomyOverhaul.dll"
    if (-not (Test-Path $dll)) { throw "Build output not found: $dll" }
    New-Item -ItemType Directory -Force -Path $pkg | Out-Null
    Copy-Item $dll $pkg -Force
    Write-Host "Deployed EconomyOverhaul.dll -> $pkg"
} finally {
    Pop-Location
}
