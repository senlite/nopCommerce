# Search concurrency rehearsal for G4 / NFR-017 (2,000 shoppers, first-page p95 ≤ 300 ms).
param(
    [string]$BaseUrl = $env:CHECKENGINE_LOAD_BASE_URL,
    [int]$Vus = $(if ($env:CHECKENGINE_LOAD_VUS) { [int]$env:CHECKENGINE_LOAD_VUS } else { 2000 }),
    [int]$Concurrency = $(if ($env:CHECKENGINE_LOAD_CONCURRENCY) { [int]$env:CHECKENGINE_LOAD_CONCURRENCY } else { 25 })
)

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
$RootDir = Split-Path (Split-Path $ScriptDir -Parent) -Parent
Set-Location $RootDir

if (-not $BaseUrl) { $BaseUrl = "http://127.0.0.1:5000" }
$env:CHECKENGINE_LOAD_BASE_URL = $BaseUrl
$env:CHECKENGINE_LOAD_VUS = "$Vus"
$env:CHECKENGINE_LOAD_CONCURRENCY = "$Concurrency"
if (-not $env:CHECKENGINE_LOAD_P95_MS) { $env:CHECKENGINE_LOAD_P95_MS = "300" }
if (-not $env:CHECKENGINE_LOAD_OUTPUT) { $env:CHECKENGINE_LOAD_OUTPUT = (Join-Path $env:TEMP "checkengine-nfr017.json") }

Write-Host "[nfr017-gate] target=$BaseUrl vus=$Vus concurrency=$Concurrency"

if (Get-Command k6 -ErrorAction SilentlyContinue) {
    Write-Host "[nfr017-gate] using k6"
    k6 run (Join-Path $RootDir "CheckEngine/tests/perf/search-nfr017.js")
    exit $LASTEXITCODE
}

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error "[nfr017-gate] neither k6 nor node is available."
    exit 2
}

Write-Host "[nfr017-gate] k6 not found; running Node 2,000-session sample"
node (Join-Path $ScriptDir "search-nfr017-sample.mjs")
