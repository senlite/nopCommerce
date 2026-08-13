# Check Engine End-to-End Execution Plan

**Purpose:** Single source of truth for implementation progress and remaining work.

**Last updated:** 2026-08-12

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
| 4 | `EP-25`–`EP-27` pending |
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
| H1.2 | Add uninstall confirmation and export-before-drop workflow | partial | Authenticated status warns of all destructive domains, export downloads complete vehicle/OEM/fitment JSON with claim provenance/qualifiers, and uninstall is blocked unless export was prepared within 24h. Live export verified; nopCommerce plugin-list confirmation UI integration and guarded disposable uninstall rehearsal remain |
| H1.3 | Align plugin version, system name and supported platform metadata with the release contract | done | `plugin.json`, assembly/file/package version, architecture source of truth and public packaging table now agree on `TwinParticles.CheckEngine` / `0.12.0` / nopCommerce 4.90; architecture test prevents drift |
| H1.3b | Bind plugin admin routes to the Admin area | done | Admin `{action}` routes omitted the area value and 404'd; every admin route now sets `area = Admin`, verified live via the vehicle seed endpoint |
| H1.3a | Deliver migrations and locale resources on plugin update, not only on install | done | `UpdateAsync` applies pending migrations and re-applies locale resources; verified by a live `0.2.0`→`0.3.0` upgrade |

#### Vehicle, VIN, OEM and fitment data

| ID | Task | Status | Gap |
|---|---|---|---|
| H1.4 | Load a reference BMW vehicle hierarchy without third-party runtime dependency | partial | Incremental priority seed now covers 10 models / 32 generations / 284 current configurations; live upgrade preserved 10 legacy leaves for 294 total, all with unique fingerprints and EN/AR aliases. Idempotency and operator preservation are proven; the documented 40,000-configuration reference-environment capacity/load proof remains |
| H1.5 | Support make/model merge and archive while preserving fitment and audit history | done | Secured make/model archive + merge workflows use atomic conflict-checked SQL; model merge keeps generation/configuration ids stable, preserving fitment/garage/SEO references. Hard delete with descendants returns archive-required; aliases, cache invalidation and attributed audit verified live |
| H1.6 | Expand BMW VIN decoding to the documented WMI/VDS coverage | partial | ISO validation exists, but decoder mappings cover only a small hard-coded set |
| H1.7 | Complete multi-candidate VIN disambiguation and privacy verification | partial | Candidate contracts exist; production corpus and log audit do not |
| H1.8 | Validate OEM normalization/supersession against 500,000 entries | partial | SQL implementation exists; scale and conflict benchmarks do not |
| H1.9 | Build and pass the fitment accuracy corpus Must set at 100% | done | 209 cases in `src/Tests/corpus/fitment`; Must set passes at 100% and runs in the Check Engine suite |
| H1.10 | Validate cached and uncached fitment latency at reference scale | partial | In-process microbenchmarks exist; production-scale data does not |
| H1.11 | Import production-scale catalog and fitment provenance data | pending | No reference-scale BMW fitment dataset is bundled or loaded |

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
| H1.12 | Complete VIN, OEM, vehicle-tree, category and keyword search against production data | partial | Keyword/category now use nopCommerce's real published catalog; OEM/tree projections hydrate real product records; VIN searches the decoded vehicle tree. Brand facet filtering now works across lanes; production-scale coverage remains |
| H1.13 | Add production full-text/external index with incremental rebuild and SQL degradation | partial | Repository failures now mark health degraded and return honest empties; full-text/external indexing and a real rebuild remain |
| H1.14 | Meet first-page latency and accuracy budgets on the published benchmark set | partial | Precision@10 corpus and first-page latency gate CI; live SQL store (426 products / 294 vehicle configurations) measured query p95 43.8ms and configuration-suggest p95 11.1ms. The documented ~250k-product reference-scale load test remains |
| H1.15 | Add facets, autocomplete and zero-result recovery UX | done | Category/brand/price/fitment facets aggregate over the full result (pre-paging) with real catalog metadata; `/check-engine/search/suggest` typeahead composes vehicle/OEM/product sources; structured recovery actions render in the rail. Verified live on SQL Server |
| H1.16 | Add privacy-safe search analytics | done | Production SQL analytics stores deployment-keyed HMAC query fingerprints plus aggregate mode/result/context/duration dimensions only; anonymous click event ids support CTR, admin summary and bounded retention pruning. VIN/query text and customer/IP identifiers are absent; verified live with VIN search, click and prune |
| H1.17 | Persist guest garage safely and migrate on sign-in | done | Browser-local payload survives app restarts and migrates inline into the authenticated SQL garage with antiforgery protection; verified live with normalized VIN and active vehicle |
| H1.18 | Add garage VIN encryption, data export and erasure | done | VINs use nopCommerce-key encryption at the SQL boundary (`enc:v1`), legacy plaintext migrates on authenticated read, export returns subject data without VIN in audit, confirmed erase atomically removes garage rows, and permanent customer deletion consumes the same erasure path; verified live |

