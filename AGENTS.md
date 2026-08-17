# AGENTS.md

## Cursor Cloud specific instructions

This repo is **nopCommerce 4.90.6 on .NET 9** (ASP.NET Core storefront/admin plus Check Engine and
GMaster). Standard commands are in the root solution and `CheckEngine/scripts`; the notes below cover
only cloud-specific caveats.

### Never modify host application code
- **Keep all changes inside plugins (`src/Plugins/**`) or themes.** Do not edit or add code in the
  nopCommerce host (`src/Libraries/**`, `src/Presentation/Nop.Web/**`,
  `src/Presentation/Nop.Web.Framework/**`, host `src/Tests/Nop.Tests/**`). This keeps future
  nopCommerce upgrades a clean drop-in.
- If something appears to need a host change, solve it from a plugin instead: register services,
  widgets, routes, view components, event consumers, or plugin-local views/overrides. If a host edit
  seems unavoidable, stop and confirm rather than editing host code.

### Build and test
- The full solution is valid: `dotnet build src/NopCommerce.sln -c Release`.
- Check Engine gate:
  `dotnet test src/Tests/TwinParticles.CheckEngine.Tests.Architecture/TwinParticles.CheckEngine.Tests.Architecture.csproj -c Release`.
- Host regression: `dotnet test src/Tests/Nop.Tests/Nop.Tests.csproj -c Release`.
- There is no separate linter; build plus `.editorconfig`/Roslyn analyzers is the lint gate.
- A full host baseline produced 1,044 passes and 8 intentional skips. The checkout-model fixture can
  inherit polluted shared settings under full-suite parallelism (`CanPreparePaymentMethodModel` and
  `PreparePaymentMethodModelShouldDependOnSettings`); all 12 fixture tests pass together in isolation.

### Databases
- **SQL Server is the validated Check Engine lifecycle provider.** A disposable SQL Server 2022 run
  proved fresh install, plugin update, clean uninstall and populated backup/restore.
- MySQL 8 remains convenient for ordinary storefront development (`mysql-docker-compose.yml`). On VMs
  where the preinstalled service is stopped, start it and recreate the local development user:
  ```bash
  sudo mkdir -p /var/run/mysqld && sudo chown mysql:mysql /var/run/mysqld
  sudo mysqld_safe >/tmp/mysqld.log 2>&1 &   # wait ~8s
  sudo mysql -e "CREATE USER IF NOT EXISTS 'nopcommerce'@'%' IDENTIFIED WITH mysql_native_password BY 'nopCommerce_db_password123!'; \
                 GRANT ALL PRIVILEGES ON *.* TO 'nopcommerce'@'%' WITH GRANT OPTION; FLUSH PRIVILEGES;"
  ```

### Running the app + completing the first-run install wizard
1. Run: `dotnet run --project src/Presentation/Nop.Web/Nop.Web.csproj --urls http://0.0.0.0:5000`
   (add `--no-build` if already built). First run 302-redirects to `/install`.
2. For the lightweight MySQL path, data provider `2` works. Let the wizard create the DB
   (`CreateDatabaseIfNotExists=true`); the connection string **must include `AllowUserVariables=True`**:
   - `DataProvider=2`, `ConnectionStringRaw=true`
   - `ConnectionString=Server=127.0.0.1;Port=3306;Database=nopcommerce;Uid=nopcommerce;Pwd=nopCommerce_db_password123!;AllowUserVariables=True;AllowPublicKeyRetrieval=True`
   - `AdminEmail=admin@yourStore.com`, `AdminPassword=Admin123$`, `InstallSampleData=true`
3. **After install, restart the `dotnet run` process.** nopCommerce reads its data config only at
   startup, so the still-running process keeps redirecting to `/install` until restarted. The
   post-install runtime config is written to `src/Presentation/Nop.Web/App_Data/appsettings.json`
   (git-ignored), so re-running install requires dropping that DB / clearing that connection string.
- Default admin login: `admin@yourStore.com` / `Admin123$`. Storefront + admin share port 5000
  (`/` storefront, `/admin` back office).

### Test notes (flaky/expected failures)
- `Nop.Tests` uses SQLite in-memory and needs no web server or external database.
- Architecture tests are hermetic and should be fully green (212 pass).
- GMaster tests should be fully green (19 pass).
- Check Engine health is intentionally `degraded` when its commercial licence is inactive even when
  database, search-index and ERP probes all report `ok`.
- The fitment accuracy corpus lives in `src/Tests/corpus/fitment`. Regenerate it with
  `python3 build_corpus.py` after changing evaluation semantics; the runner is part of the Check
  Engine suite and its Must set is a release gate.

