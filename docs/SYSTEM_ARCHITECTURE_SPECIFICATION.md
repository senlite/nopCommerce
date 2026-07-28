# System Architecture Specification Document

**nopCommerce** — factual specification of what the system is and how it works. For use by a new engineering team joining the company.

---

## 1. SYSTEM PURPOSE

**Business problem:** The software provides an e-commerce platform for running online stores. It addresses catalog management, order processing, payments, shipping, multi-store support, and store administration.

**Primary user roles:**
- **Store customers** — browse catalog, manage account, cart, checkout, orders, and returns.
- **Store administrators** — manage products, orders, customers, settings, plugins, and content via the admin area.
- **Vendors** (when enabled) — manage their own products and orders within the store.

**Core use cases supported:** Product catalog and search; shopping cart and checkout; order placement and processing; customer registration and authentication; multi-store and multi-language; discounts and promotions; shipping and tax calculation; payment processing (via plugins); content (topics, news, blog, forums); reporting and export; plugin and theme extensibility; scheduled background tasks (e.g. exchange rates, cleanup); installation and upgrade of the application and database.

---

## 2. APPLICATION TYPE

**Classification: Modular monolith with plugin-based extensibility.**

Evidence:
- **Single deployable:** One web application (`Nop.Web`) hosts both the public storefront and the admin area. There is no separate API or worker process in the core solution; background work runs inside the same process via the task scheduler.
- **Layered structure:** Clear separation into `Libraries` (Nop.Core, Nop.Data, Nop.Services) and `Presentation` (Nop.Web, Nop.Web.Framework). Dependencies flow inward: Web → Web.Framework → Services → Data → Core.
- **Modular domains:** `Nop.Core.Domain` is split by domain folders (Catalog, Customers, Orders, Shipping, Discounts, etc.). `Nop.Services` mirrors these with service interfaces and implementations per domain. Logic is organized by feature, not by a single “application” blob.
- **Plugin-based:** The `Plugins` folder contains many optional assemblies (payments, shipping, tax, widgets, external auth, etc.) that are loaded at startup via `ApplicationPartManager` and `InitializePlugins`. Plugins implement `IPlugin`, register routes via `IRouteProvider`, and can register their own `INopStartup` for DI and middleware. The core does not depend on specific plugins; plugins depend on core and framework.
- **No microservices:** No separate services, message queues, or service discovery in the core. Background tasks are triggered by in-process timers that call a special URL on the same app (`scheduletask/runtask`).

Hence: **modular monolith** (layered + domain-oriented modules in one process) **with plugin-based** extensibility.

---

## 3. TECHNICAL STACK

**Languages:** C#. The solution is .NET 9 (SDK and runtime referenced in projects and Dockerfile). No other primary languages in the main application.

**Frameworks:** ASP.NET Core for the web host (MVC, Razor, middleware, dependency injection). The UI is server-rendered with Razor views (.cshtml); no SPA framework in the core. FluentValidation is used for model validation; AutoMapper for object mapping.

**Databases:** Relational; multiple providers supported via a single abstraction. Supported providers: SQL Server (Microsoft.Data.SqlClient), MySQL (MySqlConnector), PostgreSQL (Npgsql). The active provider and connection string are configured in application settings (e.g. `ConnectionStrings` in `App_Data/appsettings.json` or equivalent). Schema and data changes are applied via FluentMigrator migrations.

**ORMs / Data access:** LinqToDB is used as the main data access layer (not Entity Framework). Repositories are generic: `IRepository<TEntity>` implemented by `EntityRepository<TEntity>`. Entities map to tables via FluentMigrator and Nop.Data mapping; LinqToDB is used for queries and commands. The project references `linq2db` and provider-specific packages (SqlClient, MySqlConnector, Npgsql).

**Authentication:** Cookie-based authentication is the default. The scheme is `NopAuthenticationDefaults.AuthenticationScheme`; `AddCookie` is used with a prefixed cookie name, HttpOnly, and `SecurePolicy.SameAsRequest`. External authentication (e.g. Facebook) and multi-factor authentication (e.g. Google Authenticator) are implemented via plugins. Data protection (e.g. for tokens) can use local storage or Azure (Azure.Extensions.AspNetCore.DataProtection.Keys/Blobs).

