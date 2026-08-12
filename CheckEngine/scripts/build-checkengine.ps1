# Deterministic local build + architecture test gate for TwinParticles.CheckEngine.
# Do NOT use NopCommerce.sln — it is broken for this plugin workflow.
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$RootDir = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $RootDir

$PluginCsproj = "src/Plugins/TwinParticles.CheckEngine/TwinParticles.CheckEngine.csproj"
$ArchCsproj = "src/Tests/TwinParticles.CheckEngine.Tests.Architecture/TwinParticles.CheckEngine.Tests.Architecture.csproj"
$E2eCsproj = "src/Tests/TwinParticles.CheckEngine.Tests.E2E/TwinParticles.CheckEngine.Tests.E2E.csproj"

Write-Host "[checkengine-build] step 1/4: restore + build plugin"
dotnet restore $PluginCsproj
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build $PluginCsproj -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[checkengine-build] step 2/4: restore + build architecture tests"
dotnet restore $ArchCsproj
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build $ArchCsproj -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (Test-Path $E2eCsproj) {
    Write-Host "[checkengine-build] step 3/4: restore + build E2E tests"
    dotnet restore $E2eCsproj
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet build $E2eCsproj -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
else {
    Write-Host "[checkengine-build] step 3/4: E2E project not found — skipped"
}

Write-Host "[checkengine-build] step 4/4: run architecture tests ($Configuration)"
dotnet test $ArchCsproj -c $Configuration --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[checkengine-build] done"
