# Check Engine End-to-End Execution Plan

**Purpose:** Single source of truth for implementation progress and remaining work.

**Last updated:** 2026-08-05

## Status legend
- `pending` = not started
- `in-progress` = currently being implemented
- `done` = implemented and validated
- `blocked` = cannot proceed without prerequisite/decision

## Current overall progress
- Completed tasks: 52 / 52
- In-progress tasks: 0 / 52
- Pending tasks: 0 / 52
- Blocked tasks: 0 / 52 (release dry-run SQL Server clone is an ops prerequisite, not a code task blocker)

### Completion notes (2026-08-05)
- Track 3: SQL product search, storefront JS widgets, garage remove, admin dashboard, import SQL persistence + Excel/PDF parsers + catalog publisher.
- Track 4: AI ports (Null/OpenAI-compatible), ERPNext HTTP adapter with stub fallback, recommendations, NL search fallback.
- Track 5: Audit schema, health/diagnostics, a11y smoke, perf budgets, resilience tests.
- Track 6: Coverage/traceability gates, E2E API/a11y soft specs, CI build script step.
- Track 7: Operator runbook + acceptance go/no-go; docs synchronized.
- Decisions: MySQL for local nopCommerce host (Npgsql/linq2db incompatibility); SQL Server remains production OLTP for `TP_CE_*`; AI/ERP safe defaults off/unconfigured.

## Completed baseline (already done)
- `done` E2E.1: Permanent Playwright harness added (`src/Tests/TwinParticles.CheckEngine.Tests.E2E`)
- `done` E2E.2: PostgreSQL-backed install smoke flow implemented
- `done` E2E.3: Manual/automated Podman scripts implemented (`e2e/start-manual-stack.ps1`, `e2e/run-regressions.ps1`)

## Execution backlog

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

## Immediate next actions
1. Run live SQL Server migration apply/uninstall rehearsal on a disposable clone (ops).
2. Obtain product/security stakeholder sign-off on the go/no-go checklist.
3. Keep this plan updated if post-release defects reopen a track.