#### Import and image management

| ID | Task | Status | Gap |
|---|---|---|---|
| H1.19 | Make all twelve import stages independently rerunnable and durably SQL-backed | done | SQL is authoritative for source bytes, run options, stage cursor/completions, errors and full row state; every stage checkpoints and can be rerun after process restart. Live SQL Server run/restart/vehicle-match rerun preserved upstream OEM state |
| H1.20 | Support robust PDF extraction, including scanned/image PDFs | partial | Current parser is best-effort text extraction only; OCR is absent |
| H1.21 | Prove a 10,000-row batch through review/publication within the documented SLA | pending | No scale/SLA test exists |
| H1.22 | Complete duplicate merge/link/keep-separate operator decisions | partial | Detection exists; full durable operator workflow is incomplete |
| H1.23 | Generate and store listing, product and zoom image derivatives | partial | Variant contracts exist; CDN delivery builds synthetic URLs |
| H1.24 | Complete licensed supplier-image sourcing and batch replacement workflow | partial | URL assignment, quarantine and individual replacement exist; sourcing operations do not |

#### Theme, localisation and SEO

| ID | Task | Status | Gap |
|---|---|---|---|
| H1.25 | Complete the premium theme component set, including the documented mega menu | partial | Search, garage and fitment chrome exist; mega menu and full theme are absent |
| H1.26 | Provide full Arabic/English localisation parity | partial | Logical RTL CSS exists; complete Arabic resource dictionaries do not |
| H1.27 | Validate RTL/LTR, keyboard and screen-reader behavior from 320–2,560 px | partial | Smoke specs are soft when widgets are absent; no complete browser matrix |
| H1.28 | Meet Core Web Vitals on throttled mid-range mobile hardware | pending | No Lighthouse/CWV gate exists |
| H1.29 | Expose public vehicle/part SEO landing routes with stable localized URLs and hreflang | done | Public EN/AR vehicle and part-for-vehicle HTML pages render canonical, reciprocal hreflang, JSON-LD and verified-fit products |
| H1.30 | Integrate incremental sitemap generation and thin-page noindex policy | done | SQL-backed URLs survive restart and join nopCommerce `/sitemap.xml`; zero-Fits pages emit noindex and are excluded. Publishing, approving or rejecting a fitment claim now regenerates the affected vehicle and part-for-vehicle landings (EN/AR) so indexability flips immediately |

#### ERPNext, security, licensing and regional plugins

| ID | Task | Status | Gap |
|---|---|---|---|
| H1.31 | Synchronize products, inventory, customers, orders, invoices, returns and shipments bidirectionally | partial | HTTP adapter and queue exist; no complete event-driven entity flows |
| H1.32 | Add scheduled processing and nopCommerce order/customer event consumers | pending | ERP operations are manually triggered through admin endpoints |
| H1.33 | Reconcile ERP order, payment and inventory totals daily | partial | Current report summarizes queue jobs, not cross-system financial totals |
| H1.34 | Complete GDPR export/erasure, tamper-evident audit and retention workflows | partial | Garage export/erasure and permanent-customer deletion integration are complete; cross-domain subject export, tamper-evident audit chaining and retention automation remain |
| H1.35 | Pass an independent security assessment with no high/critical findings | blocked | Security stakeholder review and sign-off are external gates |
| H1.36 | Implement online/offline licence activation and expiry read-only behavior | partial | Licence service uses an in-memory state store; no production activation authority |
| H1.37 | Build the separate Paymob reference payment plugin | pending | Required by `EP-17`; absent from the repository |
| H1.38 | Build the separate Bosta reference shipping plugin | pending | Required by `EP-17`; absent from the repository |

