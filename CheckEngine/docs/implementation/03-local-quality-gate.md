# 03 Local Quality Gate Runner

**Engineering status (2026-08-25):** Plugin `0.104.0` is in tree. Progress and remaining blockers
are recorded in [EXECUTION-PLAN.md](../../EXECUTION-PLAN.md).

Use these scripts to run the same quality-gate flow locally as CI, plus the operator rehearsal
gates that sit beside it.

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

## Companion operator scripts

These are not substitutes for the coverage gate. They are the local rehearsals for later
Horizon 1 evidence gates:

| Script | Gate | Notes |
|---|---|---|
| `CheckEngine/scripts/build-checkengine.sh` (or `.ps1`) | Everyday plugin + architecture tests | Default local command; skips `VectorMathTests` |
| `CheckEngine/scripts/run-a11y-gate.sh` (or `.ps1`) | G6 / `NFR-046` | Playwright axe on Check Engine widgets |
| `CheckEngine/scripts/run-cwv-gate.sh` (or `.ps1`) | G6 / `NFR-054` | Does **not** claim `NFR-002` 1.5 s search LCP is met |
| `CheckEngine/scripts/run-search-load-gate.sh` (or `.ps1`) | G4 / `NFR-017` | Browser User-Agent required; default 25 in-flight |
| `CheckEngine/scripts/pack-checkengine.sh` (or `.ps1`) | G11 packing (unsigned) | Zip + SHA-256; vendor signing remains external |

## Notes

- The PowerShell quality runner matches `.github/workflows/checkengine-quality.yml`.
- Use this before pushing when you want CI-equivalent local validation.
