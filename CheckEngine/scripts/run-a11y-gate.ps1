# Accessibility rehearsal gate for Check Engine storefront surfaces (G6 / NFR-046).
# Scans Check Engine widgets only (.ce-root, [data-ce-theme]) and fails on serious/critical axe findings.
param(
    [string]$BaseUrl = $env:CHECKENGINE_A11Y_BASE_URL,
    [string]$OutputDir = $env:CHECKENGINE_A11Y_OUTPUT_DIR,
    [string]$WorkDir = $env:CHECKENGINE_A11Y_WORKDIR,
    [string]$ChromePath = $env:CHECKENGINE_A11Y_CHROME
)

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
$RootDir = Split-Path (Split-Path $ScriptDir -Parent) -Parent
Set-Location $RootDir

if (-not $BaseUrl) { $BaseUrl = $(if ($env:CHECKENGINE_CWV_BASE_URL) { $env:CHECKENGINE_CWV_BASE_URL } else { "http://127.0.0.1:5000" }) }
if (-not $OutputDir) { $OutputDir = Join-Path $env:TEMP "checkengine-a11y" }
if (-not $WorkDir) { $WorkDir = Join-Path $env:TEMP "checkengine-a11y-npm" }
if (-not $ChromePath -and $env:PLAYWRIGHT_BROWSER_PATH) { $ChromePath = $env:PLAYWRIGHT_BROWSER_PATH }

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error "[a11y-gate] node is required. Install Node.js 18+."
    exit 2
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Force -Path $WorkDir | Out-Null

if (-not (Test-Path (Join-Path $WorkDir "node_modules/axe-core")) -or -not (Test-Path (Join-Path $WorkDir "node_modules/playwright-core"))) {
    Write-Host "[a11y-gate] installing playwright-core and axe-core into $WorkDir"
    Push-Location $WorkDir
    npm init -y | Out-Null
    npm install --no-fund --no-audit playwright-core@1.55.0 axe-core@4.10.3 | Out-Null
    Pop-Location
}

$env:CHECKENGINE_A11Y_BASE_URL = $BaseUrl
$env:CHECKENGINE_A11Y_OUTPUT_DIR = $OutputDir
$env:CHECKENGINE_A11Y_WORKDIR = $WorkDir
if ($ChromePath) { $env:CHECKENGINE_A11Y_CHROME = $ChromePath }
if (-not $env:CHECKENGINE_A11Y_PAGES) {
    $env:CHECKENGINE_A11Y_PAGES = "/,/search?q=filter,/computers,/en/gmaster-bmw-parts-5,/build-your-own-computer,/en/gmaster-gm-11127548196-2,/ar/"
}

Write-Host "[a11y-gate] scanning $BaseUrl pages=$($env:CHECKENGINE_A11Y_PAGES)"
node (Join-Path $ScriptDir "a11y-gate.mjs")