**Caching:** In-memory and distributed options. A `IStaticCacheManager` / `ICacheKeyService` abstraction supports: in-memory (default when distributed cache is off), Redis, SQL Server distributed cache, and “RedisSynchronizedMemory” (Redis-backed synchronized in-memory for web farms). Configuration is in `DistributedCacheConfig` (type, connection string, instance name). Short-term per-request cache is provided by `PerRequestCacheManager`. The repository layer supports cache keys for `GetByIdAsync`/`GetAllAsync`. Cache invalidation is done via event consumers (e.g. `*CacheEventConsumer` in Services).

**Background job framework:** No separate job queue (e.g. Hangfire). The application uses an in-process **task scheduler** (`Nop.Services.ScheduleTasks`). Scheduled tasks are defined by type name in the database (`ScheduleTask` entity); the scheduler runs a timer per task and triggers execution by sending an HTTP request to the same application at `{storeUrl}/scheduletask/runtask`. The endpoint invokes `IScheduleTaskRunner`, which resolves the task type from DI and executes it. Tasks implement `IScheduleTask`. Examples: exchange rate update, clearing logs, sending queued emails.

**External services and integrations:** Used mainly through plugins (e.g. PayPal, Amazon Pay, Avalara, UPS, Brevo, Omnisend, Power BI, Zettle). Core services use: MailKit for SMTP, MaxMind GeoIP2 for geo lookup, Azure.Storage.Blobs for blob storage (optional), and optional Azure Data Protection. WCF (System.ServiceModel.Http) is referenced for a connected service.

**Messaging:** No message broker in the core. In-process **event publishing** is used: `IEventPublisher` with `Publish`/`PublishAsync`. Subscribers implement `IConsumer<TEvent>` and are registered in DI; the engine discovers them via `ITypeFinder` and registers as `IConsumer<>`. This is request-scoped or background execution within the same process.

**Build tools:** MSBuild (.NET SDK). Solution file: `src/NopCommerce.sln`. Build is invoked with `dotnet build` (and `dotnet publish` for deployment). A post-build step runs `ClearPluginAssemblies` to trim plugin output folders. `Directory.Build.props` and `global.json` exist at the repo root.

**CI/CD:** GitHub Actions workflow in `.github/workflows/dotnet.yml`. On push/PR to `develop` it runs: checkout, setup .NET (8.0.x in the workflow), `dotnet restore`, `dotnet build`, `dotnet test`. No container build or deploy in the provided workflow.

**Containerization:** Docker support at repo root. `Dockerfile` uses multi-stage build: build stage with `mcr.microsoft.com/dotnet/sdk:9.0-alpine`, then runtime stage with `mcr.microsoft.com/dotnet/aspnet:9.0-alpine`. The web app is published from `Presentation/Nop.Web`. `entrypoint.sh` runs `dotnet Nop.Web.dll`. `docker-compose.yml` defines a web service and a SQL Server 2019 database service; `mysql-docker-compose.yml` and `postgresql-docker-compose.yml` provide MySQL and PostgreSQL alternatives.

---

## 4. RUNTIME COMPONENTS

**APIs:** The main application is an ASP.NET Core web app. It exposes:
- **MVC controllers** for the public store (e.g. `CatalogController`, `CheckoutController`, `OrderController`, `CustomerController`) and for the admin area under `Areas/Admin`.
- **REST API:** Provided by the plugin `Nop.Plugin.Misc.WebApi.Frontend` (not part of core). When enabled, it exposes API endpoints.
- **Schedule task endpoint:** A single special route `scheduletask/runtask` is used to trigger scheduled tasks via HTTP (called by the in-process task scheduler).

**UI / Frontend:** Server-rendered Razor views. Themes live under `Nop.Web/Themes` (and can be under `wwwroot`); the active theme is selected via `IThemeContext`. Views are compiled at build/publish. Static files are served from `wwwroot`, `Themes`, and `Plugins`. WebOptimizer is used for bundling/minification of CSS and JavaScript when enabled. No separate SPA or mobile app in the repo; the NopMobileApp plugin provides integration points for mobile clients.

