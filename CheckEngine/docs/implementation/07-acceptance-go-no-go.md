# Horizon 1 Acceptance Checklist / Go-No-Go

**Date:** 2026-08-25 · **Release candidate:** Check Engine plugin `0.104.0` (pre-release; Horizon 1 not gated)

**Engineering status:** Engineering evidence for G1–G6 is in tree. G11 packing is unsigned-only.
H1.35 / G8, G7, G11 vendor signing, and G12 remain external. See
[EXECUTION-PLAN.md](../../EXECUTION-PLAN.md).

## Decisions locked for this gate

| Decision | Choice | Rationale |
|---|---|---|
| Primary OLTP for Check Engine schema | SQL Server (FluentMigrator + SCOPE_IDENTITY paths) | Matches production NFR and existing migrations |
| Validated lifecycle DB | SQL Server 2022 | Production provider; install/update/uninstall and backup/restore rehearsed |
| AI default | Off / Null provider | FR-501 safe defaults |
| ERP default | Stub until BaseUrl configured | Fail-open checkout |
| Search storefront | SQL fitment/OEM maps + seeded fallback | Unblocks T3.1 without Elasticsearch |
| VIN beyond WMI | Pluggable per manufacturer (`FR-204`); BMW exemplar + H1.6a catalog JSON | Fail-closed; do not fabricate VDS |

## Must-pass checklist

| ID | Criterion | Evidence | Status |
|---|---|---|---|
| AC-015 / fitment | VIN/search paths never invent Fits | FitmentEvaluationService + publication policy tests | Pass (automated) |
| AC-027 / PDP | Fitment band fail-closed | Storefront JS + evaluate API | Pass (code) |
| AC-028 | Safety-critical publish hard-stop | FitmentPublicationPolicyService tests | Pass |
| AC-034 | Unified search mode routing | UnifiedSearchServiceTests | Pass |
| AC-041/042 | Garage active + guest migrate | GarageServiceTests | Pass |
| AC-060 | PDP fitment band | Theme widget + JS | Pass (code) |
| AC-070/074 | ERP order outbox non-blocking | ErpSyncService + stub/HTTP adapter tests | Pass (adapter); live partner reconciliation pending |
| AC-076 | Admin permission gates | Controllers use ManageCheckEngine | Pass |
| AC-080 | Security **engineering** controls | Audit + sanitizer + rate limits + secret defaults | Pass (code) |
| AC-080.1 / H1.35 / G8 | Independent security assessment, no open high/critical | External reviewer sign-off in this table's Security row | **Pending** (cannot be closed by engineering) |
| AC-095/099 | AI never auto-publishes | AiProposalServiceTests | Pass |
| Build | Plugin + architecture tests green | 1061 architecture tests (excl. `VectorMathTests`); Domain ≥ 80% / Application ≥ 70% | Pass |
| Uninstall | Clean uninstall | SQL Server 2022 live rehearsal + &lt;24h export guard | Pass: zero tables, migration rows and permissions |
| Rollback | Populated backup/restore | Checksum backup + byte-identical pre/post metrics | Pass |
| Perf CI | NFR microbench gates | PerformanceBudgetTests | Pass (CI) |
| G4 / NFR-017 | 2,000 unique shoppers, search p95 within `NFR-001` on this node at 25 in-flight | `SearchConcurrentSessionBudgetTests` + `run-search-load-gate.sh` | Pass (rehearsal; 4-node Redis herd not this host) |
| G6 / NFR-046 | axe serious/critical on Check Engine widgets | `run-a11y-gate.sh` + Playwright specs | Pass |
| G6 / NFR-002 | Search first-page LCP ≤ 1.5 s | Host logo/CSS bound LCP 2.47–3.32 s | **Not met** (host-limited; do not claim) |
| G11 pack | Drop-in zip excluding host binaries | `pack-checkengine.sh` | Partial (unsigned; vendor signing external) |

## Go / No-Go

**Conditional GO** for engineering merge of Tracks 0–7 plus H1.6a / G4 / G6 / unsigned G11 packing,
subject to:

1. Architecture test suite green on CI
2. Stakeholder sign-off on Horizon 1 commercial packaging (licensing keys) — **external blocker**
3. Independent security assessment (H1.35 / G8 / `AC-080.1`) — **external blocker**
4. Completion of the remaining Horizon 1 release gates in
   [EXECUTION-PLAN.md](../../EXECUTION-PLAN.md)

**No-Go items that would stop production cutover:** open high/critical security findings; fitment
auto-publish regression; ERP blocking checkout; claiming `NFR-002` or G11 **done** without the
evidence above.

## Sign-off

| Role | Name | Date | Decision |
|---|---|---|---|
| Engineering | Cloud agent Horizon 1 engineering evidence | 2026-08-25 | Engineering GO for 0.104.0; Horizon 1 remains conditional |
| Product owner | _pending_ | | G7 |
| Security | _pending_ | | H1.35 / G8 / AC-080.1 |
