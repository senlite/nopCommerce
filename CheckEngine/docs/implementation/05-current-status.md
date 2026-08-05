# 05 Current Implementation Status

Status date: 2026-08-05

## Completed backlog slices

All EXECUTION-PLAN tracks **T0–T7** are implemented (52/52). See [EXECUTION-PLAN.md](../../EXECUTION-PLAN.md) and:

- [06 Operator Runbook](06-operator-runbook.md)
- [07 Acceptance Go/No-Go](07-acceptance-go-no-go.md)

### Highlights since 2026-08-03

- SQL-backed product search (`SqlProductSearchReadRepository`) wired in DI
- Storefront JS for sticky search, fitment band, garage widget
- Garage remove-vehicle + admin Dashboard Razor view
- Import pipeline persists to SQL (`CorrelationId`), Excel/PDF parsers, `NopImportProductPublisher`
- AI abstraction (Null + OpenAI-compatible), proposals never auto-publish
- ERPNext HTTP adapter with stub fallback when unconfigured
- Fitment-constrained recommendations + NL search keyword fallback
- Audit events, health endpoint, diagnostics admin
- Perf/resilience/traceability/coverage gates + `build-checkengine` scripts in CI

## Validation snapshot

- Run: `bash CheckEngine/scripts/build-checkengine.sh`
- Architecture suite is the quality gate for Check Engine (see GitHub Actions `checkengine-quality`)

## Known environmental constraints (not code gaps)

- Full `src/NopCommerce.sln` restore fails on stale `Nop.Plugin.Misc.Sendinblue` project reference — build plugin/test projects directly.
- PostgreSQL install path broken with pinned `linq2db 4.3.0` + `Npgsql 8.0.4` (`NpgsqlProviderAdapter` constructor reflection). Use MySQL or SQL Server for host install in constrained environments.
- Live SQL Server migration dry-run requires an external SQL Server clone (cloud agent default is MySQL for host).