**Workers / Schedulers / Batch processes:** There are no standalone worker processes. The **task scheduler** runs inside the web process. On startup, after the database is confirmed installed, `ITaskScheduler.InitializeAsync()` and `StartSchedulerAsync()` run. The scheduler maintains a list of `TaskThread` instances (one per scheduled task). Each thread uses a timer to periodically send an HTTP request to `{storeUrl}/scheduletask/runtask` with the task type; the request is handled by `ScheduleTaskController`, which runs the task via `IScheduleTaskRunner`. Long-running or batch work is therefore executed in the context of HTTP requests triggered by the same application.

**Daemons:** None. All runtime behavior is contained in the single web process (and optional Docker sidecar database).

**Communication between components:** Controllers call application and domain services (e.g. `IOrderProcessingService`, `IProductService`); services use `IRepository<T>`, `IEventPublisher`, and other services. Plugins are loaded into the same app domain and use the same DI container and pipeline. The task scheduler communicates with the app only via HTTP to the schedule task URL.

---

## 5. ENTRY POINTS

**Main application entry:** `src/Presentation/Nop.Web/Program.cs`. `Main` creates a `WebApplicationBuilder`, adds configuration (see below), optionally uses Autofac as the DI container (`UseAutofac` from `CommonConfig`), calls `ConfigureApplicationServices(builder)`, builds the app, calls `ConfigureRequestPipeline()`, then `StartEngineAsync()`, and finally `RunAsync()`.

**Startup configuration:** Configuration is loaded in this order: default `appsettings` path (`App_Data/appsettings.json`), environment-specific file (`App_Data/appsettings.{Environment}.json`), and environment variables. `ConfigureApplicationSettings` binds config sections to types implementing `IConfig` (discovered via `ITypeFinder`) and stores them in `Singleton<AppSettings>.Instance`. Service configuration is delegated to the **NopEngine**: the engine discovers all types implementing `INopStartup`, instantiates them, sorts by `Order`, and calls `ConfigureServices` on each. Key startup classes include: `NopDbStartup` (data, migrations, repositories), `NopStartup` in Web.Framework (most services, caching, events, plugins, task scheduler), `NopCommonStartup` (session, themes, routing), `ErrorHandlerStartup`, `NopStaticFilesStartup`, `NopProxyStartup`, `NopWebMarkupMinStartup`, `NopRoutingStartup`, `AuthenticationStartup`, `AuthorizationStartup`, `NopMvcStartup`, `NopEndpoints`, and plugin-specific startups.

**Dependency injection:** Either the built-in container or Autofac (`AutofacServiceProviderFactory`). Registrations come from each `INopStartup.ConfigureServices`. The engine is registered as `IEngine` singleton; it also runs `IStartupTask` implementations and registers AutoMapper profiles (from `IOrderedMapperProfile`). Event consumers are registered as `IConsumer<TEvent>`. Plugins are loaded into `ApplicationPartManager` during `ConfigureApplicationServices` via `InitializePlugins(pluginConfig)` so that their controllers and views are discovered.

**Routing:** Endpoints are registered in `UseNopEndpoints`: the app calls `IRoutePublisher.RegisterRoutes(endpoints)`. `RoutePublisher` discovers all `IRouteProvider` implementations (via `ITypeFinder`), creates instances, orders by priority, and invokes `RegisterRoutes` on each. Thus both core and plugins contribute routes (e.g. `RouteProvider` and `GenericUrlRouteProvider` in Nop.Web, plus plugin route providers).

**Middleware / pipeline order:** The pipeline is built by the engine calling `Configure(IApplicationBuilder)` on each `INopStartup` in order. Approximate order by `Order` value: NopProxyStartup (-1), ErrorHandlerStartup (0), NopStaticFilesStartup (99), NopCommonStartup (100), NopWebMarkupMinStartup (300), NopRoutingStartup (400), AuthenticationStartup (500), AuthorizationStartup (600), NopMvcStartup (1000), NopEndpoints (900). Within these: proxy (forwarded headers) → exception handler, bad request, 404 → keep-alive, install check, session, request localization, PDF → static files → markup min → routing → authentication → authorization → MVC → endpoints. Custom middleware includes `InstallUrlMiddleware` (redirect to install if DB not installed) and `AuthenticationMiddleware` (Nop-specific auth).

