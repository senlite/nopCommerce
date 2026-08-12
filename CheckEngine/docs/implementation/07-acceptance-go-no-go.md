# Horizon 1 Acceptance Checklist / Go-No-Go

**Date:** 2026-08-05 · **Release candidate:** Check Engine Track 0–7 completion

## Decisions locked for this gate

| Decision | Choice | Rationale |
|---|---|---|
| Primary OLTP for Check Engine schema | SQL Server (FluentMigrator + SCOPE_IDENTITY paths) | Matches production NFR and existing migrations |
| Local cloud-agent host DB for nopCommerce | MySQL 8 | Pinned `linq2db 4.3.0` + `Npgsql 8.0.4` breaks PostgreSQL install |
| AI default | Off / Null provider | FR-501 safe defaults |
| ERP default | Stub until BaseUrl configured | Fail-open checkout |
| Search storefront | SQL fitment/OEM maps + seeded fallback | Unblocks T3.1 without Elasticsearch |

## Must-pass checklist

| ID | Criterion | Evidence | Status |
|---|---|---|---|
| AC-015 / fitment | VIN/search paths never invent Fits | FitmentEvaluationService + publication policy tests | Pass (automated) |
| AC-027 / PDP | Fitment band fail-closed | Storefront JS + evaluate API | Pass (code) |
| AC-028 | Safety-critical publish hard-stop | FitmentPublicationPolicyService tests | Pass |
| AC-034 | Unified search mode routing | UnifiedSearchServiceTests | Pass |
| AC-041/042 | Garage active + guest migrate | GarageServiceTests | Pass |
| AC-060 | PDP fitment band | Theme widget + JS | Pass (code) |
| AC-070/074 | ERP order outbox non-blocking | ErpSyncService + stub/HTTP adapter tests | Pass (adapter) |
| AC-076 | Admin permission gates | Controllers use ManageCheckEngine | Pass |
| AC-080 | Security review baseline | Audit + sanitizer + rate limits + secret defaults | Pass (code) |
| AC-095/099 | AI never auto-publishes | AiProposalServiceTests | Pass |
| Build | Plugin + architecture tests green | `build-checkengine.sh` | Required before ship |
| Uninstall | Clean uninstall | Operator runbook dry-run | Required on SQL Server clone |
| Perf | NFR microbench gates | PerformanceBudgetTests | Pass (CI) |

## Go / No-Go

**Conditional GO** for engineering merge of Tracks 0–7 implementation on `dev`, subject to:

1. Architecture test suite green on CI
2. Operator migration dry-run on a disposable SQL Server clone (T7.3 live rehearsal) — **environment blocker in cloud agent** (no SQL Server instance); tracked as release prerequisite, not a code gap
3. Stakeholder sign-off on Horizon 1 commercial packaging (licensing keys) — **external blocker**

**No-Go items that would stop production cutover:** open high/critical security findings; fitment auto-publish regression; ERP blocking checkout.

## Sign-off

| Role | Name | Date | Decision |
|---|---|---|---|
| Engineering | Cloud agent completion run | 2026-08-05 | Conditional GO (code complete) |
| Product owner | _pending_ | | |
| Security | _pending_ | | |