### Storefront gotchas
- Publishing `Nop.Web` does **not** rebuild the plugins. Build the plugin project first, otherwise the
  running site silently keeps the previous plugin assembly:
  `dotnet build src/Plugins/TwinParticles.CheckEngine/TwinParticles.CheckEngine.csproj -c Release`
- Check Engine renders through widget zones, so uninstalling it removes it from
  `widgetsettings.activewidgetsystemnames`. Restoring a database backup does not always restore that
  entry; if the storefront chrome is missing entirely, re-add the plugin to that setting.
- New locale resources reach existing stores through `UpdateAsync`, which runs when `plugin.json`'s
  version changes. Adding a string without bumping the version leaves upgraded stores rendering raw
  resource keys.
- Uninstall is intentionally blocked until `/Admin/CheckEngine/UninstallAdmin/Export` has produced a
  fresh (<24h) vehicle/OEM/fitment JSON export. Never bypass or move this guard after destructive
  permission/settings/locale/migration-down operations.
- Check Engine migration timestamps must be at or before the current UTC time when testing an update;
  nopCommerce excludes future-dated migrations. Bump `plugin.json` when shipping a new migration.
- Guest garage data intentionally stays in browser `localStorage` until sign-in. The authenticated
  migration request carries the payload inline (not only a process-local key), so migration survives
  app restarts and multi-node routing. Do not reintroduce anonymous guest-key read/write endpoints.
- Signed-in garage VINs are plaintext only in domain/application memory and subject export; the SQL
  repository must protect them through `IGarageVinProtector` (`enc:v1:`). Never log/copy VINs into
  audit JSON. Garage erasure is atomic and also runs on `CustomerPermanentlyDeleted`.
- VIN consumers must honor `NeedsDisambiguation`; never bind `Candidates[0]` unless the decode outcome
  is a true single match or the customer supplied an explicit configuration selection.
- Check Engine keyword/category search delegates to nopCommerce's `IProductService`, while OEM and
  vehicle-tree candidate IDs come from Check Engine SQL and are hydrated through the real catalog.
  Production search must never fall back to invented demo product IDs.
- Search analytics may receive normalized text only to compute a deployment-keyed HMAC fingerprint.
  Never add raw query/VIN/OEM, customer id, or IP columns/logs. Click-through is anonymous by analytics
  event id; retention must remain bounded and explicitly pruneable.
- Public SEO landings use `/vehicles/config-{id}` and `/parts/product-{id}/for/config-{id}` with
  `/ar/` counterparts. They contribute SQL-backed URLs through nopCommerce's `SitemapCreatedEvent`;
  do not add a second sitemap XML endpoint or restore the old process-local sitemap service.
- SEO indexability is fitment-gated: a page with no active published Fits claim remains public but
  emits `noindex, follow` and is excluded from `/sitemap.xml`.
- Import batches are SQL-authoritative, not process-local. The source bytes and full row state are
  intentionally persisted so `/ImportAdmin/Batch`, review, publish and `RerunStage` survive restarts.
  Do not restore best-effort persistence or a singleton batch dictionary.
- The bundled BMW seed is an incremental reference-data upgrade. Re-running `VehicleAdmin/Seed` must
  add only missing natural keys/aliases, preserve unrelated makes and operator edits, and never
  delete legacy configurations. Fitment authority remains in separate provenanced claims.
- Vehicle model merges reparent generations but never rewrite generation/configuration ids; those ids
  are referenced by fitment, garage and SEO history. Keep merge SQL atomic and conflict-checked,
  archive sources softly, move scoped aliases, invalidate alias caches, and append attributed audit.

### Frontend assets (optional)
- Prebuilt assets ship in `wwwroot`; Node is not required to run. To rebuild them:
  `cd src/Presentation/Nop.Web && npm install && npx gulp`.

### OneDrive artifact uploads (optional)
- After saving walkthrough recordings or screenshots with `RecordScreen` (mode `SAVE_RECORDING`), upload them to OneDrive when the environment secret `RCLONE_CONFIG_B64` is configured:
  `bash scripts/upload-artifacts-to-onedrive.sh`
- One-time setup (OAuth on your PC, base64-encode `rclone.conf`, add secret `RCLONE_CONFIG_B64` in Cursor → Cloud Agents → Environments) is documented in `docs/cloud-agent-onedrive-setup.md`. A newly added secret is only injected into **new** agent runs.
- Uploads go to `CheckEngine/AgentArtifacts/YYYY-MM-DD/{conversation-id}/` and the script prints share links via `rclone link`.
