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
- Check Engine package metadata, system/assembly identity and SemVer are aligned at
  `TwinParticles.CheckEngine` / `0.12.0` / nopCommerce 4.90
- Full solution builds; host regression passes 1,044 tests with 8 intentional skips
- SQL Server 2022 install/update/uninstall rehearsal completed
- Populated database backup/restore rehearsed with byte-identical pre/post metrics
- Real catalog keyword/category search via nopCommerce; OEM/vehicle projections hydrate real products
- Search facets (category/brand/price/fitment), `/search/suggest` typeahead, and structured zero-result recovery; verified live on SQL Server
- Search benchmark corpus with precision@10 and first-page latency budget gate CI (production-scale proof pending)
- Privacy-safe search analytics stores keyed query fingerprints and aggregate dimensions only, supports
  anonymous click-through/summary and retention pruning, and contains no VIN/query/customer/IP fields
- Live SQL search at 426 products / 294 vehicle leaves: keyword query p95 43.8ms and configuration
  autocomplete p95 11.1ms over 20 warm requests; ~250k-product load proof remains
- Guest garage survives app restart and migrates inline to the authenticated SQL garage on sign-in
- Garage VINs are encrypted at the repository boundary; authenticated subject export, confirmed
  erasure and permanent-customer-delete cleanup are audited without copying VIN into audit JSON
- Incremental BMW reference seed: 10 models, 32 generations, 284 current configuration definitions;
  live upgraded store has 294 unique preserved/current leaves and EN/AR aliases for every leaf
- Atomic make/model merge and soft archive preserve configuration/fitment ids, move aliases, invalidate
  alias caches, reject hard delete with descendants, and append customer-attributed audit events
- Fitment corpus: 233 Must cases; qualifier variants and context-aware caching verified live
- Public EN/AR vehicle and part-for-vehicle SEO landings with canonical/hreflang/JSON-LD
- SQL-backed fitment-gated URLs integrated into nopCommerce `/sitemap.xml`; thin pages are noindex
- Publishing/approving/rejecting a fitment claim regenerates the affected EN/AR landings so indexability updates immediately
- Storefront JS for sticky search, fitment band, garage widget
- Garage remove-vehicle + admin Dashboard Razor view
- Import pipeline is SQL-authoritative: source bytes, options, stage cursor/completions and full row
  state survive restart; all 12 stages are independently rerunnable and publication is idempotent
- AI abstraction (Null + OpenAI-compatible), proposals never auto-publish
- ERPNext HTTP adapter with stub fallback when unconfigured
- Fitment-constrained recommendations + NL search keyword fallback
- Audit events, health endpoint, diagnostics admin
- Perf/resilience/traceability/coverage gates + `build-checkengine` scripts in CI

## Validation snapshot

- `dotnet build src/NopCommerce.sln -c Release`
- `dotnet test src/Tests/Nop.Tests/Nop.Tests.csproj -c Release` (1,044 pass; 8 skip)
- Run: `bash CheckEngine/scripts/build-checkengine.sh`
- Check Engine suite: 552/552; GMaster suite: 19/19
- SQL Server lifecycle: install produced 25 `TP_CE_*` tables and 26 migration rows; uninstall left zero
  Check Engine tables, migration rows and permissions; backup restore returned all baseline counts

## Known environmental constraints (not code gaps)

- The disposable SQL Server rehearsal uses Docker and is not part of the automatic dependency refresh.
- Check Engine health is intentionally `degraded` when no commercial licence is active; database,
  search-index and ERP probes can still all report `ok`.
- Two upstream checkout-model assertions can fail under full-suite parallel shared-state pollution;
  the complete 12-test fixture passes in isolation. They do not exercise Check Engine code.
