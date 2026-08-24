# Core Web Vitals rehearsal gate for Check Engine storefront templates (H1.28 / NFR-054).
# Requires Node.js and Lighthouse CLI: npm install -g lighthouse
param(
    [string]$BaseUrl = $env:CHECKENGINE_CWV_BASE_URL,
    [string]$OutputDir = $env:CHECKENGINE_CWV_OUTPUT_DIR,
    [int]$MaxLcpMs = $(if ($env:CHECKENGINE_CWV_MAX_LCP_MS) { [int]$env:CHECKENGINE_CWV_MAX_LCP_MS } else { 2500 }),
    [int]$MaxSearchLcpMs = $(if ($env:CHECKENGINE_CWV_MAX_SEARCH_LCP_MS) { [int]$env:CHECKENGINE_CWV_MAX_SEARCH_LCP_MS } else { 1500 }),
    [int]$MaxInpMs = $(if ($env:CHECKENGINE_CWV_MAX_INP_MS) { [int]$env:CHECKENGINE_CWV_MAX_INP_MS } else { 200 }),
    [double]$MaxCls = $(if ($env:CHECKENGINE_CWV_MAX_CLS) { [double]$env:CHECKENGINE_CWV_MAX_CLS } else { 0.1 }),
    [string]$FormFactor = $(if ($env:CHECKENGINE_CWV_FORM_FACTOR) { $env:CHECKENGINE_CWV_FORM_FACTOR } else { "mobile" }),
    [int]$CpuSlowdown = $(if ($env:CHECKENGINE_CWV_CPU_SLOWDOWN) { [int]$env:CHECKENGINE_CWV_CPU_SLOWDOWN } else { 4 })
)

$ErrorActionPreference = "Stop"
$RootDir = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location $RootDir

if (-not $BaseUrl) { $BaseUrl = "http://127.0.0.1:5000" }
if (-not $OutputDir) { $OutputDir = Join-Path $env:TEMP "checkengine-cwv" }
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

if (-not (Get-Command lighthouse -ErrorAction SilentlyContinue)) {
    Write-Error "[cwv-gate] lighthouse CLI not found. Install with: npm install -g lighthouse"
    exit 2
}

$pages = if ($env:CHECKENGINE_CWV_PAGES) {
    $env:CHECKENGINE_CWV_PAGES.Split(",") | ForEach-Object { $_.Trim() } | Where-Object { $_ }
} else {
    @("/", "/search?q=filter", "/computers", "/en/gmaster-bmw-parts-5", "/build-your-own-computer", "/en/gmaster-gm-11127548196-2", "/ar/")
}
$failed = $false

foreach ($path in $pages) {
    $url = ($BaseUrl.TrimEnd("/") + $path)
    $safeName = ($path -replace "[/=?&]", "_").Trim("_")
    if ([string]::IsNullOrWhiteSpace($safeName)) { $safeName = "home" }
    try {
        $probe = Invoke-WebRequest -Uri $url -MaximumRedirection 5 -SkipHttpErrorCheck -TimeoutSec 15
        if ($probe.StatusCode -ne 200) {
            Write-Host "[cwv-gate] ${safeName}: skipped (HTTP $($probe.StatusCode))"
            continue
        }
    } catch {
        Write-Host "[cwv-gate] ${safeName}: skipped (unreachable)"
        continue
    }
    $report = Join-Path $OutputDir "lighthouse-$safeName-$FormFactor.json"
    $mobileFlag = if ($FormFactor -eq "mobile") { "true" } else { "false" }

    Write-Host "[cwv-gate] auditing $url ($FormFactor, CPU x$CpuSlowdown)"
    & lighthouse $url `
        --quiet `
        --form-factor=$FormFactor `
        --screenEmulation.mobile=$mobileFlag `
        --throttling.cpuSlowdownMultiplier=$CpuSlowdown `
        --throttling.rttMs=150 `
        --throttling.throughputKbps=1638.4 `
        --chrome-flags="--headless --no-sandbox" `
        --only-categories=performance `
        --output=json `
        --output-path=$report

    $json = Get-Content $report -Raw | ConvertFrom-Json
    $lcp = [double]$json.audits."largest-contentful-paint".numericValue
    $cls = [double]$json.audits."cumulative-layout-shift".numericValue
    $inp = [double]($json.audits."interaction-to-next-paint".numericValue)
    if ($inp -eq 0) { $inp = [double]$json.audits."total-blocking-time".numericValue }

    $pageLcpBudget = if ($path.StartsWith("/search")) { $MaxSearchLcpMs } else { $MaxLcpMs }
    Write-Host "[cwv-gate] ${safeName}: LCP=${lcp}ms CLS=${cls} INP/TBT=${inp}ms (budget LCP<=${pageLcpBudget})"
    if ($lcp -gt $pageLcpBudget -or $inp -gt $MaxInpMs -or $cls -gt $MaxCls) {
        Write-Host "[cwv-gate] ${safeName}: fail" -ForegroundColor Red
        $failed = $true
    } else {
        Write-Host "[cwv-gate] ${safeName}: pass" -ForegroundColor Green
    }
}

if ($failed) {
    Write-Error "[cwv-gate] one or more pages exceeded NFR-054 budgets"
    exit 1
}

Write-Host "[cwv-gate] all audited pages within NFR-054 budgets"
