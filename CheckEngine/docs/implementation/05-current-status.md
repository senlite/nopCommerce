# 05 Current Implementation Status

Status date: 2026-08-25 · Plugin: `TwinParticles.CheckEngine` `0.104.0` on nopCommerce 4.90.6 / .NET 9

**Engineering status:** This page is the implementation snapshot. Progress, evidence gates
(G1–G6 done; G11 packing partial), and remaining blockers (H1.35/G8, G7, G11 vendor signing, G12)
are recorded in [EXECUTION-PLAN.md](../../EXECUTION-PLAN.md). Numbered specs in `docs/` remain the
product baseline; they are not v1.0 sign-off.

## Completed backlog slices

The enumerated engineering scaffold is complete (40/40), and Horizon 0 (`EP-01`) is complete. The
larger product vision remains pre-release: 1/28 epics closed, 19 partial and 8 pending. Horizon 1
epics `EP-02`–`EP-17` have in-tree implementations but stay open until the Horizon 1 gate. See
[EXECUTION-PLAN.md](../../EXECUTION-PLAN.md) and:

- [06 Operator Runbook](06-operator-runbook.md)
- [07 Acceptance Go/No-Go](07-acceptance-go-no-go.md)

### Highlights through plugin 0.104.0

- Host is nopCommerce 4.90.6 / .NET 9; Check Engine and GMaster use the 4.90 plugin, ACL and
  category APIs
- Package metadata, system/assembly identity and SemVer are aligned at
  `TwinParticles.CheckEngine` / `0.104.0` / nopCommerce 4.90 (`PluginMetadataContractTests`)
- Uninstall status exposes a destructive warning/export action; downloaded JSON includes complete
  vehicle/OEM/fitment claim provenance, and destructive lifecycle is blocked without a &lt;24h export
- Architecture suite: 1061 passing (excluding `VectorMathTests`); coverlet Domain ≥ 80% /
  Application ≥ 70%
- SQL Server 2022 install/update/uninstall rehearsal completed
- Populated database backup/restore rehearsed with byte-identical pre/post metrics
- Real catalog keyword/category search via nopCommerce; OEM/vehicle projections hydrate real products
- Search facets (category/brand/price/fitment), `/search/suggest` typeahead, and structured
  zero-result recovery
- Search/suggest/recommend rate limits key guests by customer id (`search:customer:{id}`), not
  shared NAT IP (`NFR-017`)
- Privacy-safe search analytics stores keyed query fingerprints and aggregate dimensions only
- Guest garage survives app restart and migrates inline to the authenticated SQL garage on sign-in
- Garage VINs are encrypted at the repository boundary; authenticated subject export, confirmed
  erasure and permanent-customer-delete cleanup are audited without copying VIN into audit JSON
- Ambiguous VIN garage adds return conflict + ranked candidates and never silently bind candidate 0
- BMW structural reference seed plus H1.6a top-10 catalog JSON (Toyota, Volkswagen, Honda, Hyundai,
  Ford, Mercedes-Benz, Nissan, Kia, Chevrolet). Domain stays brand-agnostic (`FR-204` / `INV-013`).
  `CatalogVinDecoder` is fail-closed: undocumented VDS returns `vin.decode_failed`. 224
  NHTSA-provenanced non-BMW VDS prefixes. OEM-complete VDS maps are not fabricated.
- Atomic make/model merge and soft archive preserve configuration/fitment ids
- Fitment corpus Must cases, qualifier variants and context-aware caching
- Public EN/AR vehicle and part-for-vehicle SEO landings with canonical/hreflang/JSON-LD
- SQL-backed fitment-gated URLs integrated into nopCommerce `/sitemap.xml`
- Storefront JS for sticky search, fitment band, garage widget
- Import pipeline is SQL-authoritative; publication is idempotent
- AI abstraction (Null + OpenAI-compatible), proposals never auto-publish
- ERPNext HTTP adapter with stub fallback when unconfigured
- Audit events, health endpoint, diagnostics admin
- Operator gates: `build-checkengine`, axe/CWV (`NFR-046` / `NFR-054`), search load (`NFR-017`),
  unsigned plugin pack (`pack-checkengine.sh`)

## Validation snapshot

- Do **not** build the full `src/NopCommerce.sln` for Check Engine work.
- Run: `bash CheckEngine/scripts/build-checkengine.sh`
- Architecture tests: 1061 pass as of plugin 0.104.0 (filter
  `FullyQualifiedName!~VectorMathTests`); GMaster suite: 19/19
- Coverlet: Domain 91.12% / Application 75.64% on the last recorded 0.104.0 run
- SQL Server lifecycle: install produced 25 `TP_CE_*` tables and 26 migration rows; uninstall
  left zero Check Engine tables, migration rows and permissions after a fresh &lt;24h export
- G4 live rehearsal (this class of host): 2,000 unique guest shoppers, 0 × 429, first-page
  p95 245 ms / p99 279 ms at 25 in-flight searches. A synchronized 2,000-POST herd exceeds this
  node's thread pool (not the 4-node Redis reference in `NFR-016`).
- G6 live axe: Check Engine widgets pass serious/critical. Search LCP 2.47–3.32 s is host
  logo/CSS — **do not** claim `NFR-002` 1.5 s is met.
- G11 unsigned pack: `TwinParticles.CheckEngine.0.104.0.zip` (~53 files, ~984 KB), SHA-256
  sidecar; vendor signing remains external.

## Known environmental constraints (not code gaps)

- The disposable SQL Server rehearsal uses Docker and is not part of the automatic dependency refresh.
- Check Engine health is intentionally `degraded` when no commercial licence is active; database,
  search-index and ERP probes can still all report `ok`.
- Two upstream checkout-model assertions can fail under full-suite parallel shared-state pollution;
  the complete 12-test fixture passes in isolation. They do not exercise Check Engine code.
- Horizon 1 is not gated: H1.35/G8 (independent security assessment), G7 (product-owner sign-off),
  G11 vendor signing, G12 Marketplace submission, live ERP partner reconciliation, and `NFR-002`
  search LCP remain open.
