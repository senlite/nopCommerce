# 05 Current Implementation Status

Status date: 2026-08-13

## Completed backlog slices

The enumerated engineering scaffold is complete (40/40), and Horizon 0 (`EP-01`) is complete. The
larger product vision remains pre-release: 1/28 epics closed, 19 partial and 8 pending. See
[EXECUTION-PLAN.md](../../EXECUTION-PLAN.md) and:

- [06 Operator Runbook](06-operator-runbook.md)
- [07 Acceptance Go/No-Go](07-acceptance-go-no-go.md)

### Highlights since 2026-08-03

- Complete host upgraded to nopCommerce 4.90.6 / .NET 9
- Check Engine and GMaster ported to 4.90 plugin, ACL and category APIs
- Full solution builds; host regression passes 1,044 tests with 8 intentional skips
- SQL Server 2022 install/update/uninstall rehearsal completed
- Populated database backup/restore rehearsed with byte-identical pre/post metrics
- Real catalog keyword/category search via nopCommerce; OEM/vehicle projections hydrate real products
- Guest garage survives app restart and migrates inline to the authenticated SQL garage on sign-in
- Fitment corpus: 233 Must cases; qualifier variants and context-aware caching verified live
- Storefront JS for sticky search, fitment band, garage widget
- Garage remove-vehicle + admin Dashboard Razor view
- Import pipeline persists to SQL (`CorrelationId`), Excel/PDF parsers, `NopImportProductPublisher`
- AI abstraction (Null + OpenAI-compatible), proposals never auto-publish
- ERPNext HTTP adapter with stub fallback when unconfigured
- Fitment-constrained recommendations + NL search keyword fallback
- Audit events, health endpoint, diagnostics admin
- Perf/resilience/traceability/coverage gates + `build-checkengine` scripts in CI

## Validation snapshot

- `dotnet build src/NopCommerce.sln -c Release`
- `dotnet test src/Tests/Nop.Tests/Nop.Tests.csproj -c Release` (1,044 pass; 8 skip)
- Run: `bash CheckEngine/scripts/build-checkengine.sh`
- Check Engine suite: 485/485; GMaster suite: 19/19
- SQL Server lifecycle: install produced 25 `TP_CE_*` tables and 26 migration rows; uninstall left zero
  Check Engine tables, migration rows and permissions; backup restore returned all baseline counts

## Known environmental constraints (not code gaps)

- The disposable SQL Server rehearsal uses Docker and is not part of the automatic dependency refresh.
- Check Engine health is intentionally `degraded` when no commercial licence is active; database,
  search-index and ERP probes can still all report `ok`.
- An upstream checkout-model test failed once under full-suite parallel execution, then passed in
  isolation and on the complete rerun; treat recurrence as a flake unless reproducible.
