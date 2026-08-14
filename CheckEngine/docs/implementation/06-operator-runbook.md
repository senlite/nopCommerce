# Check Engine Operator Runbook

> How to install, operate, and recover Check Engine on a nopCommerce host.

**Status:** Approved · **Owner:** platform · **Last revised:** 2026-08-05

## Executive Summary

This runbook covers plugin install/uninstall, index rebuild, import operations, ERP sync, health probes, and SQL migration forward/rollback expectations for release dry-runs.

## Install

1. Build the plugin: `dotnet build src/Plugins/TwinParticles.CheckEngine/TwinParticles.CheckEngine.csproj -c Release`
2. Confirm output under `src/Presentation/Nop.Web/Plugins/TwinParticles.CheckEngine/`
3. Start the host, open **Admin → Configuration → Local plugins**, install **TwinParticles.CheckEngine**
4. Open **Admin/CheckEngine/Configure**, enable the plugin
5. Open **Admin/CheckEngine/Dashboard** for operational entry points

## Uninstall

1. Export any custom vehicle/OEM/fitment data before uninstall (SQL backup of `TP_CE_*` tables)
2. Uninstall from Local plugins
3. Confirm `TP_CE_*` tables are dropped by reverse migrations (AutoReversingMigration). If residual tables remain, drop `TP_CE_%` explicitly after backup

## Search index

- Admin action: `POST Admin/CheckEngine/SearchAdmin/Rebuild`
- Public search: `POST /check-engine/search/query`
- When the external index is unhealthy, unified search sets `isDegraded: true` and falls back to SQL/keyword paths

## Import workflow

1. Upload CSV/Excel/PDF via Import Admin `Run` (Dashboard helper or JSON API)
2. Review pending rows (`SetReviewStatus` Approve/Reject)
3. `Publish` with `dryRun=true` first, then `dryRun=false`
4. Approved rows create/update host products through `NopImportProductPublisher` and write `TP_CE_ProductOemMap`
5. Batches are session-cached by Guid and best-effort persisted to `TP_CE_ImportBatch` / `TP_CE_ImportRow` with `CorrelationId`

## ERPNext

- Configure BaseUrl + API key/secret in host settings / user secrets (never commit)
- Empty BaseUrl keeps the stub adapter (safe default)
- Admin: `ErpAdmin/Queue`, `Process`, `Reconcile`, `InventorySnapshot`
- Checkout must not block on ERP; outbox drains via scheduled processing

## AI features

- All AI feature toggles default **off**
- Without API key, `OpenAiCompatibleCompletionPort` no-ops like `NullAiCompletionPort`
- AI proposals are never auto-published (`AiProposalService` always `IsPublished=false`)

## Health and diagnostics

- Public: `GET /check-engine/health`
- Admin diagnostics package: `Admin/CheckEngine/DiagnosticsAdmin` (secrets redacted)
- Audit trail: `TP_CE_AuditEvent` (fitment review + import approve/reject/publish)

## Migration dry-run (T7.3)

On a populated SQL Server copy:

```powershell
# Forward
dotnet build src/Plugins/TwinParticles.CheckEngine/TwinParticles.CheckEngine.csproj -c Release
# Install/restart host to apply FluentMigrator migrations

# Rollback rehearsal: uninstall plugin (AutoReversingMigration Down) on a disposable clone
# Validate TP_CE_* removal and host storefront still serves catalog pages
```

Record measured duration and any irreversible steps in the release notes.

## Local quality gate

```bash
bash CheckEngine/scripts/build-checkengine.sh
# or
pwsh CheckEngine/scripts/build-checkengine.ps1
```