**Request lifecycle (high level):** Incoming request → host → proxy/forwarded headers → exception handler → 404/bad request handling → keep-alive → install URL check (redirect to install if needed) → session → request localization → static files (short-circuit if match) → HTML minification → routing → authentication (cookie) → authorization → MVC (controller/action selection) → action filters and model binding → controller action → services (e.g. order, product, customer) → repository / cache / events → response (view, JSON, redirect) → endpoint → response.

---

## 6. MODULE MAP

**Repository layout (high level):**

- **`src/Libraries/Nop.Core`** — Core domain and infrastructure used by all other modules. Contains: domain entities (Catalog, Customers, Orders, Shipping, etc.), configuration types (`IConfig`, `AppSettings`), caching interfaces, events (`IEventPublisher`), infrastructure (engine, type finder, startup tasks), security and HTTP helpers. **Depends on:** nothing (only .NET and NuGet packages).

- **`src/Libraries/Nop.Data`** — Data access and database setup. Contains: `IRepository<T>`, `EntityRepository<T>`, data provider abstraction and implementations (MsSql, MySql, PostgreSql), LinqToDB integration, FluentMigrator migrations (schema and data), `DataSettingsManager`, mapping. **Depends on:** Nop.Core.

- **`src/Libraries/Nop.Services`** — Application and domain services (business logic layer). Organized by domain: Affiliates, Attributes, Authentication, Blogs, Catalog, Common, Customers, Directory, Discounts, Events, ExportImport, Forums, Gdpr, Installation, Localization, Logging, Media, Messages, News, Orders, Payments, Plugins, ScheduleTasks, Security, Seo, Shipping, Stores, Tax, Themes, Topics, Vendors. Each area typically has interfaces (e.g. `IProductService`) and implementations, plus optional cache event consumers. **Depends on:** Nop.Core, Nop.Data.

- **`src/Presentation/Nop.Web.Framework`** — Web-specific infrastructure used by the main app and plugins. Contains: MVC infrastructure (routing, model binding, filters, tag helpers), themes, security (captcha, auth extensions), plugin loading (`ApplicationPartManager` extensions), middleware extensions, validators, WebOptimizer integration, menu (admin), localization helpers. **Depends on:** Nop.Core, Nop.Data, Nop.Services (and thus transitively Core and Data).

- **`src/Presentation/Nop.Web`** — The host application. Contains: `Program.cs`, Controllers (public and Areas/Admin), Factories (model factories for views), Views, Components, Themes, Plugins folder (deployed plugins), App_Data (appsettings, data protection), wwwroot, route and dependency registration. **Depends on:** Nop.Core, Nop.Data, Nop.Services, Nop.Web.Framework.

- **`src/Plugins/*`** — Optional plugin projects. Each plugin implements `IPlugin`, may implement `IRouteProvider`, `INopStartup`, `IAdminMenuPlugin`, and event consumers. **Depend on:** Nop.Core and usually Nop.Web.Framework (and often Nop.Services) via project references.

- **`src/Tests/Nop.Tests`** — Unit and integration tests (Nop.Core.Tests, Nop.Data.Tests, Nop.Services.Tests, Nop.Web.Tests). **Depends on:** corresponding production projects and test helpers (e.g. in-memory/SQLite data provider for tests).

- **`src/Build`** — Build-time tool `ClearPluginAssemblies` (and its solution). **Depends on:** none from main app.

- **`upgradescripts/`** — Versioned SQL and readme files for manual or assisted upgrades between major/minor versions (e.g. 4.50–4.60). Used for data migrations and long-running SEO/customer data migrations. **Not a runtime module.**

**Textual dependency map:**

```
Nop.Web
  → Nop.Web.Framework, Nop.Services, Nop.Data, Nop.Core

Nop.Web.Framework
  → Nop.Services, Nop.Data, Nop.Core

Nop.Services
  → Nop.Data, Nop.Core

Nop.Data
  → Nop.Core

Nop.Core
  → (external packages only)

Plugins
  → Nop.Web.Framework (and/or Nop.Services), Nop.Core
```

---

## 7. DOMAIN MODEL

