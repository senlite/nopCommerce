# AGENTS.md

## Cursor Cloud specific instructions

This repo is **nopCommerce 4.90.6 on .NET 9** (ASP.NET Core storefront/admin plus Check Engine and
GMaster). Standard commands are in the root solution and `CheckEngine/scripts`; the notes below cover
only cloud-specific caveats.

### Build and test
- The full solution is valid: `dotnet build src/NopCommerce.sln -c Release`.
- Check Engine gate:
  `dotnet test src/Tests/TwinParticles.CheckEngine.Tests.Architecture/TwinParticles.CheckEngine.Tests.Architecture.csproj -c Release`.
- Host regression: `dotnet test src/Tests/Nop.Tests/Nop.Tests.csproj -c Release`.
- There is no separate linter; build plus `.editorconfig`/Roslyn analyzers is the lint gate.
- A full host run produced 1,044 passes and 8 intentional skips. `CanPreparePaymentMethodModel` failed
  once under full-suite parallelism but passed in isolation and on the complete rerun.

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
- Guest garage data intentionally stays in browser `localStorage` until sign-in. The authenticated
  migration request carries the payload inline (not only a process-local key), so migration survives
  app restarts and multi-node routing. Do not reintroduce anonymous guest-key read/write endpoints.
- Check Engine keyword/category search delegates to nopCommerce's `IProductService`, while OEM and
  vehicle-tree candidate IDs come from Check Engine SQL and are hydrated through the real catalog.
  Production search must never fall back to invented demo product IDs.

### Frontend assets (optional)
- Prebuilt assets ship in `wwwroot`; Node is not required to run. To rebuild them:
  `cd src/Presentation/Nop.Web && npm install && npx gulp`.
