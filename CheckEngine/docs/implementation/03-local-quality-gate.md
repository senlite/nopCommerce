# 03 Local Quality Gate Runner

Use this script to run the same quality-gate flow locally as CI.

## Script

- `CheckEngine/scripts/run-checkengine-quality.ps1`

## What it runs

1. Restore: `dotnet restore`
2. Build (Release by default): `dotnet build --no-restore`
3. Test with outputs:
   - TRX log: `checkengine-tests.trx`
   - Coverage: `XPlat Code Coverage` scoped to Domain + Application via `coverage.runsettings`
   - Results directory: `./TestResults`
   - Hard fail when Domain line coverage &lt; 80% (`NFR-058`) or Application &lt; 70% (`NFR-059`)
     (`CheckEngine/scripts/assert-checkengine-coverage.py`)
4. Summary generation:
   - Markdown summary: `./TestResults/checkengine-quality-summary.md`
   - Includes total/passed/failed/skipped, duration, line coverage, branch coverage
5. Trend history append:
   - CSV history: `CheckEngine/scripts/quality-history/checkengine-quality-history.csv`
   - Appends one row per run for trend reporting
6. Delta section:
   - Summary includes deltas vs previous run for duration, line coverage, and branch coverage

## Usage

From repository root:

- `powershell -ExecutionPolicy Bypass -File .\CheckEngine\scripts\run-checkengine-quality.ps1`
- `powershell -ExecutionPolicy Bypass -File .\CheckEngine\scripts\run-checkengine-quality.ps1 -Configuration Debug`

## Notes

- This matches `.github/workflows/checkengine-quality.yml`.
- Use this before pushing when you want CI-equivalent local validation.