**Main entities:** Domain entities live in `Nop.Core.Domain` and inherit from `BaseEntity` (integer `Id`). Examples: `Product`, `Category`, `Manufacturer`, `Customer`, `Order`, `OrderItem`, `Address`, `Shipment`, `ShoppingCartItem`, `Discount`, `Currency`, `Country`, `StateProvince`, `Store`, `Vendor`, `ScheduleTask`, `Setting`, `Language`, `Topic`, `NewsItem`, `BlogPost`, `MessageTemplate`, `QueuedEmail`, `PermissionRecord`, `CustomerRole`, `ProductAttribute`, `SpecificationAttribute`, `Download`, `Picture`, `GdprLog`, etc. Many entities implement `ISoftDeletedEntity` for logical delete. Store mapping is supported via `IStoreMappingSupported` and `StoreMapping`.

**Aggregates:** The codebase does not formalize DDD-style aggregates with explicit boundaries. Order-related consistency is handled in services (e.g. `IOrderProcessingService`) that coordinate `Order`, `OrderItem`, `Shipment`, and inventory. Product catalog is centered around `Product` with related entities (categories, manufacturers, attributes, pictures). Customer data is split across `Customer`, `CustomerRole`, `Address`, and `GenericAttribute` for extensible key-value data.

**Services:** Application and domain logic reside in `Nop.Services`. Services are interface-based (e.g. `IOrderProcessingService`, `IProductService`, `ICustomerService`, `IShippingService`, `IPaymentService`, `IPluginService`, `IScheduleTaskService`). They orchestrate repositories, emit events, call plugins (e.g. payment, shipping), and enforce workflows (e.g. place order, recalculate totals, apply discounts).

**Business logic placement:** In the services layer. Controllers and model factories are thin; they call services and map to view models. Validation is in FluentValidation validators and model binding. Domain entities are mostly anemic (properties + optional domain events); invariants and rules are enforced in services.

**Workflows:** Enforced by service methods. Examples: placing an order (`IOrderProcessingService.PlaceOrderAsync`) coordinates cart, payment, shipping, inventory, and order persistence. Plugin interfaces (e.g. `IPaymentMethod`, `IShippingRateComputationMethod`) allow multiple implementations; the corresponding plugin managers resolve the active one and call it from the service. Events (`IEventPublisher`) are used for side effects (e.g. cache invalidation, logging, sending emails) rather than for core workflow control.

---

## 8. DATA LAYER

**Database type and schema:** The application supports SQL Server, MySQL, and PostgreSQL. The provider and connection string are configured in application settings (e.g. `ConnectionStrings` in `appsettings.json` under `App_Data`, or legacy `dataSettings.json`/`Settings.txt`). Schema is versioned and applied via **FluentMigrator** migrations in `Nop.Data.Migrations` (Installation, UpgradeTo4x0, etc.). Migrations are tagged with `NopMigrationAttribute` and a version; `MigrationManager` applies “schema” and “data” migrations. Upgrade scripts in `upgradescripts/` provide additional SQL for major upgrades (e.g. long-running customer/SEO migrations).

**Migrations:** FluentMigrator runner is configured in `NopDbStartup` with `AddSqlServer().AddMySql5().AddPostgres()`. Migrations are scanned from assemblies that contain `MigrationBase`-derived types. On startup (when DB is installed), `MigrationManager.ApplyUpMigrations` is called for the web and data assemblies. Installation creates the initial schema via installation-specific migrations.

**Repository / data access pattern:** Generic repository `IRepository<TEntity>` with async and sync methods: `GetByIdAsync`, `GetAllAsync`, `GetAllPagedAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`, and overloads with predicates and cache keys. Implementation is `EntityRepository<TEntity>`, which uses the LinqToDB-based data provider. Table access is via `IRepository<T>.Table` (IQueryable). Repositories are registered as scoped: `services.AddScoped(typeof(IRepository<>), typeof(EntityRepository<>))`.

**Transactions:** Transaction handling is done at the service or database layer where needed; the repository interface does not expose an explicit unit-of-work. FluentMigrator runs migrations in their own transaction scope.

**Caching:** Repositories accept optional `getCacheKey` and `useShortTermCache` parameters. Cache is used for single-entity and list queries; invalidation is event-driven via consumers that react to entity insert/update/delete events and remove or update cache entries. Distributed cache (Redis/SQL) can be used for static cache when running in a web farm.

---

## 9. EXECUTION FLOWS