### Horizon 2 — Intelligence / v1.1 (`EP-18`–`EP-21`)

Partial Horizon 2 scaffolding landed early. It must not be described as a completed Intelligence release.

| ID | Task | Status | Gap |
|---|---|---|---|
| H2.1 | Complete multi-provider AI abstraction | partial | OpenAI-compatible and Null ports exist; dedicated Azure OpenAI/Anthropic adapters do not |
| H2.2 | Enforce per-feature disclosure, token accounting and hard spend ceilings | partial | Toggles and in-memory ledger exist; ceilings are not enforced across every call |
| H2.3 | Parse natural language into structured vehicle/part intent | partial | Current flow extracts keywords or falls back to ordinary keyword search |
| H2.4 | Implement vector/semantic bilingual search | pending | No embedding model or vector index exists |
| H2.5 | Pass the published natural-language/semantic accuracy benchmark | pending | Benchmark corpus and target evidence are absent |
| H2.6 | Persist AI descriptions, specifications, translations and SEO as review candidates | partial | Hooks call an LLM but mostly set stage flags rather than durable candidate fields |
| H2.7 | Enforce the controlled automotive translation glossary | pending | No glossary-backed generation/validation pipeline exists |
| H2.8 | Complete reviewable AI fitment candidate workflow | partial | Proposal safety exists; complete operator workflow is absent |
| H2.9 | Productionize fitment-constrained recommendations | partial | Rule-based recommendations exist; scale, ranking and quality gates do not |
| H2.10 | Build the grounded customer assistant | pending | No assistant service or storefront experience exists |

### Horizon 3 — Marketplace / v1.2 (`EP-22`–`EP-24`)

These are deliberately future-horizon items, not current Horizon 1 defects.

| ID | Task | Status |
|---|---|---|
| H3.1 | Supplier onboarding, verification and agreement acceptance | pending |
| H3.2 | Vendor catalog/order/customer isolation and upgrade path | pending |
| H3.3 | Vendor dashboards, inventory and performance analytics | pending |
| H3.4 | Flat, percentage, tiered and category-specific commissions | pending |
| H3.5 | Payout reconciliation and statements integrated with ERPNext | pending |
| H3.6 | Multi-vendor cart, split orders and split shipments | pending |
| H3.7 | Attributed, reviewable and revocable vendor fitment contributions | pending |

### Horizon 4 — Vertical portals / v1.3–v1.5 (`EP-25`–`EP-27`)

| ID | Task | Status |
|---|---|---|
| H4.1 | Workshop portal: jobs, labour, trade pricing and parts allocation | pending |
| H4.2 | Fleet portal: bulk vehicles, maintenance forecast, approvals and cost reporting | pending |
| H4.3 | Dealer portal: franchise catalogs, quotas, dealer pricing and warranty claims | pending |

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
| G4 | Search, import and fitment performance at reference scale | pending |
| G5 | SQL Server apply/upgrade/down rehearsal on a disposable clone | done |
| G6 | Complete browser accessibility, RTL and CWV evidence | pending |
| G7 | Product owner sign-off | blocked |
| G8 | Security sign-off | blocked |
| G9 | Private beta exit gate | pending |
| G10 | Public beta exit gate | pending |
| G11 | Commercial packaging and production licence authority | pending |
| G12 | nopCommerce Marketplace submission | pending |

## Immediate next actions

Work follows dependency order rather than skipping to later roadmap features:

1. Build the 40k-configuration reference environment (remaining H1.4), import production-scale catalog
   + provenanced fitment data (H1.11), then run the ~250k-product search/fitment latency and accuracy
   gates (remaining H1.10/H1.14).
2. Replace remaining ERP/licensing stubs, then run accessibility/RTL/CWV and security gates.
3. Complete Paymob/Bosta companion plugins and the v1.0 private/public beta gates.
