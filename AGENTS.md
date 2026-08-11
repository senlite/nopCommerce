# AGENTS.md

## Cursor Cloud specific instructions

This repo is **nopCommerce 4.70** (ASP.NET Core storefront + admin, plus the in-development
`TwinParticles.CheckEngine` plugin). The update script keeps NuGet packages restored; the notes
below cover the non-obvious things needed to build, test, and run.

### Toolchain: SDK is .NET 9, but the app runs on .NET 8
- `global.json` pins the **.NET SDK to 9.0.316**, but every project targets `net8.0`.
- Building works with the .NET 9 SDK, but **running** any project (the web app, and the
  `ClearPluginAssemblies` post-build helper) requires the **.NET 8 ASP.NET Core runtime** to be
  installed. Both the .NET 8 runtime and the 9.0.316 SDK are installed on the VM (`dotnet` is on PATH).

### Do NOT build/restore the full solution — it has a broken reference
- `src/NopCommerce.sln` still references `Nop.Plugin.Misc.Sendinblue`, whose `.csproj` was removed
  (renamed to Brevo). `dotnet restore/build src/NopCommerce.sln` therefore **fails**
  ("project file ... Nop.Plugin.Misc.Sendinblue.csproj was not found").
- Build/test/run the specific projects instead (this is also what CI and the `e2e/` harness do):
  - Web app: `dotnet build src/Presentation/Nop.Web/Nop.Web.csproj -c Debug`
  - Architecture tests (CI gate): `dotnet test src/Tests/TwinParticles.CheckEngine.Tests.Architecture/TwinParticles.CheckEngine.Tests.Architecture.csproj -c Release`
  - Unit tests: `dotnet test src/Tests/Nop.Tests/Nop.Tests.csproj`
- There is **no dedicated linter**; code quality is enforced by `dotnet build` + `.editorconfig`/Roslyn analyzers.

### Database: use MySQL — PostgreSQL is broken with the pinned dependencies
- The PostgreSQL provider is **unusable** with the pinned `linq2db 4.3.0` + `Npgsql 8.0.4`. Install
  fails at the first DB write with `System.ArgumentNullException: Value cannot be null. (Parameter 'constructor')`
  inside `LinqToDB.DataProvider.PostgreSQL.NpgsqlProviderAdapter.GetInstance()` (linq2db 4.3.0 predates
  Npgsql 8 support). This is a dependency-version bug, not an environment issue. Ignore the `e2e/`
  Postgres/Podman scripts for local runs.
- **MySQL 8 works** and is a documented provider (`mysql-docker-compose.yml`). MySQL 8 is installed on
  the VM but is **not auto-started** — start it and (re)create the app user each session:
  ```bash
  sudo mkdir -p /var/run/mysqld && sudo chown mysql:mysql /var/run/mysqld
  sudo mysqld_safe >/tmp/mysqld.log 2>&1 &   # wait ~8s
  sudo mysql -e "CREATE USER IF NOT EXISTS 'nopcommerce'@'%' IDENTIFIED WITH mysql_native_password BY 'nopCommerce_db_password123!'; \
                 GRANT ALL PRIVILEGES ON *.* TO 'nopcommerce'@'%' WITH GRANT OPTION; FLUSH PRIVILEGES;"
  ```

### Running the app + completing the first-run install wizard
1. Run: `dotnet run --project src/Presentation/Nop.Web/Nop.Web.csproj --urls http://0.0.0.0:5000`
   (add `--no-build` if already built). First run 302-redirects to `/install`.
2. Install fields that work (data provider `2` = MySQL). Let the wizard create the DB
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
- `Nop.Tests` uses SQLite in-memory (no DB/web server needed). A few known failures are NOT caused by
  the environment: `TestDocsLinks` / `TestOfficialSiteLinks` require outbound internet (return 403 in
  the sandbox), and a couple of `ExportManagerTests` Xlsx tests are order-dependent/flaky. ~825+/837 pass.
- Architecture tests are hermetic and should be fully green (192 pass).

### Frontend assets (optional)
- Prebuilt assets ship in `wwwroot`; Node is not required to run. To rebuild them:
  `cd src/Presentation/Nop.Web && npm install && npx gulp`.