**Flow 1 — Place order (checkout):**  
User submits checkout form → `CheckoutController` (e.g. action that processes payment) → model binding and validation → `IOrderProcessingService.PlaceOrderAsync(processPaymentRequest)` → order processing service coordinates cart (`IShoppingCartService`), payment (via `IPaymentService` and active payment plugin), shipping, totals, and discount application → creates `Order` and `OrderItem` entities, updates inventory, may create shipment → `IRepository<Order>.InsertAsync` (and related repos) → events published (e.g. order placed) → response (redirect to order confirmation or payment URL). Payment plugins may redirect to an external gateway; on return, the same or another action completes the order.

**Flow 2 — Scheduled task run:**  
Timer in `TaskScheduler` fires for a task → HTTP request to `GET/POST {storeUrl}/scheduletask/runtask` with task type identifier → `ScheduleTaskController` receives request → validates request (e.g. secret or internal call) → `IScheduleTaskRunner.RunAsync(scheduleTask)` → runner resolves task type from `ScheduleTask.Type` (assembly-qualified name), resolves instance from DI (`EngineContext.Current.Resolve(type)`), acquires lock via `ILocker`, executes task, updates `ScheduleTask` last run time via `IScheduleTaskService` → response. Task examples: `UpdateExchangeRateTask`, clear log, send queued emails.

**Flow 3 — Product page (public store):**  
Request to product URL → routing (e.g. `GenericUrlRouteProvider` or slug-based route) → `ProductController` action (e.g. ProductDetails) → `IProductService.GetProductByIdAsync`, `ICategoryService`, optional `IProductAttributeService`, etc. → repositories and cache → model factory builds view model → view (Razor) rendered → response. Optional event publishing for “product viewed” or similar; cache event consumers keep product/category cache in sync with repository changes.

---

## 10. CONFIGURATION & ENVIRONMENTS

**Environment configuration:** Configuration is built from multiple sources: `App_Data/appsettings.json`, `App_Data/appsettings.{Environment}.json` (where `Environment` is the hosting environment name, e.g. Development, Production), and environment variables. The `IConfig`-based sections are bound and stored in `Singleton<AppSettings>.Instance`; plugins can add their own config types. Data settings (connection string, provider) can come from the same app settings or from legacy files (`dataSettings.json` / `Settings.txt`) that are migrated into app settings on first load.

**Secrets:** Connection strings and other secrets can be stored in `appsettings.json` (often not recommended for production), in environment variables, or in environment-specific JSON files that are not committed. Data Protection keys can be stored on disk (`App_Data/DataProtectionKeys`) or in Azure (Azure Blob/Vault) via `AzureBlobConfig` / Azure Data Protection packages. There is no dedicated secrets manager in the core; the application relies on configuration and file system permissions.

**Environment separation:** The hosting environment name is used to load `appsettings.{Environment}.json` and to enable developer exception pages or full error stack (e.g. `CommonConfig.DisplayFullErrorStack` or `IWebHostEnvironment.IsDevelopment()`). No separate “test” or “staging” application logic beyond configuration and environment name.

---

## 11. DEPLOYMENT MODEL

**Build:** From the solution root (or `src`): `dotnet build NopCommerce.sln -c Release`. The web project is published with `dotnet publish Nop.Web.csproj -c Release -o <output>`. The build copies content (App_Data, Plugins, Themes, wwwroot, etc.) and runs the plugin cleanup target to reduce plugin output size.

**Runtime:** The application runs as an ASP.NET Core web process. Entry point is `dotnet Nop.Web.dll` (or equivalent when run from Visual Studio). Kestrel listens on the configured URLs (e.g. `ASPNETCORE_URLS=http://+:80` in Docker). No separate worker or queue processor; scheduled tasks run inside the web process via HTTP-triggered endpoints.

**Topology:** Single-server deployment: one app instance and one database. Multi-instance (web farm) is supported: multiple app instances can share the same database; distributed cache (Redis or SQL Server) and RedisSynchronizedMemory are used so that cache and scheduled tasks behave correctly (e.g. task lock to avoid duplicate runs). Docker Compose runs one web container and one database container by default; scaling would be done by adding more web containers and a shared cache and database.

**Containers:** The provided Dockerfile produces a single image that runs the web app. The entrypoint is `entrypoint.sh`, which then executes `dotnet Nop.Web.dll`. Database is a separate container (SQL Server, MySQL, or PostgreSQL per compose file). No orchestrator (e.g. Kubernetes) configuration in the repository.

---

*End of System Architecture Specification.*
