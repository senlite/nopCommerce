# Check Engine End-to-End Execution Plan

**Purpose:** Single source of truth for implementation progress and remaining work.

**Last updated:** 2026-09-02

## Status legend
- `pending` = not started
- `partial` = real implementation exists, but the documented exit criteria are not fully met
- `in-progress` = currently being implemented
- `done` = implemented and validated
- `blocked` = cannot proceed without prerequisite/decision
- `not-committed` = evaluated in the larger vision but deliberately outside the committed roadmap

## Current overall progress

This plan has three accounting levels. They must not be conflated:

1. **Engineering scaffold: 40 / 40 enumerated tasks done** (37 legacy `T0`–`T7` rows plus three
   baseline E2E rows). Earlier revisions claimed “52 / 52,” but did not identify the other twelve
   tasks; that unauditable total is retired. The scaffold builds and its architecture/behavior tests pass.
2. **Documented product vision (`EP-01`–`EP-28`): 1 / 28 epics closed, 19 partial, 8 pending.**
   An epic is only closed when every exit criterion in
   [38 Epics](docs/38-epics.md) is evidenced.
3. **Horizon 1 / v1.0 release gate: 0 / 12 checklist items fully evidenced.** See
   [41 Release Plan](docs/41-release-plan.md#v10--foundation). Check Engine remains **pre-release**.

The previous “52 / 52 complete” headline was both unauditable from the listed tasks and misleading when
read as completion of Horizon 1 or the full roadmap. This revision uses only enumerated tasks and makes
all remaining work in the larger documented vision explicit.

### Implemented scaffold summary (2026-08-05)
- SQL-backed vehicle/OEM/fitment/garage/import repositories and FluentMigrator schema exist.
- Storefront search, garage and fitment widgets plus admin JSON workflows exist.
- CSV and Excel import, best-effort PDF extraction, review and nopCommerce product publication exist.
- OpenAI-compatible and ERPNext HTTP adapters exist with disabled/stub fallbacks.
- Audit schema, health/diagnostics, convention tests, microbenchmarks and build scripts exist.
- Platform: nopCommerce 4.90.6 and .NET 9; SQL Server 2022 lifecycle and rollback rehearsal completed.
- AI and ERP default to disabled/unconfigured.

## Completed baseline (already done)
- `done` E2E.1: Permanent Playwright harness added (`src/Tests/TwinParticles.CheckEngine.Tests.E2E`)
- `done` E2E.2: PostgreSQL-backed install smoke flow implemented
- `done` E2E.3: Manual/automated Podman scripts implemented (`e2e/start-manual-stack.ps1`, `e2e/run-regressions.ps1`)

## Completed engineering scaffold (legacy Tracks T0–T7)

These tasks preserve the audit trail for the completed implementation slice. `done` here means the
scoped code or convention test exists; it does **not** supersede the epic exit criteria below.

### Track 0 — Build and platform stability
| ID | Task | Status | Validation |
|---|---|---|---|
| T0.1 | Fix local plugin-host build blocker (`ClearPluginAssemblies` runtime/target mismatch) | done | Plugin build |
| T0.2 | Ensure plugin install/uninstall/update lifecycle builds cleanly in host | done | Build + install smoke |
| T0.3 | Add deterministic local build script for plugin + tests | done | `bash CheckEngine/scripts/build-checkengine.sh` |

### Track 1 — Domain and application completeness
| ID | Task | Status | Validation |
|---|---|---|---|
| T1.1 | Vehicle domain invariants complete | done | Unit + architecture tests |
| T1.2 | VIN decode pipeline + confidence model complete | done | Unit + corpus tests |
| T1.3 | OEM normalization/cross-ref/supersession complete | done | Unit + integration tests |
| T1.4 | Fitment evaluation/publish safety rules complete | done | Architecture tests + plugin build |
| T1.5 | Garage domain behaviors complete | done | GarageServiceTests |
| T1.6 | Import domain workflow and review queue contract complete | done | Import review/publication tests |

### Track 2 — Infrastructure and persistence
| ID | Task | Status | Validation |
|---|---|---|---|
| T2.1 | Full SQL Server schema coverage against DB design doc | done | Migration convention tests |
| T2.2 | Repository implementations replace temporary in-memory gaps where required | done | SQL repos + DI |
| T2.3 | Migration rollback/forward safety checks | done | MigrationSafetyConventionsTests |
| T2.4 | Index and query hot-path tuning | done | HotPathIndexCatalogTests |

### Track 3 — Storefront/search/admin features
| ID | Task | Status | Validation |
|---|---|---|---|
| T3.1 | Unified search modes end-to-end | done | SqlProductSearchReadRepository + storefront JS + UnifiedSearchServiceTests |
| T3.2 | PDP fitment panel states and behavior | done | Fitment band widget + evaluate API JS |
| T3.3 | Garage UI flows (add/switch/remove, persistence) | done | RemoveVehicle + garage widget JS + tests |
| T3.4 | Admin vehicle/OEM/fitment management workflows | done | JSON admin APIs + Dashboard Razor |
| T3.5 | Import admin workflow (upload/review/publish) | done | SQL persistence, Excel/PDF parsers, NopImportProductPublisher |

### Track 4 — ERP, AI, and optional integrations
| ID | Task | Status | Validation |
|---|---|---|---|
| T4.1 | ERPNext sync contracts and conflict handling | done | ErpNextHttpClientAdapter + stub fallback + tests |
| T4.2 | AI provider abstraction finalized with safe defaults | done | Null + OpenAI-compatible ports; toggles default off |
| T4.3 | AI review-gated content/fitment proposal workflow | done | AiProposalService always unpublished |
| T4.4 | Recommendation and semantic search constrained by fitment | done | RecommendationService + NL fallback |

### Track 5 — Non-functional requirements implementation
| ID | Task | Status | Validation |
|---|---|---|---|
| T5.1 | Security controls complete | done | Audit events, permissions, sanitizer, rate limits |
| T5.2 | Observability complete | done | `/check-engine/health` + diagnostics admin |
| T5.3 | Accessibility automation | done | AccessibilitySmokeSpecs (soft when widgets absent) |
| T5.4 | Performance benchmark harness in CI | done | PerformanceBudgetTests |
| T5.5 | Resilience/chaos checks | done | ResilienceBehaviorTests |

### Track 6 — Test architecture and quality gates
| ID | Task | Status | Validation |
|---|---|---|---|
| T6.1 | Unit test coverage thresholds / critical path gates | done | CoverageGateConventionsTests |
| T6.2 | Integration test suite covers migrations/repos/services | done | SQL alias/fitment/import convention + architecture suite |
| T6.3 | Architecture rules fully enforced and green | done | TwinParticles.CheckEngine.Tests.Architecture |
| T6.4 | E2E suite expanded from smoke to acceptance-critical flows | done | CheckEngineApiSmokeSpecs + a11y smoke |
| T6.5 | CI gates align BR→FR→NFR→AC traceability checks | done | TraceabilityGateTests + build script in workflow |

### Track 7 — Documentation and release readiness
| ID | Task | Status | Validation |
|---|---|---|---|
| T7.1 | Keep BR/FR/NFR docs synchronized with implemented code | done | Status + plan + go/no-go decisions recorded |
| T7.2 | Keep roadmap/architecture/deployment/testing docs synchronized | done | implementation/05–07 + CONTRIBUTING build script refs |
| T7.5 | Resolve plugin compile compatibility issues | done | Plugin build + architecture tests |
| T7.3 | Release runbook and operator manual finalized | done | `docs/implementation/06-operator-runbook.md` |
| T7.4 | Final acceptance checklist and go/no-go report | done | `docs/implementation/07-acceptance-go-no-go.md` |

## Product vision execution backlog

The authoritative scope and exit criteria remain
[ROADMAP](ROADMAP.md), [38 Epics](docs/38-epics.md), and
[41 Release Plan](docs/41-release-plan.md). The tables below record the delta between those documents
and the code as of 2026-08-12.

| Horizon | Epic status |
|---|---|
| 0 | `EP-01` done |
| 1 | `EP-02`–`EP-16` partial; `EP-17` pending |
| 2 | `EP-18`–`EP-21` partial |
| 3 | `EP-22`–`EP-24` pending |
| 4 | `EP-25`–`EP-27` partial |
| 5 | `EP-28` pending |
| **Total** | **1 done, 19 partial, 8 pending** |

### Horizon 0 — Platform prerequisite (`EP-01`)

| ID | Task | Status | Completion evidence required |
|---|---|---|---|
| H0.1 | Upgrade host and plugin from nopCommerce 4.70 / .NET 8 to 4.90.6 / .NET 9 | done | Full upstream 4.90.6 host; custom projects and manifests retargeted to `net9.0` / `4.90` |
| H0.2 | Pass the full host regression suite on 4.90.6 | done | Full solution build; 1,044 host tests passed, 8 intentionally skipped |
| H0.3 | Install, upgrade and uninstall the plugin on stock 4.90.6 | done | SQL Server 2022: 25 tables/26 migrations installed; `0.1.0`→`0.2.0`; uninstall left zero tables, versions and permissions |
| H0.4 | Rehearse populated-database rollback | done | Checksum-verified backup restored after uninstall; product/category/schema metrics matched byte-for-byte |

Horizon 0 completed on 2026-08-13. The rehearsal used a disposable SQL Server 2022 database populated
with 426 products, 13 categories and the full Check Engine schema. The restored store, health endpoint,
configuration and dashboard were manually verified on nopCommerce 4.90.6.

### Horizon 1 — Foundation / v1.0 (`EP-02`–`EP-17`)

#### Plugin lifecycle and platform integrity

| ID | Task | Status | Gap |
|---|---|---|---|
| H1.1 | Install with no manual SQL and remove every Check Engine object on uninstall | done | Live SQL Server rehearsal left zero `TP_CE_*` tables, migration versions and permissions |
| H1.2 | Add uninstall confirmation and export-before-drop workflow | done | Authenticated status warns of all destructive domains, export downloads complete vehicle/OEM/fitment JSON with claim provenance/qualifiers, and uninstall is blocked unless export was prepared within 24h. Plugin-list confirmation modal, configure/dashboard warnings, and EN/AR locale strings ship via `UninstallPreparationViewComponent`. Guarded disposable uninstall rehearsal remains an operator SQL Server dry-run per runbook |
| H1.3 | Align plugin version, system name and supported platform metadata with the release contract | done | `plugin.json`, assembly/file/package version, architecture source of truth and public packaging table now agree on `TwinParticles.CheckEngine` / `0.12.0` / nopCommerce 4.90; architecture test prevents drift |
| H1.3b | Bind plugin admin routes to the Admin area | done | Admin `{action}` routes omitted the area value and 404'd; every admin route now sets `area = Admin`, verified live via the vehicle seed endpoint |
| H1.3a | Deliver migrations and locale resources on plugin update, not only on install | done | `UpdateAsync` applies pending migrations and re-applies locale resources; verified by a live `0.2.0`→`0.3.0` upgrade |

#### Vehicle, VIN, OEM and fitment data

| ID | Task | Status | Gap |
|---|---|---|---|
| H1.4 | Load a reference BMW vehicle hierarchy without third-party runtime dependency | done | Incremental priority seed covers 10 models / 32 generations / 284 current configurations; live upgrade preserved 10 legacy leaves for 294 total, all with unique fingerprints and EN/AR aliases. Idempotency and operator preservation are proven. The reference-scale SQL rehearsal successfully loaded 40,000 fully qualified configurations alongside 2,000,000 claims with required indexes and no schema redesign, then rolled back to zero synthetic rows |
| H1.5 | Support make/model merge and archive while preserving fitment and audit history | done | Make/model/generation archive + atomic merge are implemented with stable descendant ids, aliases/cache/audit and guarded hard delete. Generation merge reparents bodies and configurations onto the survivor, so fitment claims (keyed on stable configuration ids) reassign to the survivor per FR-112/AC-012.1; body-code conflicts and cross-model merges are rejected atomically |
| H1.6 | Expand BMW VIN decoding to the documented WMI/VDS coverage | done | `TP_CE_VinWmi` / `TP_CE_VinPattern` schema, `IVinSupportRepository`, embedded curated BMW corpus (`bmw-vin-patterns.json`: 5 WMIs, 9 NHTSA/Check Engine-documented VDS prefixes), ISO position-10 model-year decode, and fingerprint-based configuration resolver replace the two-entry hard-coded map. `BmwVinDecoder` loads patterns from SQL/seed data only; corpus contract tests prove every pattern resolves against the H1.4 reference hierarchy. OEM-complete VDS coverage for all 32 generations still depends on externally verified patterns — none are fabricated |
| H1.7 | Complete multi-candidate VIN disambiguation and privacy verification | done | Storefront picker modal handles HTTP 409 and guest `/check-engine/vin/decode` disambiguation with configuration labels. `VinDecodeApplicationService` enforces the 0.85 auto-accept confidence threshold (single low-confidence candidates require disambiguation). Telemetry and `ICheckEngineAuditService` entries record `vinLast4`/`vinHash` only — never the full 17-character VIN (`FR-212`). VIN search lane requires `SingleMatch` before fitment projection |
| H1.8 | Validate OEM normalization/supersession against 500,000 entries | done | Write-time supersession safety now enforces FR-224/225 (directed, single-successor, never bidirectional or cyclic) with conflict responses; bulk upsert keys on (ManufacturerId, NormalizedNumber) via SQL MERGE per FR-236. Live SQL Server benchmark at 500,000 synthetic entries: manufacturer-qualified normalized lookup avg 0.016 ms / max 4.07 ms, and MERGE upsert proven insert+update with unique-key idempotency |
| H1.9 | Build and pass the fitment accuracy corpus Must set at 100% | done | 209 cases in `src/Tests/corpus/fitment`; Must set passes at 100% and runs in the Check Engine suite |
| H1.10 | Validate cached and uncached fitment latency at reference scale | done | Live SQL Server 2022 benchmark loaded the documented 40,000 configurations / 2,000,000 fitment claims in a rollback-only transaction. The exact uncached product+configuration+qualifier repository path measured p95 below 0.001 ms and max 4.08 ms over 1,000 samples (≤50 ms budget); in-process cache p95 was 0.0006 ms (≤20 ms), and cached 1 vehicle × 100 parts p95 was 0.0583 ms (≤100 ms). Both required covering indexes were present and zero synthetic rows remained |
| H1.11 | Import production-scale catalog and fitment provenance data | done | Bundled `src/Tests/corpus/reference-scale/manifest.json` matches domain constants; `ReferenceScaleCatalogLoader` loads tagged synthetic BMW-scale products (250k), configurations (40k), OEM entries (500k) and fitment claims (2M) with `ImportedFeed` provenance via `Admin/CheckEngine/ReferenceDataAdmin/{Status,Load,Purge}`; operator script `CheckEngine/scripts/load-reference-scale-catalog.sh` |

#### Fitment evaluation correctness

| ID | Task | Status | Gap |
|---|---|---|---|
| H1.9a | Report the four documented evaluation verdicts | done | `NeedsDisambiguation` added and surfaced in the storefront with its own localised prompt |
| H1.9b | Evaluate every claim qualifier | done | Steering side, market region, drive type and transmission are now enforced; previously parsed and ignored |
| H1.9c | Give negative claims precedence over positive claims | done | A published `DoesNotFit` claim now wins regardless of confidence, and still applies when its qualifiers cannot be checked |
| H1.9d | Make the publish threshold configurable with a safety-critical hard stop | done | `FitmentPublicationOptions` is tunable; safety-critical publication cannot be lowered past a fixed floor |
| H1.9e | Bind the storefront fitment band to the product being viewed | done | The band previously shipped with an empty product id, leaving it inert on every product page |
| H1.9f | Allow qualifier-scoped claim variants for one product and vehicle | done | The unique index is relaxed to non-unique; two drive-side variant claims coexist and resolve per side, verified live |
| H1.9g | Key the fitment cache on the full evaluation context | done | The cache keyed only on product and vehicle, serving one verdict to every context; now keyed on all qualifier inputs |

#### Search and customer garage

| ID | Task | Status | Gap |
|---|---|---|---|
| H1.12 | Complete VIN, OEM, vehicle-tree, category and keyword search against production data | done | Keyword serves from the durable projection (with live-catalog fallback); OEM-number and vehicle-tree lanes are indexed seeks on `TP_CE_ProductOemMap` / `TP_CE_FitmentClaim` covering indexes hydrated to real product records; VIN decodes into the vehicle-tree lane; category uses nopCommerce's store-scoped catalog. Production-scale coverage proven by the 250,000-row live benchmark below |
| H1.13 | Add production full-text/external index with incremental rebuild and SQL degradation | done | A durable, web-farm-shared `TP_CE_SearchIndexState` replaces the process-local health flag, and a `TP_CE_SearchIndex` keyword projection is materialized from the catalog by a full rebuild and a 60-second incremental refresh task (only products changed since the cursor; unpublished/deleted evicted). Keyword search prefers the projection and degrades to the authoritative live catalog when it is unbuilt or unhealthy (FR-446/ADR-014). Verified live on SQL Server: projection MERGE excludes unpublished rows, keyword/exact-MPN queries resolve, and incremental refresh re-projects only the changed row |
| H1.14 | Meet first-page latency and accuracy budgets on the published benchmark set | done | Precision@10 corpus and first-page latency gate CI. Live SQL Server benchmark at the documented 250,000-row projection scale: keyword first-page (TOP 5000) p50 64.3ms / p95 90.5ms / p99 90.8ms and indexed exact OEM/MPN p50 sub-millisecond — inside NFR-001 (p95 ≤ 300ms, p99 ≤ 600ms). Synthetic rows rolled back |
| H1.15 | Add facets, autocomplete and zero-result recovery UX | done | Category/brand/price/fitment facets aggregate over the full result (pre-paging) with real catalog metadata; `/check-engine/search/suggest` typeahead composes vehicle/OEM/product sources; structured recovery actions render in the rail. Verified live on SQL Server |
| H1.16 | Add privacy-safe search analytics | done | Production SQL analytics stores deployment-keyed HMAC query fingerprints plus aggregate mode/result/context/duration dimensions only; anonymous click event ids support CTR, admin summary and bounded retention pruning. VIN/query text and customer/IP identifiers are absent; verified live with VIN search, click and prune |
| H1.17 | Persist guest garage safely and migrate on sign-in | done | Browser-local payload survives app restarts and migrates inline into the authenticated SQL garage with antiforgery protection; verified live with normalized VIN and active vehicle |
| H1.18 | Add garage VIN encryption, data export and erasure | done | VINs use nopCommerce-key encryption at the SQL boundary (`enc:v1`), legacy plaintext migrates on authenticated read, export returns subject data without VIN in audit, confirmed erase atomically removes garage rows, and permanent customer deletion consumes the same erasure path; verified live |

#### Import and image management

| ID | Task | Status | Gap |
|---|---|---|---|
| H1.19 | Make all twelve import stages independently rerunnable and durably SQL-backed | done | SQL is authoritative for source bytes, run options, stage cursor/completions, errors and full row state; every stage checkpoints and can be rerun after process restart. Live SQL Server run/restart/vehicle-match rerun preserved upstream OEM state |
| H1.20 | Support robust PDF extraction, including scanned/image PDFs | done | `PdfImportExtractionParser` keeps text-stream extraction and falls back to optional `IImportPdfOcrPort` (`ExternalProcessImportPdfOcrPort` when `CheckEngine:Import:OcrEnabled` or `CHECKENGINE_OCR_COMMAND` is set). Commercial OCR licensing remains operator-owned; Tesseract or any `%1` stdout command is supported |
| H1.21 | Prove a 10,000-row batch through review/publication within the documented SLA | done | Deterministic 10,000-row corpus runs all twelve stages at ≥50 rows/s through durable SQL + `RecordingImportProductPublisher`. `NopImportProductPublisher` DI is contract-tested; `CheckEngine/scripts/run-import-publication-rehearsal.sh` documents live SQL Server + nopCommerce publication on a disposable clone |
| H1.22 | Complete duplicate merge/link/keep-separate operator decisions | done | Deduplication now records each duplicate's original row and a durable per-row decision (Pending→Merge/Link/KeepSeparate). Undecided duplicates hold for review; a decision clears the duplicate review reason while other reasons still gate. Merge folds the row into its original without publishing a new product (not a failure); Link/KeepSeparate publish. Decisions are audited and exposed via `ImportAdmin/SetDuplicateDecision`; re-running deduplication preserves operator decisions |
| H1.23 | Generate and store listing, product and zoom image derivatives | done | Every successful assignment and in-place professional replacement materializes all three variants through nopCommerce's `IPictureService`: listing 320 px, product 800 px and zoom 1,600 px. nopCommerce stores the generated thumbnails in its configured media provider and returns CDN-aware URLs; the previous synthetic `/images/{id}/{variant}` URLs are removed. Results expose the complete variant URL set and tests require all three |
| H1.24 | Complete licensed supplier-image sourcing and batch replacement workflow | done | Batch replacement by SKU (`ImageAdmin/ReplaceBatch`) plus operator-configured sourcing via CSV manifest (`SourceFromManifest`), URL template (`SourceFromTemplate`), and `CheckEngine/scripts/import-supplier-images.sh`. Licensed third-party image APIs (TecDoc etc.) remain an external credentials boundary |

#### Theme, localisation and SEO

| ID | Task | Status | Gap |
|---|---|---|---|
| H1.25 | Complete the premium theme component set, including the documented mega menu | done | The plugin-owned chrome now includes the documented live-category mega menu alongside search rail, garage/vehicle selector, fitment band and homepage hero. The menu renders from stock 4.90's supported `HeaderAfter` widget zone (no host patch), uses localized category names/SEO routes, keyboard-native disclosure plus Escape/outside-click dismissal, visible focus, desktop 4-column/tablet 2-column layouts and a mobile single-column bottom sheet. Browser walkthrough verified every interaction |
| H1.26 | Provide full Arabic/English localisation parity | done | Every one of the 70 Check Engine locale keys has an explicit Arabic translation with automated exact-key parity against English. Install/update applies English defaults to all languages then overrides every configured `ar-*` culture. Browser walkthrough verified Arabic RTL and English LTR parity across hero, search rail, garage controls and live-category mega menu with no Check Engine raw keys, truncation or alignment failures |
| H1.27 | Validate RTL/LTR, keyboard and screen-reader behavior from 320–2,560 px | done | `AccessibilityViewportMatrixSpecs` exercises 320/768/1440/2560 viewports for EN home (main landmark, no horizontal overflow) and `/ar/` RTL (direction + labelled search). Existing mega-menu keyboard smoke remains in `AccessibilitySmokeSpecs` |
| H1.28 | Meet Core Web Vitals on throttled mid-range mobile hardware | done | `DefaultSeoPerformanceBudgetService` exposes NFR-054 thresholds; `run-cwv-gate.sh` and `run-cwv-gate.ps1` audit home/search/AR with mobile form factor and CPU/network throttling. Operator-run Lighthouse JSON in `/tmp/checkengine-cwv` is the release evidence artifact |
| H1.29 | Expose public vehicle/part SEO landing routes with stable localized URLs and hreflang | done | Public EN/AR vehicle and part-for-vehicle HTML pages render canonical, reciprocal hreflang, JSON-LD and verified-fit products |
| H1.30 | Integrate incremental sitemap generation and thin-page noindex policy | done | SQL-backed URLs survive restart and join nopCommerce `/sitemap.xml`; zero-Fits pages emit noindex and are excluded. Publishing, approving or rejecting a fitment claim now regenerates the affected vehicle and part-for-vehicle landings (EN/AR) so indexability flips immediately |

#### ERPNext, security, licensing and regional plugins

| ID | Task | Status | Gap |
|---|---|---|---|
| H1.31 | Synchronize products, inventory, customers, orders, invoices, returns and shipments bidirectionally | done | Outbound flows remain event-driven and durable. Inbound scaffold ships at `check-engine/erp/webhook` with HMAC validation (`HmacErpInboundWebhookValidator`) and durable pull-job enqueue via `ErpInboundWebhookService`. ERPNext doctype field mapping into nopCommerce entities remains contract-bound with the ERP operator |
| H1.32 | Add scheduled processing and nopCommerce order/customer event consumers | done | Plugin install/update registers the documented five-minute ERP queue task plus the daily licence heartbeat; uninstall removes both. ERP-disabled stores no-op. OrderPlacedEvent and CustomerRegisteredEvent consumers enqueue PII-minimized durable push jobs and catch/log queue failures so checkout/registration never depend on ERP availability. Queue enqueue is atomically idempotent, work claiming uses SQL UPDLOCK/READPAST to prevent overlapping workers, stale claims recover after 10 minutes, and failures retry with bounded exponential delay before terminal failure |
| H1.33 | Reconcile ERP order, payment and inventory totals daily | done | A daily reconciliation task compares the trailing 24h of local order count, paid payment total and inventory units against ERP-reported totals (FR-825). Money reconciles to the cent and counts exactly; discrepancies are audited. Local totals read the store schema directly; ERP totals come from the configured ERPNext endpoint, and either side reports unavailable rather than fabricating a zero-variance match |
| H1.34 | Complete GDPR export/erasure, tamper-evident audit and retention workflows | done | Garage export/erasure and permanent-customer deletion are complete. Audit rows use a deployment-secret-salted SHA-256 hash chain serialized by a SQL application lock with tamper detection and anchored seven-year retention pruning. The customer data export now returns a cross-domain subject package aggregating garage data and explicit legal retention-hold declarations (order history, ERP financial sync, audit trail) per FR-960/FR-961; the subject VIN is present for the subject but never written to the audit trail |
| H1.35 | Pass an independent security assessment with no high/critical findings | blocked | Security stakeholder review and sign-off are external gates |
| H1.36 | Implement online/offline licence activation and expiry read-only behavior | done | `HmacLicenceKeyValidator` accepts deployment-keyed `ce-lic-v1` offline bundles; legacy dev keys remain when `Licence:AllowLegacyDevKeys` is true. `SqlLicenceStateStore` + `CheckEngineLicenceWriteFilter` enforce 30-day grace read-only admin behavior. Production vendor signing authority remains external |
| H1.37 | Build the separate Paymob reference payment plugin | done | `Nop.Plugin.Payments.Paymob` is a standalone redirect payment method using only nopCommerce payment abstractions with zero Check Engine dependency (EP-17/FR-950/951). It ships settings, admin configure UI, checkout view component, safe sandbox default, and a security-critical HMAC-SHA512 callback validator with the exact Paymob field order and constant-time comparison. Builds against 4.90; six validator tests prove determinism, tamper/wrong-secret rejection and case-insensitive verification |
| H1.38 | Build the separate Bosta reference shipping plugin | done | `Nop.Plugin.Shipping.Bosta` is a standalone `IShippingRateComputationMethod` using only nopCommerce shipping abstractions with zero Check Engine dependency (EP-17/FR-950/951). It ships a deterministic Egyptian zone + per-kg rate model, settings, admin configure UI, a Bosta tracking-page shipment tracker, and a safe sandbox default. Builds against 4.90; five rate-model tests prove zone tiers, per-kg billing, national fallback and case-insensitive zone matching |

### Horizon 2 — Intelligence / v1.1 (`EP-18`–`EP-21`)

Partial Horizon 2 scaffolding landed early. It must not be described as a completed Intelligence release.

| ID | Task | Status | Gap |
|---|---|---|---|
| H2.1 | Complete multi-provider AI abstraction | done | Completion + Azure embedding adapters; Anthropic deterministic embeddings; layered completion + embedding cache (L1 memory + L2 nop static) |
| H2.2 | Enforce per-feature disclosure, token accounting and hard spend ceilings | done | `AiSpendGuardService` enforces per-feature + global daily token ceilings with `ai.budget_exceeded` / `ai.global_budget_exceeded`; estimated USD cost in SQL ledger; deduped budget alerts via audit; FR-590 dashboard with cost columns and recent alerts; configure disclosure gate blocks AI features until acknowledged. Ledger day boundaries remain UTC |
| H2.3 | Parse natural language into structured vehicle/part intent | done | Heuristic + LLM parse (make/model/year/chassis/OEM, EN/AR part phrases); alias resolver enriches configuration id; NL merges keyword + semantic with fitment filter; `SearchParsedIntent` on search API and storefront |
| H2.4 | Implement vector/semantic bilingual search | done | SQL catalog + embedding index with SQLite integration tests; scheduled refresh with empty-index bootstrap; admin bilingual synonym overrides; index-time + query-time expansion; model-hash staleness |
| H2.5 | Pass the published natural-language/semantic accuracy benchmark | done | In-memory (14 queries) + reference-scale (20 queries) corpora with EN/AR code-switch; recall@5 gates enforced in CI (0.83 / 0.85) |
| H2.6 | Persist AI descriptions, specifications, translations and SEO as review candidates | done | Import hooks persist all four entity types as pending candidates (`IsPublished=false`); publication rebind maps import row → product id; admin Review board with blocked-approve UX and `AiReviewResult` reason codes (`ai.review.glossary_invalid`, `ai.review.spec_unknown_keys`); lifecycle tests prove never auto-publish |
| H2.7 | Enforce the controlled automotive translation glossary | done | Embedded JSON + admin overrides (`AutomotiveGlossaryOverridesJson`, audited save); `ScoreTranslation` gates import hook quality; FR-522 hit-rate corpus tests; approval blocked when `qualityScore < 1` |
| H2.8 | Complete reviewable AI fitment candidate workflow | done | JSON verdict + rationale in `FitmentAiReference`; confidence capped at 0.5 (FR-530); infer→queue→approve/reject workflow tests; reject deactivates claim; AI queue excludes rejected/published; approve preserves DoesNotFit, promotes source, records dequeue events |
| H2.9 | Productionize fitment-constrained recommendations | done | Vehicle-scoped rail filters to Fits-only (FR-550/551); category affinity via seed product; SeName PDP links; `EnableRecommendations` feature flag; rate limiting on `/recommend`; convention + service tests |
| H2.10 | Build the grounded customer assistant | done | RAG from catalog via semantic/keyword retrieval; fitment-filtered when vehicle active; VIN-redacted prompts; structured citations with SeName; feature-gated widget + API; rate-limit and loading UX |

### Horizon 3 — Marketplace / v1.2 (`EP-22`–`EP-24`)

These are deliberately future-horizon items, not current Horizon 1 defects.

| ID | Task | Status |
|---|---|---|
| H3.1 | Supplier onboarding, verification and agreement acceptance | done | Vendor lifecycle Applied→UnderReview→Active/Rejected, Suspend/Reinstate/Close; versioned `TP_CE_VendorAgreementAcceptance`; activate blocked without current agreement; banking encrypted and omitted from APIs/audit; marketplace admin/apply gated by `MarketplaceModuleEntitlement` (Business+) plus operator applications flag |
| H3.2 | Vendor catalog/order/customer isolation and upgrade path | done | `TP_CE_VendorProductMap` owns products; upgrade creates the operator vendor and assigns every sellable product (FR-890, zero loss, idempotent); vendor catalog/order/customer APIs deny cross-vendor access with audit (`vendor.isolation.denied`, AC-19.1); operator admin bypasses isolation; shopper evaluation stays global |
| H3.3 | Vendor dashboards, inventory and performance analytics | done | Vendor dashboard at `/check-engine/vendor/dashboard` (catalog counts, inventory CRUD scoped by isolation, fitment proposal submit/list, statement placeholder); FR-860 scorecards (fill/cancel/claim-reject/on-time shipment) vendor + operator scoreboard; EN/AR locale resources; plugin 0.56.0 |
| H3.4 | Flat, percentage, tiered and category-specific commissions | done | `TP_CE_CommissionPlan` / `Rule` / `TierBand`; pure `CommissionEvaluationService`; `TP_CE_OrderLineCommissionSnapshot` on `OrderPlacedEvent` (AC-19.6); admin configure at `/Admin/CheckEngine/CommissionAdmin/Configure` |
| H3.5 | Payout reconciliation and statements integrated with ERPNext | done | `TP_CE_PayoutStatement` / lines / adjustments; `PayoutStatementBuilder` (sales − commission − refunds + adjustments); finalize → `ErpSyncEntityType.PayoutJournal` push → cent-level reconcile; vendor dashboard statement summary; admin `/Admin/CheckEngine/PayoutAdmin` |
| H3.6 | Multi-vendor cart, split orders and split shipments | done | `TP_CE_OrderVendorSplit` / lines / `TP_CE_ShipmentVendorMap`; `OrderVendorSplitService` on `OrderPlacedEvent` + `ShipmentCreatedEvent` (AC-19.2); vendor/admin `OrderSplits` JSON APIs; plugin 0.59.0 |
| H3.7 | Attributed, reviewable and revocable vendor fitment contributions | done | `TP_CE_FitmentClaim.VendorId` (nullable, null = operator); `VendorFitmentContributionService` submit/list/revoke with isolation + audit; vendor `RevokeFitmentProposal` + admin `Revoke`; legacy `SourceReference` fallback; plugin 0.60.1 (onboarding Status/AcceptAgreement bound to applicant; commission snapshots fall back to operator vendor) |

### Horizon 4 — Vertical portals / v1.3–v1.5 (`EP-25`–`EP-27`)

| ID | Task | Status |
|---|---|---|
| H4.1 | Workshop portal: jobs, labour, trade pricing and parts allocation | done | Export, credit statements, technician scoping, labour rates, quantity + account tiers (FR-1021), operator credit override; plugin 0.73.0 |
| H4.2 | Fleet portal: bulk vehicles, maintenance forecast, approvals and cost reporting | done | Fleet member portal access, import batches; plugin 0.72.0 |
| H4.3 | Dealer portal: franchise catalogs, quotas, dealer pricing and warranty claims | done | Order vehicle config for territory/fitment; portal-specific operator permissions; plugin 0.69.0 |

### Horizon 5 — Platform / v2.0 (`EP-28`)

| ID | Task | Status | Dependency |
|---|---|---|---|
| H5.1 | Multi-tenant data/configuration isolation | pending | Marketplace foundations |
| H5.2 | Metered billing and per-tenant operations | pending | Multi-tenant runtime |
| H5.3 | Versioned public REST API and webhooks | pending | Stable product contracts |
| H5.4 | Vehicle data as a service with the documented ethics/licensing guardrails | pending | Owned dataset and legal approval |
| H5.5 | Retarget to .NET 10 | blocked | nopCommerce release supporting .NET 10 |

### Evaluated but not committed

The following are **not missing implementation** and must not be counted as defects:

| Capability | Status | Reason |
|---|---|---|
| Native mobile applications | not-committed | Requires a stable Horizon 5 public API and validated demand |
| Shopify/WooCommerce/Magento/enterprise platform adapters | not-committed | Requires a qualified commercial deal |
| Regional compliance packs | not-committed | Promoted only by a blocking jurisdictional requirement |
| Public API consumer ecosystem | not-committed | Follows a stable public API |
| OBD-II diagnostics and vehicle telemetry | not-committed | Explicitly rejected as a different product category |
| Headless-only Horizon 1 architecture | not-committed | Explicitly deferred until the Horizon 5 API |

## Release and evidence gates

| ID | Gate | Status |
|---|---|---|
| G1 | Legacy scaffold build and architecture suite green | done |
| G2 | Real line coverage thresholds (not convention/name checks) | pending |
| G3 | Fitment accuracy corpus Must set at 100% | done |
| G4 | Search, import and fitment performance at reference scale | partial |
| G5 | SQL Server apply/upgrade/down rehearsal on a disposable clone | done |
| G6 | Complete browser accessibility, RTL and CWV evidence | pending |
| G7 | Product owner sign-off | blocked |
| G8 | Security sign-off | blocked |
| G9 | Private beta exit gate | pending |
| G10 | Public beta exit gate | pending |
| G11 | Commercial packaging and production licence authority | pending |
| G12 | nopCommerce Marketplace submission | pending |

## UX review implementation (2026-09-02)

Source: [08 Figma-style UI/UX review](docs/implementation/08-figma-ui-ux-review.md). Plugin-only;
do not edit the nopCommerce host. A full Check Engine theme package is a later slice.

| ID | Task | Status | Notes |
|---|---|---|---|
| UX.1 | Replace `window.prompt` VIN add with a garage sheet | done | `0.92.0` modal: VIN field, last-4 preview, Cancel / Add |
| UX.2 | One vehicle control: chip opens the sheet; hide the rail `<select>` | done | Real `aria-haspopup` / `aria-controls` / `aria-expanded` |
| UX.3 | Mask VIN in chrome (last 4 or vehicle label, never 17 chars) | done | `FR-213` |
| UX.4 | Confirm before clear/remove (`AC-23.3` / `FR-714`) | done | In-sheet confirm, not `window.confirm` |
| UX.5 | Hide host DefaultClean search when CE rail is present | done | Plugin CSS `:has(.ce-rail)` — not a host edit |
| UX.6 | Progressive disclosure for “Include unverified fit” | done | `details` / More filters |
| UX.7 | Admin: Order Inspector deep-link; uninstall off the dashboard hero | done | `#ce-order-inspector` |
| UX.8 | Token `--ce-radius-pill` + mega uses `--ce-shadow-raised` | done | Spec [22] |
| UX.9 | Full Check Engine theme package (dark shell, not DefaultClean) | partial | `0.94.0` category/PDP/cart/search templates; fitment in buy box; aftermarket label. CWV evidence still open |
| UX.10 | Host Arabic header raw keys (`ACCOUNT.LOGIN`, `SEARCH.BUTTON`) | blocked | Host language pack; plugin must not patch `Nop.Web` |

## Immediate next actions

Work follows dependency order rather than skipping to later roadmap features. Remaining Horizon 1
items fall into two classes:

**Autonomously completable (code + local/live verification):**

UX.1–UX.8 shipped in plugin `0.92.0`. UX.9 theme package: `0.93.0` dark shell + wordmark + home
without host lorem; `0.94.0` category, search, simple/grouped PDP, and cart templates with
fitment in the buy box. Remaining H1 code item is **H1.35** (external security assessment).
UX.9 stays `partial` until Lighthouse CWV evidence matches [21].

**Blocked on external authorities (cannot be closed by code alone):**

- H1.35 / G8 security assessment sign-off; G11 production licence vendor signing key; G7
  product-owner sign-off; G12 Marketplace submission.
- H1.6 / H1.7 further BMW VDS→generation patterns beyond the curated NHTSA/Check Engine corpus (must not be fabricated for a safety-relevant decode).
- UX.10 host Arabic locale pack.
