# Check Engine End-to-End Execution Plan

**Purpose:** Single source of truth for implementation progress and remaining work.

**Last updated:** 2026-08-05

## Status legend
- `pending` = not started
- `in-progress` = currently being implemented
- `done` = implemented and validated
- `blocked` = cannot proceed without prerequisite/decision

## Current overall progress
- Completed tasks: 11 / 52
- In-progress tasks: 2 / 52
- Pending tasks: 39 / 52
- Blocked tasks: 0 / 52

## Completed baseline (already done)
- `done` E2E.1: Permanent Playwright harness added (`src/Tests/TwinParticles.CheckEngine.Tests.E2E`)
- `done` E2E.2: PostgreSQL-backed install smoke flow implemented
- `done` E2E.3: Manual/automated Podman scripts implemented (`e2e/start-manual-stack.ps1`, `e2e/run-regressions.ps1`)

## Execution backlog (remaining to full system)

### Track 0 — Build and platform stability
| ID | Task | Status | Validation |
|---|---|---|---|
| T0.1 | Fix local plugin-host build blocker (`ClearPluginAssemblies` runtime/target mismatch) | done | `dotnet build src/Plugins/TwinParticles.CheckEngine/TwinParticles.CheckEngine.csproj` |
| T0.2 | Ensure plugin install/uninstall/update lifecycle builds cleanly in host | done | Build + install smoke |
| T0.3 | Add deterministic local build script for plugin + tests | pending | Script run on clean env |

### Track 1 — Domain and application completeness
| ID | Task | Status | Validation |
|---|---|---|---|
| T1.1 | Vehicle domain invariants (make/model/generation/config) complete | done | Unit + architecture tests |
| T1.2 | VIN decode pipeline + confidence model complete | done | Unit + corpus tests |
| T1.3 | OEM normalization/cross-ref/supersession complete | done | Unit + integration tests |
| T1.4 | Fitment evaluation/publish safety rules complete | done | `dotnet test src/Tests/TwinParticles.CheckEngine.Tests.Architecture/TwinParticles.CheckEngine.Tests.Architecture.csproj -c Release`; `dotnet build src/Plugins/TwinParticles.CheckEngine/TwinParticles.CheckEngine.csproj -c Release` |
| T1.5 | Garage domain behaviors (guest/auth merge, active vehicle) complete | done | `dotnet test src/Tests/TwinParticles.CheckEngine.Tests.Architecture/TwinParticles.CheckEngine.Tests.Architecture.csproj -c Release --filter GarageServiceTests`; `dotnet test src/Tests/TwinParticles.CheckEngine.Tests.Architecture/TwinParticles.CheckEngine.Tests.Architecture.csproj -c Release`; `dotnet build src/Plugins/TwinParticles.CheckEngine/TwinParticles.CheckEngine.csproj -c Release` |
| T1.6 | Import domain workflow and review queue contract complete | done | `dotnet test src/Tests/TwinParticles.CheckEngine.Tests.Architecture/TwinParticles.CheckEngine.Tests.Architecture.csproj -c Release --filter ImportReviewAndPublicationContractTests`; `dotnet test src/Tests/TwinParticles.CheckEngine.Tests.Architecture/TwinParticles.CheckEngine.Tests.Architecture.csproj -c Release --filter ImportPipelineReviewStatusTests`; `dotnet test src/Tests/TwinParticles.CheckEngine.Tests.Architecture/TwinParticles.CheckEngine.Tests.Architecture.csproj -c Release`; `dotnet build src/Plugins/TwinParticles.CheckEngine/TwinParticles.CheckEngine.csproj -c Release` |

### Track 2 — Infrastructure and persistence
| ID | Task | Status | Validation |
|---|---|---|---|
| T2.1 | Full SQL Server schema coverage against DB design doc | in-progress | Migration tests |
| T2.2 | Repository implementations replace temporary in-memory gaps where required | in-progress | Integration tests |
| T2.3 | Migration rollback/forward safety checks on populated dataset | pending | Migration safety suite |
| T2.4 | Index and query hot-path tuning aligned to NFR budgets | pending | Perf baselines |

### Track 3 — Storefront/search/admin features
| ID | Task | Status | Validation |
|---|---|---|---|
| T3.1 | Unified search modes (VIN/OEM/tree/category/keyword) end-to-end | pending | E2E + integration |
| T3.2 | PDP fitment panel states and behavior complete | pending | E2E + UI checks |
| T3.3 | Garage UI flows (add/switch/remove, persistence) complete | pending | E2E |
| T3.4 | Admin vehicle/OEM/fitment management workflows complete | pending | Integration + manual smoke |
| T3.5 | Import admin workflow (upload/review/publish) complete | pending | Integration + E2E |

### Track 4 — ERP, AI, and optional integrations
| ID | Task | Status | Validation |
|---|---|---|---|
| T4.1 | ERPNext sync contracts and conflict handling complete | pending | Contract + integration tests |
| T4.2 | AI provider abstraction finalized with safe defaults | pending | Unit + integration tests |
| T4.3 | AI review-gated content/fitment proposal workflow complete | pending | Integration tests |
| T4.4 | Recommendation and semantic search constrained by fitment | pending | Unit + E2E |

### Track 5 — Non-functional requirements implementation
| ID | Task | Status | Validation |
|---|---|---|---|
| T5.1 | Security controls (authz/input validation/rate limit/secret handling) complete | pending | Security test suite |
| T5.2 | Observability (logs, audit, health checks, diagnostics) complete | pending | Integration + ops checks |
| T5.3 | Accessibility automation (axe/keyboard/RTL) implemented | pending | E2E accessibility suite |
| T5.4 | Performance benchmark harness and budgets enforced in CI | pending | Perf pipelines |
| T5.5 | Resilience/chaos checks for restart/failover scenarios | pending | Chaos test run |

### Track 6 — Test architecture and quality gates
| ID | Task | Status | Validation |
|---|---|---|---|
| T6.1 | Unit test coverage thresholds met for domain/application | pending | Coverage report |
| T6.2 | Integration test suite covers migrations/repos/services | pending | Test run |
| T6.3 | Architecture rules fully enforced and green | done | `TwinParticles.CheckEngine.Tests.Architecture` |
| T6.4 | E2E suite expanded from smoke to acceptance-critical flows | pending | Playwright suite |
| T6.5 | CI gates align BR→FR→NFR→AC traceability checks | pending | CI pass |

### Track 7 — Documentation and release readiness
| ID | Task | Status | Validation |
|---|---|---|---|
| T7.1 | Keep BR/FR/NFR docs synchronized with implemented code | in-progress | Doc/code audit |
| T7.2 | Keep roadmap/architecture/deployment/testing docs synchronized | in-progress | Doc/code audit |
| T7.5 | Resolve plugin compile compatibility issues discovered during Track 0 (DTO mapping, customer guest checks, licensing DI) | done | Plugin build + architecture tests |
| T7.3 | Release runbook and operator manual finalized | pending | Dry-run rehearsal |
| T7.4 | Final acceptance checklist and go/no-go report | pending | Stakeholder signoff |

## Immediate next actions
1. Execute `T2.1` and `T2.2`: advance SQL schema coverage and replace in-memory repository gaps.
2. Validate plugin build and architecture/integration tests after each infrastructure change.
3. Update this plan after each milestone so it remains the implementation source of truth.
