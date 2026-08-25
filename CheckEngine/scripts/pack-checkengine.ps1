# Pack TwinParticles.CheckEngine.{version}.zip (G11 commercial packaging rehearsal).
param(
    [string]$OutDir = $(if ($env:CHECKENGINE_PACK_OUT) { $env:CHECKENGINE_PACK_OUT } else { (Join-Path $env:TEMP "checkengine-pack") }),
    [string]$Configuration = $(if ($env:CONFIGURATION) { $env:CONFIGURATION } else { "Release" }),
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
$RootDir = Split-Path (Split-Path $ScriptDir -Parent) -Parent
Set-Location $RootDir

$env:CHECKENGINE_PACK_OUT = $OutDir
$argsList = @((Join-Path $ScriptDir "pack-checkengine.py"), "--configuration", $Configuration, "--out-dir", $OutDir)
if (-not $NoBuild) { $argsList += "--build" }

Write-Host "[pack-gate] out=$OutDir configuration=$Configuration"
python3 @argsList
