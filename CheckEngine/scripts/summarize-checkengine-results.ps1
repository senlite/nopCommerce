param(
    [string]$ResultsDir = "TestResults",
    [string]$TrxName = "checkengine-tests.trx"
)

$ErrorActionPreference = "Stop"

$trxFile = Get-ChildItem -Path $ResultsDir -Recurse -File -Filter $TrxName | Select-Object -First 1
if (-not $trxFile) {
    throw "TRX file '$TrxName' not found under '$ResultsDir'."
}

[xml]$trxXml = Get-Content -Path $trxFile.FullName
$counters = $trxXml.TestRun.ResultSummary.Counters
$times = $trxXml.TestRun.Times

$total = [int]$counters.total
$passed = [int]$counters.passed
$failed = [int]$counters.failed
$skipped = [int]$counters.notExecuted

$duration = "n/a"
if ($times -and $times.start -and $times.finish) {
    $start = [datetimeoffset]::Parse($times.start)
    $finish = [datetimeoffset]::Parse($times.finish)
    $duration = [math]::Round(($finish - $start).TotalSeconds, 2)
}

$coverageFile = Get-ChildItem -Path $ResultsDir -Recurse -File -Filter "coverage.cobertura.xml" | Select-Object -First 1
$lineCoverage = "n/a"
$branchCoverage = "n/a"
if ($coverageFile) {
    [xml]$coverageXml = Get-Content -Path $coverageFile.FullName
    $lineRate = [double]$coverageXml.coverage.'line-rate'
    $branchRate = [double]$coverageXml.coverage.'branch-rate'
    $lineCoverage = "{0:N2}%" -f ($lineRate * 100)
    $branchCoverage = "{0:N2}%" -f ($branchRate * 100)
}

$historyDir = "CheckEngine/scripts/quality-history"
$historyPath = Join-Path $historyDir "checkengine-quality-history.csv"
if (-not (Test-Path $historyDir)) {
    New-Item -ItemType Directory -Path $historyDir | Out-Null
}

$lineCoverageNumeric = if ($lineCoverage -eq "n/a") { "" } else { $lineCoverage.TrimEnd('%') }
$branchCoverageNumeric = if ($branchCoverage -eq "n/a") { "" } else { $branchCoverage.TrimEnd('%') }
$durationNumeric = if ($duration -eq "n/a") { "" } else { $duration }

$previousRecord = $null
if (Test-Path $historyPath) {
    $previousRecord = Import-Csv -Path $historyPath | Select-Object -Last 1
}

$record = [PSCustomObject]@{
    timestamp_utc = (Get-Date).ToUniversalTime().ToString("o")
    total = $total
    passed = $passed
    failed = $failed
    skipped = $skipped
    duration_seconds = $durationNumeric
    line_coverage_percent = $lineCoverageNumeric
    branch_coverage_percent = $branchCoverageNumeric
}

$durationDelta = "n/a"
$lineCoverageDelta = "n/a"
$branchCoverageDelta = "n/a"

if ($previousRecord) {
    if ($previousRecord.duration_seconds -and $durationNumeric) {
        $durationDeltaValue = [math]::Round(([double]$durationNumeric - [double]$previousRecord.duration_seconds), 2)
        $durationDelta = "{0:+0.00;-0.00;0.00}" -f $durationDeltaValue
    }

    if ($previousRecord.line_coverage_percent -and $lineCoverageNumeric) {
        $lineDeltaValue = [math]::Round(([double]$lineCoverageNumeric - [double]$previousRecord.line_coverage_percent), 2)
        $lineCoverageDelta = "{0:+0.00;-0.00;0.00}" -f $lineDeltaValue
    }

    if ($previousRecord.branch_coverage_percent -and $branchCoverageNumeric) {
        $branchDeltaValue = [math]::Round(([double]$branchCoverageNumeric - [double]$previousRecord.branch_coverage_percent), 2)
        $branchCoverageDelta = "{0:+0.00;-0.00;0.00}" -f $branchDeltaValue
    }
}

if (-not (Test-Path $historyPath)) {
    $record | Export-Csv -Path $historyPath -NoTypeInformation
} else {
    $record | Export-Csv -Path $historyPath -NoTypeInformation -Append
}

$summary = @(
    "# CheckEngine Quality Summary",
    "",
    "- Total: $total",
    "- Passed: $passed",
    "- Failed: $failed",
    "- Skipped: $skipped",
    "- Duration (seconds): $duration",
    "- Line coverage: $lineCoverage",
    "- Branch coverage: $branchCoverage",
    "",
    "## Delta vs Previous Run",
    "- Duration delta (seconds): $durationDelta",
    "- Line coverage delta (pp): $lineCoverageDelta",
    "- Branch coverage delta (pp): $branchCoverageDelta"
)

$summaryPath = Join-Path $ResultsDir "checkengine-quality-summary.md"
$summary | Set-Content -Path $summaryPath -Encoding UTF8

if ($env:GITHUB_STEP_SUMMARY) {
    $summary | Add-Content -Path $env:GITHUB_STEP_SUMMARY
}

Write-Host "[checkengine-quality] summary written: $summaryPath"
Write-Host "[checkengine-quality] history updated: $historyPath"
