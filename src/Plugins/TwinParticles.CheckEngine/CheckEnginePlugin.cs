using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Cms;
using Nop.Core.Domain.ScheduleTasks;
using Nop.Data.Migrations;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;
using Nop.Services.Security;
using Nop.Web.Framework.Infrastructure;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine;

public sealed class CheckEnginePlugin : BasePlugin, IMiscPlugin, IWidgetPlugin
{
    private readonly ILocalizationService _localizationService;
    private readonly ILanguageService _languageService;
    private readonly IMigrationManager _migrationManager;
    private readonly IPermissionService _permissionService;
    private readonly IScheduleTaskService _scheduleTaskService;
    private readonly ISettingService _settingService;
    private readonly IWebHelper _webHelper;
    private readonly IVehicleSeedLoader _vehicleSeedLoader;

    public CheckEnginePlugin(ILocalizationService localizationService,
        ILanguageService languageService,
        IMigrationManager migrationManager,
        IPermissionService permissionService,
        IScheduleTaskService scheduleTaskService,
        ISettingService settingService,
        IWebHelper webHelper,
        IVehicleSeedLoader vehicleSeedLoader)
    {
        _localizationService = localizationService;
        _languageService = languageService;
        _migrationManager = migrationManager;
        _permissionService = permissionService;
        _scheduleTaskService = scheduleTaskService;
        _settingService = settingService;
        _webHelper = webHelper;
        _vehicleSeedLoader = vehicleSeedLoader;
    }

    /// <summary>
    /// Check Engine migrations live in the Infrastructure assembly, but nopCommerce only scans the
    /// assembly declaring the plugin type, so they must be applied explicitly.
    /// </summary>
    private static Assembly MigrationAssembly =>
        typeof(Infrastructure.DependencyInjection.ServiceCollectionExtensions).Assembly;

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/CheckEngine/Configure";
    }

    public Task<IList<string>> GetWidgetZonesAsync()
    {
        return Task.FromResult<IList<string>>([
            AdminWidgetZones.PluginListButtons,
            PublicWidgetZones.HeaderAfter,
            PublicWidgetZones.HeaderMenuAfter,
            PublicWidgetZones.BodyStartHtmlTagAfter,
            PublicWidgetZones.ProductDetailsTop,
            PublicWidgetZones.HomepageTop
        ]);
    }

    public Type GetWidgetViewComponent(string widgetZone)
    {
        if (widgetZone.Equals(AdminWidgetZones.PluginListButtons, StringComparison.OrdinalIgnoreCase))
            return typeof(Components.UninstallPreparationViewComponent);

        return typeof(Components.CheckEngineThemeChromeViewComponent);
    }

    public bool HideInWidgetList => false;

    public override async Task InstallAsync()
    {
        _migrationManager.ApplyUpMigrations(MigrationAssembly, MigrationProcessType.Installation);

        await _settingService.SaveSettingAsync(new CheckEnginePluginSettings());
        var settings = await _settingService.LoadSettingAsync<CheckEnginePluginSettings>();
        CheckEngineAiSettingsSync.Apply(settings);
        await EnsureScheduleTasksAsync();

        await AddOrUpdateLocaleResourcesAsync();
        await EnsureWidgetActiveAsync();
        await _vehicleSeedLoader.SeedAsync(default);

        await base.InstallAsync();
    }

    /// <summary>
    /// Applies the plugin's locale resources. Run on update as well as install, otherwise strings
    /// added by a release only exist on stores that installed the plugin fresh, and upgraded stores
    /// render raw resource keys.
    /// </summary>
    private async Task AddOrUpdateLocaleResourcesAsync()
    {
        var englishResources = new Dictionary<string, string>
        {
            ["Plugins.TwinParticles.CheckEngine.General"] = "Check Engine",
            ["Plugins.TwinParticles.CheckEngine.General.Enabled"] = "Enabled",
            ["Plugins.TwinParticles.CheckEngine.General.Enabled.Hint"] = "Determines whether Check Engine core services are enabled.",
            ["Plugins.TwinParticles.CheckEngine.Configuration"] = "Configuration",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.Enabled"] = "Enabled",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.Enabled.Hint"] = "Toggle to enable or disable the Check Engine plugin scaffold.",
            ["Plugins.TwinParticles.CheckEngine.Configuration.General"] = "General",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Ai"] = "AI platform",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Ai.Hint"] = "Configure provider credentials, spend ceilings, and per-feature toggles. AI stays disabled until disclosure is acknowledged and features are enabled.",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Ai.DisclosureRequired"] = "Acknowledge the data disclosure before enabling any AI feature.",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Ai.DisclosureSummary"] = "Review the data categories transmitted to your AI provider before enabling features below.",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiProviderKind"] = "Provider",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingProviderKind"] = "Embedding provider",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingProviderKind.SameAsCompletion"] = "Same as completion (OpenAI when Anthropic)",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingProviderKind.Deterministic"] = "Deterministic (offline)",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingProviderKind.Hint"] = "Anthropic has no embedding API. Route semantic search to OpenAI/Azure or use deterministic vectors.",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingBaseUrl"] = "Embedding base URL (optional)",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingApiKey"] = "Embedding API key (optional)",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiBaseUrl"] = "Base URL",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiApiKey"] = "API key",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiModel"] = "Completion model",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingModel"] = "Embedding model",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiAzureDeploymentName"] = "Azure deployment name",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiAzureEmbeddingDeploymentName"] = "Azure embedding deployment name",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiDailyTokenCeiling"] = "Daily token ceiling",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiTokenCostPer1KUsd"] = "Estimated cost per 1K tokens (USD)",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiResponseCacheTtlMinutes"] = "Response cache TTL (minutes)",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiDisclosureAcknowledged"] = "Data disclosure acknowledged",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEnabledFeatures"] = "Enabled features (legacy CSV)",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiPerFeatureDailyTokenCeilings"] = "Per-feature daily ceilings",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiFeatures"] = "Enabled AI features",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableImportEnrichment"] = "Import enrichment",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableImportTranslation"] = "Import translation",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableImportSeo"] = "Import SEO generation",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableSearchNaturalLanguage"] = "Natural-language search",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableSearchSemantic"] = "Semantic search",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableFitmentInference"] = "Fitment inference",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableCustomerAssistant"] = "Customer assistant",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableRecommendations"] = "Fitment recommendations",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Marketplace"] = "Marketplace",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Marketplace.Hint"] = "Supplier applications stay closed until this flag is on and the licence includes marketplace entitlement (Business and above).",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableMarketplace"] = "Accept supplier applications",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.MarketplaceAgreementVersion"] = "Supplier agreement version",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review"] = "Vendor onboarding",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Queue"] = "Applications under review",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Hint"] = "Approve only after verification and acceptance of the current operator agreement. Banking details stay encrypted and are never shown here.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply"] = "Become a supplier",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Eyebrow"] = "Marketplace onboarding",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Hero"] = "Join the parts marketplace",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Hint"] = "Submit your business profile, tax identifiers, and payout details. Listing starts only after review and agreement acceptance.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Steps"] = "Application steps",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Step.Business"] = "Business profile",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Step.Payout"] = "Payout details",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Step.Review"] = "Operator review",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Section.Business"] = "Business information",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Section.Business.Hint"] = "Legal identity and categories you intend to supply.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Section.Payout"] = "Payout & banking",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Section.Payout.Hint"] = "Encrypted at rest. Shown only to authorized operators during approval.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Submit"] = "Submit application",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Submitting"] = "Submitting…",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Success"] = "Application received",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Success.Hint"] = "Your application is under review. Save the access token below to check status or accept the agreement before signing in.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.VendorId"] = "Application reference",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.AccessToken"] = "Guest access token",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.AccessToken.Hint"] = "Shown once. Required for status checks until your account is linked.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.CopyToken"] = "Copy token",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Copied"] = "Token copied",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Legal"] = "Enter the legal business name.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Email"] = "Enter a valid contact email.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Tax"] = "Enter at least one tax id as a JSON array.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Categories"] = "Enter at least one category.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Banking"] = "Enter payout / banking details.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Agreement"] = "Accept the supplier agreement to continue.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Submit"] = "Unable to submit the application.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Network"] = "Network error. Try again.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.TradingName.Hint"] = "Optional public-facing name.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.TaxIds.Hint"] = "JSON array, e.g. [\"VAT-123\"]",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Categories.Hint"] = "Comma-separated, e.g. cooling,filters",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Banking.Hint"] = "IBAN, account holder, and bank name. Never share in chat or email.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.LegalName"] = "Legal business name",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.TradingName"] = "Trading name",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Email"] = "Contact email",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.TaxIds"] = "Tax identifiers (JSON)",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Categories"] = "Categories",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Banking"] = "Banking / payout details",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Agreement.Text"] = "By applying you accept the current operator supplier agreement. A versioned acceptance record is stored.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Agreement.Accept"] = "I accept the supplier agreement",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard"] = "Vendor dashboard",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Eyebrow"] = "Supplier workspace",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Hero"] = "Your marketplace cockpit",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Hint"] = "Manage your catalog stock, review orders, submit fitment proposals, and track performance.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Tabs"] = "Dashboard sections",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Tab.Overview"] = "Overview",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Loading"] = "Loading dashboard…",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Products"] = "Products",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.StockUnits"] = "Stock units",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.LowStock"] = "low",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Orders"] = "Orders",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Orders.Hint"] = "Orders containing your catalog lines.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Orders.Empty"] = "No orders yet.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.OrderId"] = "Order",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.OrderTotal"] = "Total",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Catalog"] = "Catalog",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Catalog.Hint"] = "Product ids assigned to your vendor account.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Catalog.Empty"] = "No catalog products assigned.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Customers"] = "Customers",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Scorecard.Hint"] = "Rolling performance metrics used for operator scoreboards.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Scorecard"] = "Performance scorecard",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard"] = "Supplier scoreboard",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard.Hint"] = "Operator view of supplier fill rate, cancellations, fitment rejections, and on-time shipment.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Scorecard.FillRate"] = "Fill rate",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Scorecard.CancelRate"] = "Cancel rate",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Scorecard.ClaimRejectRate"] = "Claim rejection rate",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Scorecard.OnTimeShipment"] = "On-time shipment",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Inventory"] = "Inventory",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Inventory.Hint"] = "Update stock quantities for products in your catalog.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Inventory.Product"] = "Product",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Inventory.Stock"] = "Stock quantity",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Inventory.Save"] = "Save",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Inventory.Saved"] = "Stock updated",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Inventory.Empty"] = "No inventory rows yet.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.Proposals"] = "Fitment proposals",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.Hint"] = "Proposals enter the operator review queue. Published claims remain globally visible to shoppers.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.ProductId"] = "Product id",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.VehicleConfigId"] = "Vehicle configuration id",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.ClaimId"] = "Claim",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.Status"] = "Status",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.Submit"] = "Submit proposal",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.Revoke"] = "Revoke",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.Empty"] = "No fitment proposals yet.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Statements"] = "Statements",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Statements.Hint"] = "Commission accruals and payout statements for your vendor account.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Statements.Pending"] = "Commission statements and payouts will appear here once payout reconciliation is enabled.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Statements.NetPayout"] = "Net payout",
            ["Plugins.TwinParticles.CheckEngine.Commission.Configure"] = "Commission plan",
            ["Plugins.TwinParticles.CheckEngine.Commission.Configure.Hint"] = "Rules are evaluated by priority (lower first). Category overrides beat defaults. Rates are snapshotted on each order line at placement.",
            ["Plugins.TwinParticles.CheckEngine.Commission.Vendor"] = "Vendor & plan",
            ["Plugins.TwinParticles.CheckEngine.Commission.SelectVendor"] = "Supplier",
            ["Plugins.TwinParticles.CheckEngine.Commission.PlanName"] = "Plan name",
            ["Plugins.TwinParticles.CheckEngine.Commission.PlanActive"] = "Plan active",
            ["Plugins.TwinParticles.CheckEngine.Commission.Rules"] = "Commission rules",
            ["Plugins.TwinParticles.CheckEngine.Commission.Rules.Hint"] = "Add flat, percentage, tiered, or category-specific rules. Lower priority numbers run first.",
            ["Plugins.TwinParticles.CheckEngine.Commission.AddRule"] = "Add rule",
            ["Plugins.TwinParticles.CheckEngine.Commission.AdvancedJson"] = "Advanced JSON",
            ["Plugins.TwinParticles.CheckEngine.Commission.AdvancedJson.Hint"] = "Power-user import/export. Changes here can be applied back to the form.",
            ["Plugins.TwinParticles.CheckEngine.Commission.ImportJson"] = "Apply JSON to form",
            ["Plugins.TwinParticles.CheckEngine.Commission.Save"] = "Save commission plan",
            ["Plugins.TwinParticles.CheckEngine.Payout.Admin"] = "Payout statements",
            ["Plugins.TwinParticles.CheckEngine.Payout.Admin.Hint"] = "Generate periodic vendor statements from snapshotted commissions, finalize, push to ERPNext, and reconcile totals.",
            ["Plugins.TwinParticles.CheckEngine.Payout.SelectVendor"] = "Supplier",
            ["Plugins.TwinParticles.CheckEngine.Payout.PeriodStart"] = "Period start",
            ["Plugins.TwinParticles.CheckEngine.Payout.PeriodEnd"] = "Period end",
            ["Plugins.TwinParticles.CheckEngine.Payout.Generate"] = "Generate statement",
            ["Plugins.TwinParticles.CheckEngine.Payout.Generating"] = "Generating…",
            ["Plugins.TwinParticles.CheckEngine.Payout.Generated"] = "Statement created",
            ["Plugins.TwinParticles.CheckEngine.Payout.Statements"] = "Statements",
            ["Plugins.TwinParticles.CheckEngine.Payout.Details"] = "Statement details",
            ["Plugins.TwinParticles.CheckEngine.Payout.View"] = "View",
            ["Plugins.TwinParticles.CheckEngine.Payout.Empty"] = "No statements for this supplier yet.",
            ["Plugins.TwinParticles.CheckEngine.Payout.SelectStatement"] = "Select a statement from the table first.",
            ["Plugins.TwinParticles.CheckEngine.Payout.Finalize"] = "Finalize",
            ["Plugins.TwinParticles.CheckEngine.Payout.PushErp"] = "Push to ERP",
            ["Plugins.TwinParticles.CheckEngine.Payout.Reconcile"] = "Reconcile",
            ["Plugins.TwinParticles.CheckEngine.Payout.ErpReference"] = "ERP reference",
            ["Plugins.TwinParticles.CheckEngine.Payout.Col.Id"] = "ID",
            ["Plugins.TwinParticles.CheckEngine.Payout.Col.Period"] = "Period",
            ["Plugins.TwinParticles.CheckEngine.Payout.Col.Gross"] = "Gross sales",
            ["Plugins.TwinParticles.CheckEngine.Payout.Col.Commission"] = "Commission",
            ["Plugins.TwinParticles.CheckEngine.Payout.Col.Refunds"] = "Refunds",
            ["Plugins.TwinParticles.CheckEngine.Payout.Col.Adjustments"] = "Adjustments",
            ["Plugins.TwinParticles.CheckEngine.Payout.Col.Status"] = "Status",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Admin.Loading"] = "Loading…",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Admin.ErrorLoad"] = "Could not load data.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Admin.ActionOk"] = "Saved successfully.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Admin.ActionFail"] = "Action failed.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Empty"] = "No applications in the review queue.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Action.Review"] = "Mark under review",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Action.Approve"] = "Approve",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Action.Reject"] = "Reject",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.RejectNotes"] = "Rejection notes (optional):",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Col.Id"] = "ID",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Col.LegalName"] = "Legal name",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Col.Email"] = "Email",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Col.Categories"] = "Categories",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Col.Status"] = "Status",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Col.Agreement"] = "Agreement",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Col.Banking"] = "Banking",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard.Vendors"] = "Suppliers tracked",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard.OrdersSample"] = "Orders in sample",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard.Empty"] = "No scorecard data yet.",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard.Col.Vendor"] = "Supplier",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard.Col.Orders"] = "Orders",
            ["Plugins.TwinParticles.CheckEngine.Marketplace.Statements.None"] = "No payout statements yet for this vendor.",
            ["Plugins.TwinParticles.CheckEngine.Dashboard"] = "Check Engine Dashboard",
            ["Plugins.TwinParticles.CheckEngine.Dashboard.AdminLinks"] = "Admin JSON endpoints",
            ["Plugins.TwinParticles.CheckEngine.Dashboard.ImportUpload"] = "Import upload",
            ["Plugins.TwinParticles.CheckEngine.Dashboard.ImportUpload.Hint"] = "Select a supplier file and run the import pipeline (content is sent as Base64 JSON).",
            ["Plugins.TwinParticles.CheckEngine.Garage.Label"] = "Garage",
            ["Plugins.TwinParticles.CheckEngine.Garage.SelectVehicle"] = "Select vehicle",
            ["Plugins.TwinParticles.CheckEngine.Garage.Empty"] = "No vehicle selected",
            ["Plugins.TwinParticles.CheckEngine.Garage.AddVehicle"] = "Add vehicle (VIN)…",
            ["Plugins.TwinParticles.CheckEngine.Garage.VehicleSelector"] = "Choose the vehicle to check parts against",
            ["Plugins.TwinParticles.CheckEngine.Garage.VinPrompt"] = "Enter a VIN to add a vehicle",
            ["Plugins.TwinParticles.CheckEngine.Garage.AddFailed"] = "Unable to add that vehicle.",
            ["Plugins.TwinParticles.CheckEngine.Garage.VinDisambiguation.Title"] = "Which vehicle is this?",
            ["Plugins.TwinParticles.CheckEngine.Garage.VinDisambiguation.Lead"] = "This VIN matches more than one vehicle configuration. Choose the one that matches your car.",
            ["Plugins.TwinParticles.CheckEngine.Garage.VinDisambiguation.Cancel"] = "Cancel",
            ["Plugins.TwinParticles.CheckEngine.Garage.VinDisambiguation.Year"] = "Model year",
            ["Plugins.TwinParticles.CheckEngine.Search.Label"] = "Search parts, OEM number, or VIN",
            ["Plugins.TwinParticles.CheckEngine.Search.Placeholder"] = "Search parts, OEM, or VIN",
            ["Plugins.TwinParticles.CheckEngine.Search.Submit"] = "Search",
            ["Plugins.TwinParticles.CheckEngine.Search.Widen"] = "Include unverified fit",
            ["Plugins.TwinParticles.CheckEngine.Search.Hint"] = "Enter a keyword, an OEM part number, or a 17-character VIN. Results are filtered to your active vehicle unless you include unverified fit.",
            ["Plugins.TwinParticles.CheckEngine.Search.ResultsLabel"] = "Check Engine search results",
            ["Plugins.TwinParticles.CheckEngine.Search.ResultsCount"] = "results",
            ["Plugins.TwinParticles.CheckEngine.Search.ParsedIntent"] = "Understood as",
            ["Plugins.TwinParticles.CheckEngine.Search.Mode"] = "Mode",
            ["Plugins.TwinParticles.CheckEngine.Search.Mode.Auto"] = "Auto",
            ["Plugins.TwinParticles.CheckEngine.Search.Mode.NaturalLanguage"] = "Natural language",
            ["Plugins.TwinParticles.CheckEngine.Search.Mode.Semantic"] = "Semantic",
            ["Plugins.TwinParticles.CheckEngine.Search.Admin"] = "Search admin",
            ["Plugins.TwinParticles.CheckEngine.Search.Admin.KeywordIndex"] = "Keyword index",
            ["Plugins.TwinParticles.CheckEngine.Search.Admin.RebuildKeyword"] = "Rebuild keyword index",
            ["Plugins.TwinParticles.CheckEngine.Search.Admin.RebuildKeywordAndEmbeddings"] = "Rebuild keyword + refresh embeddings",
            ["Plugins.TwinParticles.CheckEngine.Search.Admin.Embeddings"] = "Semantic embeddings",
            ["Plugins.TwinParticles.CheckEngine.Search.Admin.RebuildEmbeddingsAll"] = "Rebuild all embedding locales",
            ["Plugins.TwinParticles.CheckEngine.Search.Admin.RefreshEmbeddings"] = "Refresh stale embeddings",
            ["Plugins.TwinParticles.CheckEngine.Search.Synonyms"] = "Bilingual search synonyms",
            ["Plugins.TwinParticles.CheckEngine.Search.Synonyms.Hint"] = "Controlled AR→EN automotive synonym pairs used to expand semantic search queries and embedding index text. Overrides are audited.",
            ["Plugins.TwinParticles.CheckEngine.Search.Synonyms.Arabic"] = "Arabic term",
            ["Plugins.TwinParticles.CheckEngine.Search.Synonyms.English"] = "English term",
            ["Plugins.TwinParticles.CheckEngine.Search.Synonyms.Source"] = "Source",
            ["Plugins.TwinParticles.CheckEngine.Search.Synonyms.OverridesJson"] = "Override terms (JSON object, Arabic keys)",
            ["Plugins.TwinParticles.CheckEngine.Search.Synonyms.Save"] = "Save synonym overrides",
            ["Plugins.TwinParticles.CheckEngine.Search.Empty.Title"] = "No matching parts found",
            ["Plugins.TwinParticles.CheckEngine.Search.Empty.Hint"] = "Check the OEM number or VIN, or widen fitment to include unverified parts.",
            ["Plugins.TwinParticles.CheckEngine.Search.Unavailable"] = "Search is unavailable right now.",
            ["Plugins.TwinParticles.CheckEngine.Search.Facets.Title"] = "Refine",
            ["Plugins.TwinParticles.CheckEngine.Search.Facets.Category"] = "Category",
            ["Plugins.TwinParticles.CheckEngine.Search.Facets.Brand"] = "Brand",
            ["Plugins.TwinParticles.CheckEngine.Search.Facets.Price"] = "Price",
            ["Plugins.TwinParticles.CheckEngine.Search.Facets.Fitment"] = "Fitment",
            ["Plugins.TwinParticles.CheckEngine.Search.Facets.Clear"] = "Clear filters",
            ["Plugins.TwinParticles.CheckEngine.Search.Recovery.Title"] = "Try one of these",
            ["Plugins.TwinParticles.CheckEngine.Search.Suggest.Vehicles"] = "Vehicles",
            ["Plugins.TwinParticles.CheckEngine.Search.Suggest.Oems"] = "OEM numbers",
            ["Plugins.TwinParticles.CheckEngine.Search.Suggest.Products"] = "Products",
            ["Plugins.TwinParticles.CheckEngine.Menu.AriaLabel"] = "Parts navigation",
            ["Plugins.TwinParticles.CheckEngine.Menu.AllParts"] = "All parts",
            ["Plugins.TwinParticles.CheckEngine.Menu.Eyebrow"] = "Parts catalog",
            ["Plugins.TwinParticles.CheckEngine.Menu.Title"] = "Browse by category",
            ["Plugins.TwinParticles.CheckEngine.Menu.Hint"] = "Choose a category, or add your vehicle to see verified-fit parts first.",
            ["Plugins.TwinParticles.CheckEngine.Menu.Empty"] = "Categories will appear here when the catalog is published.",
            ["Plugins.TwinParticles.CheckEngine.Menu.AddVehicle"] = "Add your vehicle",
            ["Plugins.TwinParticles.CheckEngine.Menu.SearchByOem"] = "Search by OEM number",
            ["Plugins.TwinParticles.CheckEngine.Fitment.Fits"] = "Fits your vehicle",
            ["Plugins.TwinParticles.CheckEngine.Fitment.Fits.Hint"] = "Verified against your active vehicle.",
            ["Plugins.TwinParticles.CheckEngine.Fitment.DoesNotFit"] = "Does not fit",
            ["Plugins.TwinParticles.CheckEngine.Fitment.DoesNotFit.Hint"] = "This part is not compatible with your active vehicle.",
            ["Plugins.TwinParticles.CheckEngine.Fitment.Unknown"] = "Fitment unknown",
            ["Plugins.TwinParticles.CheckEngine.Fitment.Unknown.Hint"] = "We could not verify this part against your vehicle.",
            ["Plugins.TwinParticles.CheckEngine.Fitment.NeedsDetail"] = "More vehicle detail needed",
            ["Plugins.TwinParticles.CheckEngine.Fitment.NeedsDetail.Hint"] = "This part fits some versions of your vehicle. Add the missing details to confirm.",
            ["Plugins.TwinParticles.CheckEngine.Fitment.NeedsDetail.Cta"] = "Complete vehicle details",
            ["Plugins.TwinParticles.CheckEngine.Fitment.SelectVehicle"] = "Select your vehicle",
            ["Plugins.TwinParticles.CheckEngine.Fitment.SelectVehicle.Hint"] = "Choose a vehicle to check whether this part fits.",
            ["Plugins.TwinParticles.CheckEngine.Fitment.SelectVehicle.Cta"] = "Add your vehicle",
            ["Plugins.TwinParticles.CheckEngine.Hero.Eyebrow"] = "Check Engine",
            ["Plugins.TwinParticles.CheckEngine.Hero.Title"] = "Find the part that fits your car",
            ["Plugins.TwinParticles.CheckEngine.Hero.Lead"] = "Search by VIN, OEM number, or keyword. Save your vehicle once and every result is checked against it.",
            ["Plugins.TwinParticles.CheckEngine.Hero.PrimaryCta"] = "Search parts",
            ["Plugins.TwinParticles.CheckEngine.Hero.SecondaryCta"] = "Add your vehicle",
            ["Plugins.TwinParticles.CheckEngine.L10n.Number"] = "Number format",
            ["Plugins.TwinParticles.CheckEngine.L10n.Date"] = "Date format",
            ["Plugins.TwinParticles.CheckEngine.L10n.Unit"] = "Unit format",
            ["Plugins.TwinParticles.CheckEngine.L10n.Preview"] = "Localization preview",
            ["Plugins.TwinParticles.CheckEngine.Licence.Status"] = "Licence status",
            ["Plugins.TwinParticles.CheckEngine.Licence.LastHeartbeat"] = "Last heartbeat",
            ["Plugins.TwinParticles.CheckEngine.Licence.ActivationKey"] = "Activation key",
            ["Plugins.TwinParticles.CheckEngine.Uninstall.Warning"] = "Uninstall permanently deletes Check Engine vehicle, OEM, fitment, garage, analytics, audit and integration data.",
            ["Plugins.TwinParticles.CheckEngine.Uninstall.ExportRequired"] = "Download a fresh export before uninstalling. Uninstall is blocked unless an export was prepared within the last 24 hours.",
            ["Plugins.TwinParticles.CheckEngine.Uninstall.ExportPrepared"] = "A fresh export is on file. You may proceed with uninstall until it expires.",
            ["Plugins.TwinParticles.CheckEngine.Uninstall.ExportExpired"] = "The last export has expired. Download a new export before uninstalling.",
            ["Plugins.TwinParticles.CheckEngine.Uninstall.ExportAction"] = "Download uninstall export",
            ["Plugins.TwinParticles.CheckEngine.Uninstall.ConfirmTitle"] = "Uninstall Check Engine?",
            ["Plugins.TwinParticles.CheckEngine.Uninstall.ConfirmBody"] = "This removes every Check Engine table, setting, locale resource and schedule task. Host catalog orders are kept, but vehicle, OEM and fitment data are destroyed.",
            ["Plugins.TwinParticles.CheckEngine.Uninstall.ConfirmProceed"] = "Proceed with uninstall",
            ["Plugins.TwinParticles.CheckEngine.Uninstall.ConfirmCancel"] = "Cancel",
            ["Plugins.TwinParticles.CheckEngine.Ai.Review"] = "AI review queue",
            ["Plugins.TwinParticles.CheckEngine.Ai.Review.Candidates"] = "Content candidates",
            ["Plugins.TwinParticles.CheckEngine.Ai.Review.Hint"] = "Approve or reject AI-generated descriptions, translations, and SEO. Nothing publishes until a reviewer accepts it.",
            ["Plugins.TwinParticles.CheckEngine.Ai.Review.Fitment"] = "AI fitment candidates",
            ["Plugins.TwinParticles.CheckEngine.Glossary.Admin"] = "Automotive glossary",
            ["Plugins.TwinParticles.CheckEngine.Glossary.Hint"] = "Controlled EN→AR part-type vocabulary used by AI translation. Overrides are audited and merged onto the embedded catalog.",
            ["Plugins.TwinParticles.CheckEngine.Glossary.English"] = "English term",
            ["Plugins.TwinParticles.CheckEngine.Glossary.Arabic"] = "Arabic term",
            ["Plugins.TwinParticles.CheckEngine.Glossary.Source"] = "Source",
            ["Plugins.TwinParticles.CheckEngine.Glossary.OverridesJson"] = "Override terms (JSON object)",
            ["Plugins.TwinParticles.CheckEngine.SpecKeys.Admin"] = "Specification keys",
            ["Plugins.TwinParticles.CheckEngine.SpecKeys.Hint"] = "Allowed nopCommerce attribute keys for AI specification extraction. Add custom keys or remove embedded keys; changes are audited and affect candidate validation.",
            ["Plugins.TwinParticles.CheckEngine.SpecKeys.Key"] = "Attribute key",
            ["Plugins.TwinParticles.CheckEngine.SpecKeys.Source"] = "Source",
            ["Plugins.TwinParticles.CheckEngine.SpecKeys.OverridesJson"] = "Overrides (JSON with add/remove arrays)",
            ["Plugins.TwinParticles.CheckEngine.Ai.Dashboard"] = "AI usage dashboard",
            ["Plugins.TwinParticles.CheckEngine.Ai.Dashboard.Usage"] = "Usage by feature",
            ["Plugins.TwinParticles.CheckEngine.Ai.AcknowledgeDisclosure"] = "Acknowledge data disclosure",
            ["Plugins.TwinParticles.CheckEngine.Fitment.AiReview"] = "AI fitment review",
            ["Plugins.TwinParticles.CheckEngine.Fitment.AiReview.Infer"] = "Run fitment inference",
            ["Plugins.TwinParticles.CheckEngine.Fitment.AiReview.Hint"] = "AI-inferred claims stay unpublished until a reviewer approves them. Approval promotes provenance to curator manual.",
            ["Plugins.TwinParticles.CheckEngine.Assistant.Title"] = "Parts assistant",
            ["Plugins.TwinParticles.CheckEngine.Assistant.Placeholder"] = "Ask about a part in your catalog…",
            ["Plugins.TwinParticles.CheckEngine.Assistant.Send"] = "Ask",
            ["Plugins.TwinParticles.CheckEngine.Assistant.Unavailable"] = "The assistant is unavailable right now.",
            ["Plugins.TwinParticles.CheckEngine.Assistant.Open"] = "Open parts assistant",
            ["Plugins.TwinParticles.CheckEngine.Assistant.Sources"] = "Catalog sources",
            ["Plugins.TwinParticles.CheckEngine.Assistant.Thinking"] = "Searching the catalog…",
            ["Plugins.TwinParticles.CheckEngine.Assistant.RateLimited"] = "Too many questions — please wait a moment and try again.",
            ["Plugins.TwinParticles.CheckEngine.Assistant.VehicleScoped"] = "Answers are limited to parts verified for your active vehicle.",
            ["Plugins.TwinParticles.CheckEngine.Assistant.VehicleUnscoped"] = "Select a vehicle to filter answers to verified-fit parts.",
            ["Plugins.TwinParticles.CheckEngine.Recommend.Title"] = "Also fits your vehicle",
            ["Plugins.TwinParticles.CheckEngine.Recommend.UnscopedTitle"] = "You may also like",
            ["Plugins.TwinParticles.CheckEngine.Recommend.FitmentBadge"] = "Fits your vehicle",
            ["Plugins.TwinParticles.CheckEngine.Portal.Loading"] = "Loading portal…",
            ["Plugins.TwinParticles.CheckEngine.Workshop.Title"] = "Workshop portal",
            ["Plugins.TwinParticles.CheckEngine.Workshop.Eyebrow"] = "Trade workspace",
            ["Plugins.TwinParticles.CheckEngine.Workshop.Hero"] = "Job-based ordering",
            ["Plugins.TwinParticles.CheckEngine.Workshop.Hint"] = "Create jobs, allocate parts with fitment checks, and invoice on trade credit — no retail cart required.",
            ["Plugins.TwinParticles.CheckEngine.Workshop.CreateJob"] = "Create job",
            ["Plugins.TwinParticles.CheckEngine.Workshop.Jobs"] = "Jobs",
            ["Plugins.TwinParticles.CheckEngine.Workshop.JobId"] = "Job",
            ["Plugins.TwinParticles.CheckEngine.Workshop.Status"] = "Status",
            ["Plugins.TwinParticles.CheckEngine.Workshop.LabourEstimate"] = "Labour estimate",
            ["Plugins.TwinParticles.CheckEngine.Workshop.OrderId"] = "Order",
            ["Plugins.TwinParticles.CheckEngine.Workshop.VehicleConfigId"] = "Vehicle configuration",
            ["Plugins.TwinParticles.CheckEngine.Workshop.JobDetail"] = "Job detail",
            ["Plugins.TwinParticles.CheckEngine.Workshop.Vehicles"] = "Vehicles on job",
            ["Plugins.TwinParticles.CheckEngine.Workshop.Lines"] = "Allocated lines",
            ["Plugins.TwinParticles.CheckEngine.Workshop.Fitment"] = "Fitment",
            ["Plugins.TwinParticles.CheckEngine.Workshop.UnitPrice"] = "Unit price",
            ["Plugins.TwinParticles.CheckEngine.Workshop.SelectJob"] = "Select a job to view details.",
            ["Plugins.TwinParticles.CheckEngine.Fleet.Title"] = "Fleet portal",
            ["Plugins.TwinParticles.CheckEngine.Fleet.Eyebrow"] = "Fleet operations",
            ["Plugins.TwinParticles.CheckEngine.Fleet.Hero"] = "Register and approve at scale",
            ["Plugins.TwinParticles.CheckEngine.Fleet.Hint"] = "Bulk-import VINs into your fleet register and route purchases through budget-centre approvals.",
            ["Plugins.TwinParticles.CheckEngine.Fleet.ImportVins"] = "Import VINs",
            ["Plugins.TwinParticles.CheckEngine.Fleet.VinList"] = "VIN list (space or newline separated)",
            ["Plugins.TwinParticles.CheckEngine.Fleet.Vehicles"] = "Fleet register",
            ["Plugins.TwinParticles.CheckEngine.Fleet.AssetTag"] = "Asset tag",
            ["Plugins.TwinParticles.CheckEngine.Fleet.Approvals"] = "Approval queue",
            ["Plugins.TwinParticles.CheckEngine.Fleet.MaintenanceForecast"] = "Maintenance forecast",
            ["Plugins.TwinParticles.CheckEngine.Fleet.VehicleId"] = "Vehicle",
            ["Plugins.TwinParticles.CheckEngine.Fleet.Service"] = "Service",
            ["Plugins.TwinParticles.CheckEngine.Fleet.DueDate"] = "Due date",
            ["Plugins.TwinParticles.CheckEngine.Dealer.Title"] = "Dealer portal",
            ["Plugins.TwinParticles.CheckEngine.Dealer.Eyebrow"] = "Franchise buyer",
            ["Plugins.TwinParticles.CheckEngine.Dealer.Hero"] = "Allocation-aware ordering",
            ["Plugins.TwinParticles.CheckEngine.Dealer.Hint"] = "Browse your franchise-scoped catalog, place orders within quota, and submit warranty claims.",
            ["Plugins.TwinParticles.CheckEngine.Dealer.Catalog"] = "Dealer catalog",
            ["Plugins.TwinParticles.CheckEngine.Dealer.ProductId"] = "Product",
            ["Plugins.TwinParticles.CheckEngine.Dealer.ProductName"] = "Name",
            ["Plugins.TwinParticles.CheckEngine.Dealer.Price"] = "Dealer price",
            ["Plugins.TwinParticles.CheckEngine.Dealer.Quantity"] = "Qty",
            ["Plugins.TwinParticles.CheckEngine.Dealer.PlaceOrder"] = "Place order",
            ["Plugins.TwinParticles.CheckEngine.Dealer.WarrantyClaim"] = "Warranty claims",
            ["Plugins.TwinParticles.CheckEngine.Dealer.SubmitClaim"] = "Submit claim",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.Title"] = "Vertical portal accounts",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.Hint"] = "Provision workshop, fleet, or dealer trade accounts for a nopCommerce customer. Requires portal licence entitlements.",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.CustomerId"] = "Customer ID",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.DisplayName"] = "Display name",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.ProvisionWorkshop"] = "Provision workshop account",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.ProvisionFleet"] = "Provision fleet account",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.ProvisionDealer"] = "Provision dealer account",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.DealerSeeding"] = "Dealer allocation & quota",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.DealerSeeding.Hint"] = "Seed product or category allocations and spend quotas for an existing dealer account.",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.DealerAccountId"] = "Dealer account ID",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.CategoryId"] = "Category ID (optional)",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.AllocationUnits"] = "Allocation ceiling (units)",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.SpendCeiling"] = "Spend ceiling",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.SeedAllocation"] = "Seed allocation",
            ["Plugins.TwinParticles.CheckEngine.Portal.Admin.SeedQuota"] = "Seed quota",
            ["Plugins.TwinParticles.CheckEngine.Portal.Error.Unauthenticated"] = "Log in to access this portal.",
            ["Plugins.TwinParticles.CheckEngine.Portal.Error.NotProvisioned"] = "No trade account is linked to your customer profile. Ask your operator to provision one.",
            ["Plugins.TwinParticles.CheckEngine.Portal.Error.Generic"] = "Unable to load the portal dashboard.",
            ["Plugins.TwinParticles.CheckEngine.Licence.Panel.Title"] = "Licence & entitlements",
            ["Plugins.TwinParticles.CheckEngine.Licence.Panel.Hint"] = "Activate a signed ce-lic-v1 bundle. Enterprise unlocks fleet and dealer portals.",
            ["Plugins.TwinParticles.CheckEngine.Licence.Panel.Key"] = "Licence key",
            ["Plugins.TwinParticles.CheckEngine.Licence.Panel.Activate"] = "Activate",
            ["Plugins.TwinParticles.CheckEngine.Licence.Panel.Heartbeat"] = "Record heartbeat",
            ["Plugins.TwinParticles.CheckEngine.Licence.Panel.Status"] = "Status",
            ["Plugins.TwinParticles.CheckEngine.Dashboard.PortalLinks"] = "Vertical portals"
        };

        // English is the safe default for every installed language. Arabic-specific values then
        // override the complete key set for every Arabic culture configured in the store.
        await _localizationService.AddOrUpdateLocaleResourceAsync(englishResources);
        var arabicResources = ArabicResources();
        var languages = await _languageService.GetAllLanguagesAsync(showHidden: true);
        foreach (var language in languages.Where(language =>
                     language.LanguageCulture.StartsWith("ar", StringComparison.OrdinalIgnoreCase)))
        {
            await _localizationService.AddOrUpdateLocaleResourceAsync(arabicResources, language.Id);
        }
    }

    private static Dictionary<string, string> ArabicResources() => new()
    {
        ["Plugins.TwinParticles.CheckEngine.General"] = "توافق قطع السيارات",
        ["Plugins.TwinParticles.CheckEngine.General.Enabled"] = "مفعّل",
        ["Plugins.TwinParticles.CheckEngine.General.Enabled.Hint"] = "يحدد ما إذا كانت خدمات توافق قطع السيارات مفعّلة.",
        ["Plugins.TwinParticles.CheckEngine.Configuration"] = "الإعدادات",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.Enabled"] = "مفعّل",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.Enabled.Hint"] = "تفعيل أو تعطيل إضافة توافق قطع السيارات.",
        ["Plugins.TwinParticles.CheckEngine.Configuration.General"] = "عام",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Ai"] = "منصة الذكاء الاصطناعي",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Ai.Hint"] = "إعداد بيانات الاعتماد والحدود وتفعيل الميزات. يبقى الذكاء الاصطناعي معطلاً حتى الإقرار بالإفصاح.",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Ai.DisclosureRequired"] = "يجب الإقرار بالإفصاح عن البيانات قبل تفعيل أي ميزة ذكاء اصطناعي.",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Ai.DisclosureSummary"] = "راجع فئات البيانات المرسلة إلى مزود الذكاء الاصطناعي قبل تفعيل الميزات أدناه.",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiProviderKind"] = "المزود",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingProviderKind"] = "مزود التضمين",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingProviderKind.SameAsCompletion"] = "مثل الإكمال (OpenAI مع Anthropic)",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingProviderKind.Deterministic"] = "حتمي (دون اتصال)",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingProviderKind.Hint"] = "لا يوفر Anthropic واجهة تضمين. وجّه البحث الدلالي إلى OpenAI/Azure أو استخدم متجهات حتمية.",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingBaseUrl"] = "عنوان أساس التضمين (اختياري)",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingApiKey"] = "مفتاح API للتضمين (اختياري)",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiBaseUrl"] = "عنوان الأساس",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiApiKey"] = "مفتاح API",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiModel"] = "نموذج الإكمال",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingModel"] = "نموذج التضمين",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiAzureDeploymentName"] = "اسم نشر Azure",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiAzureEmbeddingDeploymentName"] = "اسم نشر تضمين Azure",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiDailyTokenCeiling"] = "حد الرموز اليومي",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiTokenCostPer1KUsd"] = "التكلفة التقديرية لكل 1000 رمز (USD)",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiResponseCacheTtlMinutes"] = "مدة ذاكرة الاستجابة (دقائق)",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiDisclosureAcknowledged"] = "تم الإقرار بالإفصاح",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEnabledFeatures"] = "الميزات المفعّلة (CSV)",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiPerFeatureDailyTokenCeilings"] = "حدود يومية لكل ميزة",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiFeatures"] = "ميزات الذكاء الاصطناعي",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableImportEnrichment"] = "إثراء الاستيراد",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableImportTranslation"] = "ترجمة الاستيراد",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableImportSeo"] = "SEO للاستيراد",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableSearchNaturalLanguage"] = "بحث باللغة الطبيعية",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableSearchSemantic"] = "بحث دلالي",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableFitmentInference"] = "استنتاج التوافق",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableCustomerAssistant"] = "مساعد العملاء",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableRecommendations"] = "توصيات التوافق",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Marketplace"] = "السوق",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Marketplace.Hint"] = "تبقى طلبات الموردين مغلقة حتى تفعيل هذا الخيار ومع ترخيص يشمل صلاحية السوق.",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableMarketplace"] = "قبول طلبات الموردين",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.MarketplaceAgreementVersion"] = "إصدار اتفاقية المورد",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review"] = "تسجيل الموردين",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Queue"] = "الطلبات قيد المراجعة",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Hint"] = "لا تعتمد المورد إلا بعد التحقق وقبول الاتفاقية الحالية. بيانات البنك مشفّرة ولا تُعرض هنا.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply"] = "كن مورداً",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Eyebrow"] = "تسجيل في السوق",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Hero"] = "انضم إلى سوق قطع الغيار",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Hint"] = "قدّم ملف المنشأة والمعرفات الضريبية وبيانات الدفع. يبدأ العرض بعد المراجعة وقبول الاتفاقية.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Steps"] = "خطوات الطلب",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Step.Business"] = "ملف المنشأة",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Step.Payout"] = "بيانات الدفع",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Step.Review"] = "مراجعة المشغّل",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Section.Business"] = "معلومات المنشأة",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Section.Business.Hint"] = "الهوية القانونية والفئات التي تنوي توريدها.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Section.Payout"] = "الدفع والبنك",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Section.Payout.Hint"] = "مشفّرة عند التخزين. تُعرض للمشغّلين المخوّلين فقط أثناء الموافقة.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Submit"] = "إرسال الطلب",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Submitting"] = "جاري الإرسال…",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Success"] = "تم استلام الطلب",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Success.Hint"] = "طلبك قيد المراجعة. احفظ رمز الوصول أدناه للتحقق من الحالة أو قبول الاتفاقية قبل تسجيل الدخول.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.VendorId"] = "مرجع الطلب",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.AccessToken"] = "رمز وصول الضيف",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.AccessToken.Hint"] = "يُعرض مرة واحدة. مطلوب للتحقق من الحالة حتى ربط حسابك.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.CopyToken"] = "نسخ الرمز",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Copied"] = "تم نسخ الرمز",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Legal"] = "أدخل الاسم القانوني للمنشأة.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Email"] = "أدخل بريداً إلكترونياً صالحاً.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Tax"] = "أدخل معرفاً ضريبياً واحداً على الأقل كمصفوفة JSON.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Categories"] = "أدخل فئة واحدة على الأقل.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Banking"] = "أدخل بيانات الدفع / البنك.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Agreement"] = "اقبل اتفاقية المورد للمتابعة.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Submit"] = "تعذّر إرسال الطلب.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Apply.Error.Network"] = "خطأ في الشبكة. حاول مجدداً.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.TradingName.Hint"] = "اسم تجاري اختياري للعرض.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.TaxIds.Hint"] = "مصفوفة JSON، مثل [\"VAT-123\"]",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Categories.Hint"] = "مفصولة بفاصلة، مثل cooling,filters",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Banking.Hint"] = "IBAN وصاحب الحساب واسم البنك. لا تشاركها في الدردشة أو البريد.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.LegalName"] = "الاسم القانوني",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.TradingName"] = "الاسم التجاري",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Email"] = "البريد الإلكتروني",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.TaxIds"] = "المعرفات الضريبية (JSON)",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Categories"] = "الفئات",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Banking"] = "بيانات البنك / الدفع",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Agreement.Text"] = "بتقديم الطلب تقبل اتفاقية المورد الحالية. يُحفظ سجل قبول مُصدَّر.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Agreement.Accept"] = "أوافق على اتفاقية المورد",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard"] = "لوحة المورد",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Eyebrow"] = "مساحة المورد",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Hero"] = "لوحة تحكم السوق",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Hint"] = "أدِر مخزون المنتجات واطّلع على الطلبات وقدّم مقترحات التوافق وتابع الأداء.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Tabs"] = "أقسام اللوحة",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Tab.Overview"] = "نظرة عامة",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Loading"] = "جاري تحميل اللوحة…",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Products"] = "المنتجات",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.StockUnits"] = "وحدات المخزون",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.LowStock"] = "منخفض",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Orders"] = "الطلبات",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Orders.Hint"] = "الطلبات التي تحتوي على منتجات من كتالوجك.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Orders.Empty"] = "لا توجد طلبات بعد.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.OrderId"] = "الطلب",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.OrderTotal"] = "الإجمالي",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Catalog"] = "فهرس المنتجات",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Catalog.Hint"] = "معرّفات المنتجات المخصصة لحسابك.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Catalog.Empty"] = "لا منتجات في الفهرس بعد.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Dashboard.Customers"] = "العملاء",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Scorecard.Hint"] = "مقاييس أداء متجددة تُستخدم في لوحة المشغّل.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Scorecard"] = "بطاقة الأداء",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard"] = "لوحة أداء الموردين",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard.Hint"] = "عرض المشغّل لمعدلات التعبئة والإلغاء ورفض التوافق والشحن في الوقت.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Scorecard.FillRate"] = "معدل التعبئة",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Scorecard.CancelRate"] = "معدل الإلغاء",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Scorecard.ClaimRejectRate"] = "معدل رفض المطالبات",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Scorecard.OnTimeShipment"] = "الشحن في الوقت",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Inventory"] = "المخزون",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Inventory.Hint"] = "حدّث كميات المخزون لمنتجات فهرسك.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Inventory.Product"] = "المنتج",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Inventory.Stock"] = "كمية المخزون",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Inventory.Save"] = "حفظ",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Inventory.Saved"] = "تم تحديث المخزون",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Inventory.Empty"] = "لا صفوف مخزون بعد.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.Proposals"] = "مقترحات التوافق",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.Hint"] = "تدخل المقترحات قائمة مراجعة المشغّل. المطالبات المنشورة تظل مرئية للمتسوقين.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.ProductId"] = "معرّف المنتج",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.VehicleConfigId"] = "معرّف تكوين السيارة",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.ClaimId"] = "المطالبة",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.Status"] = "الحالة",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.Submit"] = "إرسال المقترح",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.Revoke"] = "سحب",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Fitment.Empty"] = "لا مقترحات توافق بعد.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Statements"] = "كشوف الحساب",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Statements.Hint"] = "استحقاقات العمولة وكشوف المدفوعات لحسابك.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Statements.Pending"] = "ستظهر كشوف العمولات والمدفوعات هنا عند تفعيل تسوية المدفوعات.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Statements.NetPayout"] = "صافي الدفع",
        ["Plugins.TwinParticles.CheckEngine.Commission.Configure"] = "خطة العمولة",
        ["Plugins.TwinParticles.CheckEngine.Commission.Configure.Hint"] = "تُقيَّم القواعد حسب الأولوية (الأقل أولاً). تجاوزات الفئة تسبق الافتراضي. تُثبَّت النسب عند إنشاء الطلب.",
        ["Plugins.TwinParticles.CheckEngine.Commission.Vendor"] = "المورد والخطة",
        ["Plugins.TwinParticles.CheckEngine.Commission.SelectVendor"] = "المورد",
        ["Plugins.TwinParticles.CheckEngine.Commission.PlanName"] = "اسم الخطة",
        ["Plugins.TwinParticles.CheckEngine.Commission.PlanActive"] = "الخطة نشطة",
        ["Plugins.TwinParticles.CheckEngine.Commission.Rules"] = "قواعد العمولة",
        ["Plugins.TwinParticles.CheckEngine.Commission.Rules.Hint"] = "أضف رسوماً ثابتة أو نسباً أو شرائح أو تجاوزات فئة. الأرقام الأقل أولوية تُنفَّذ أولاً.",
        ["Plugins.TwinParticles.CheckEngine.Commission.AddRule"] = "إضافة قاعدة",
        ["Plugins.TwinParticles.CheckEngine.Commission.AdvancedJson"] = "JSON متقدم",
        ["Plugins.TwinParticles.CheckEngine.Commission.AdvancedJson.Hint"] = "استيراد/تصدير للمستخدم المتقدم. يمكن تطبيق التغييرات على النموذج.",
        ["Plugins.TwinParticles.CheckEngine.Commission.ImportJson"] = "تطبيق JSON على النموذج",
        ["Plugins.TwinParticles.CheckEngine.Commission.Save"] = "حفظ خطة العمولة",
        ["Plugins.TwinParticles.CheckEngine.Payout.Admin"] = "كشوف المدفوعات",
        ["Plugins.TwinParticles.CheckEngine.Payout.Admin.Hint"] = "أنشئ كشوف المورد الدورية من العمولات المثبتة، اعتمدها، ادفعها إلى ERPNext، وسوِّ الت totals.",
        ["Plugins.TwinParticles.CheckEngine.Payout.SelectVendor"] = "المورد",
        ["Plugins.TwinParticles.CheckEngine.Payout.PeriodStart"] = "بداية الفترة",
        ["Plugins.TwinParticles.CheckEngine.Payout.PeriodEnd"] = "نهاية الفترة",
        ["Plugins.TwinParticles.CheckEngine.Payout.Generate"] = "إنشاء كشف",
        ["Plugins.TwinParticles.CheckEngine.Payout.Generating"] = "جارٍ الإنشاء…",
        ["Plugins.TwinParticles.CheckEngine.Payout.Generated"] = "تم إنشاء الكشف",
        ["Plugins.TwinParticles.CheckEngine.Payout.Statements"] = "الكشوف",
        ["Plugins.TwinParticles.CheckEngine.Payout.Details"] = "تفاصيل الكشف",
        ["Plugins.TwinParticles.CheckEngine.Payout.View"] = "عرض",
        ["Plugins.TwinParticles.CheckEngine.Payout.Empty"] = "لا توجد كشوف لهذا المورد بعد.",
        ["Plugins.TwinParticles.CheckEngine.Payout.SelectStatement"] = "اختر كشفاً من الجدول أولاً.",
        ["Plugins.TwinParticles.CheckEngine.Payout.Finalize"] = "اعتماد",
        ["Plugins.TwinParticles.CheckEngine.Payout.PushErp"] = "إرسال إلى ERP",
        ["Plugins.TwinParticles.CheckEngine.Payout.Reconcile"] = "مطابقة",
        ["Plugins.TwinParticles.CheckEngine.Payout.ErpReference"] = "مرجع ERP",
        ["Plugins.TwinParticles.CheckEngine.Payout.Col.Id"] = "المعرف",
        ["Plugins.TwinParticles.CheckEngine.Payout.Col.Period"] = "الفترة",
        ["Plugins.TwinParticles.CheckEngine.Payout.Col.Gross"] = "إجمالي المبيعات",
        ["Plugins.TwinParticles.CheckEngine.Payout.Col.Commission"] = "العمولة",
        ["Plugins.TwinParticles.CheckEngine.Payout.Col.Refunds"] = "المستردات",
        ["Plugins.TwinParticles.CheckEngine.Payout.Col.Adjustments"] = "التسويات",
        ["Plugins.TwinParticles.CheckEngine.Payout.Col.Status"] = "الحالة",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Admin.Loading"] = "جارٍ التحميل…",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Admin.ErrorLoad"] = "تعذر تحميل البيانات.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Admin.ActionOk"] = "تم الحفظ بنجاح.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Admin.ActionFail"] = "فشل الإجراء.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Empty"] = "لا توجد طلبات في قائمة المراجعة.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Action.Review"] = "وضع قيد المراجعة",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Action.Approve"] = "اعتماد",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Action.Reject"] = "رفض",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.RejectNotes"] = "ملاحظات الرفض (اختياري):",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Col.Id"] = "المعرف",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Col.LegalName"] = "الاسم القانوني",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Col.Email"] = "البريد",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Col.Categories"] = "الفئات",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Col.Status"] = "الحالة",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Col.Agreement"] = "الاتفاقية",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Review.Col.Banking"] = "البنك",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard.Vendors"] = "الموردون المتتبعون",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard.OrdersSample"] = "الطلبات في العينة",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard.Empty"] = "لا توجد بيانات بطاقة أداء بعد.",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard.Col.Vendor"] = "المورد",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard.Col.Orders"] = "الطلبات",
        ["Plugins.TwinParticles.CheckEngine.Marketplace.Statements.None"] = "لا توجد كشوف مدفوعات لهذا المورد بعد.",
        ["Plugins.TwinParticles.CheckEngine.Dashboard"] = "لوحة تحكم توافق قطع السيارات",
        ["Plugins.TwinParticles.CheckEngine.Dashboard.AdminLinks"] = "واجهات إدارة JSON",
        ["Plugins.TwinParticles.CheckEngine.Dashboard.ImportUpload"] = "رفع ملف الاستيراد",
        ["Plugins.TwinParticles.CheckEngine.Dashboard.ImportUpload.Hint"] = "اختر ملف المورد وشغّل مسار الاستيراد.",
        ["Plugins.TwinParticles.CheckEngine.Garage.Label"] = "مرآبي",
        ["Plugins.TwinParticles.CheckEngine.Garage.SelectVehicle"] = "اختر السيارة",
        ["Plugins.TwinParticles.CheckEngine.Garage.Empty"] = "لم يتم اختيار سيارة",
        ["Plugins.TwinParticles.CheckEngine.Garage.AddVehicle"] = "أضف سيارة برقم الهيكل…",
        ["Plugins.TwinParticles.CheckEngine.Garage.VehicleSelector"] = "اختر السيارة للتحقق من توافق القطع",
        ["Plugins.TwinParticles.CheckEngine.Garage.VinPrompt"] = "أدخل رقم الهيكل لإضافة سيارة",
        ["Plugins.TwinParticles.CheckEngine.Garage.AddFailed"] = "تعذرت إضافة هذه السيارة.",
        ["Plugins.TwinParticles.CheckEngine.Garage.VinDisambiguation.Title"] = "أي سيارة هي هذه؟",
        ["Plugins.TwinParticles.CheckEngine.Garage.VinDisambiguation.Lead"] = "رقم الهيكل يطابق أكثر من إعداد للسيارة. اختر الإعداد الذي يطابق سيارتك.",
        ["Plugins.TwinParticles.CheckEngine.Garage.VinDisambiguation.Cancel"] = "إلغاء",
        ["Plugins.TwinParticles.CheckEngine.Garage.VinDisambiguation.Year"] = "سنة الموديل",
        ["Plugins.TwinParticles.CheckEngine.Search.Label"] = "ابحث عن قطعة أو رقم OEM أو رقم هيكل",
        ["Plugins.TwinParticles.CheckEngine.Search.Placeholder"] = "ابحث عن قطعة أو OEM أو VIN",
        ["Plugins.TwinParticles.CheckEngine.Search.Submit"] = "بحث",
        ["Plugins.TwinParticles.CheckEngine.Search.Widen"] = "تضمين التوافق غير المؤكد",
        ["Plugins.TwinParticles.CheckEngine.Search.Hint"] = "أدخل كلمة بحث أو رقم قطعة OEM أو رقم هيكل من 17 خانة.",
        ["Plugins.TwinParticles.CheckEngine.Search.ResultsLabel"] = "نتائج بحث توافق القطع",
        ["Plugins.TwinParticles.CheckEngine.Search.ResultsCount"] = "نتيجة",
        ["Plugins.TwinParticles.CheckEngine.Search.ParsedIntent"] = "فُسِّر كـ",
        ["Plugins.TwinParticles.CheckEngine.Search.Mode"] = "الوضع",
        ["Plugins.TwinParticles.CheckEngine.Search.Mode.Auto"] = "تلقائي",
        ["Plugins.TwinParticles.CheckEngine.Search.Mode.NaturalLanguage"] = "لغة طبيعية",
        ["Plugins.TwinParticles.CheckEngine.Search.Mode.Semantic"] = "دلالي",
        ["Plugins.TwinParticles.CheckEngine.Search.Admin"] = "إدارة البحث",
        ["Plugins.TwinParticles.CheckEngine.Search.Admin.KeywordIndex"] = "فهرس الكلمات المفتاحية",
        ["Plugins.TwinParticles.CheckEngine.Search.Admin.RebuildKeyword"] = "إعادة بناء فهرس الكلمات",
        ["Plugins.TwinParticles.CheckEngine.Search.Admin.RebuildKeywordAndEmbeddings"] = "إعادة بناء الكلمات وتحديث التضمينات",
        ["Plugins.TwinParticles.CheckEngine.Search.Admin.Embeddings"] = "التضمينات الدلالية",
        ["Plugins.TwinParticles.CheckEngine.Search.Admin.RebuildEmbeddingsAll"] = "إعادة بناء كل اللغات",
        ["Plugins.TwinParticles.CheckEngine.Search.Admin.RefreshEmbeddings"] = "تحديث التضمينات المتغيرة",
        ["Plugins.TwinParticles.CheckEngine.Search.Synonyms"] = "مرادفات البحث ثنائية اللغة",
        ["Plugins.TwinParticles.CheckEngine.Search.Synonyms.Hint"] = "أزواج AR→EN للبحث الدلالي وتضمين الفهرس. التعديلات تُسجّل في التدقيق.",
        ["Plugins.TwinParticles.CheckEngine.Search.Synonyms.Arabic"] = "المصطلح العربي",
        ["Plugins.TwinParticles.CheckEngine.Search.Synonyms.English"] = "المصطلح الإنجليزي",
        ["Plugins.TwinParticles.CheckEngine.Search.Synonyms.Source"] = "المصدر",
        ["Plugins.TwinParticles.CheckEngine.Search.Synonyms.OverridesJson"] = "مصطلحات مخصصة (JSON، مفاتيح عربية)",
        ["Plugins.TwinParticles.CheckEngine.Search.Synonyms.Save"] = "حفظ مرادفات البحث",
        ["Plugins.TwinParticles.CheckEngine.Search.Empty.Title"] = "لم يتم العثور على قطع مطابقة",
        ["Plugins.TwinParticles.CheckEngine.Search.Empty.Hint"] = "تحقق من رقم OEM أو VIN أو وسّع نطاق التوافق.",
        ["Plugins.TwinParticles.CheckEngine.Search.Unavailable"] = "البحث غير متاح حالياً.",
        ["Plugins.TwinParticles.CheckEngine.Search.Facets.Title"] = "تصفية",
        ["Plugins.TwinParticles.CheckEngine.Search.Facets.Category"] = "الفئة",
        ["Plugins.TwinParticles.CheckEngine.Search.Facets.Brand"] = "العلامة التجارية",
        ["Plugins.TwinParticles.CheckEngine.Search.Facets.Price"] = "السعر",
        ["Plugins.TwinParticles.CheckEngine.Search.Facets.Fitment"] = "التوافق",
        ["Plugins.TwinParticles.CheckEngine.Search.Facets.Clear"] = "مسح عوامل التصفية",
        ["Plugins.TwinParticles.CheckEngine.Search.Recovery.Title"] = "جرّب أحد هذه الخيارات",
        ["Plugins.TwinParticles.CheckEngine.Search.Suggest.Vehicles"] = "السيارات",
        ["Plugins.TwinParticles.CheckEngine.Search.Suggest.Oems"] = "أرقام OEM",
        ["Plugins.TwinParticles.CheckEngine.Search.Suggest.Products"] = "المنتجات",
        ["Plugins.TwinParticles.CheckEngine.Menu.AriaLabel"] = "تصفح قطع الغيار",
        ["Plugins.TwinParticles.CheckEngine.Menu.AllParts"] = "كل قطع الغيار",
        ["Plugins.TwinParticles.CheckEngine.Menu.Eyebrow"] = "كتالوج القطع",
        ["Plugins.TwinParticles.CheckEngine.Menu.Title"] = "تصفح حسب الفئة",
        ["Plugins.TwinParticles.CheckEngine.Menu.Hint"] = "اختر فئة أو أضف سيارتك لعرض القطع المتوافقة أولاً.",
        ["Plugins.TwinParticles.CheckEngine.Menu.Empty"] = "ستظهر الفئات هنا عند نشر الكتالوج.",
        ["Plugins.TwinParticles.CheckEngine.Menu.AddVehicle"] = "أضف سيارتك",
        ["Plugins.TwinParticles.CheckEngine.Menu.SearchByOem"] = "ابحث برقم OEM",
        ["Plugins.TwinParticles.CheckEngine.Fitment.Fits"] = "متوافق مع سيارتك",
        ["Plugins.TwinParticles.CheckEngine.Fitment.Fits.Hint"] = "تم التحقق من التوافق مع سيارتك النشطة.",
        ["Plugins.TwinParticles.CheckEngine.Fitment.DoesNotFit"] = "غير متوافق",
        ["Plugins.TwinParticles.CheckEngine.Fitment.DoesNotFit.Hint"] = "هذه القطعة غير متوافقة مع سيارتك النشطة.",
        ["Plugins.TwinParticles.CheckEngine.Fitment.Unknown"] = "التوافق غير معروف",
        ["Plugins.TwinParticles.CheckEngine.Fitment.Unknown.Hint"] = "لم نتمكن من التحقق من توافق هذه القطعة مع سيارتك.",
        ["Plugins.TwinParticles.CheckEngine.Fitment.NeedsDetail"] = "نحتاج تفاصيل إضافية عن السيارة",
        ["Plugins.TwinParticles.CheckEngine.Fitment.NeedsDetail.Hint"] = "القطعة تناسب بعض فئات سيارتك. أضف التفاصيل الناقصة للتأكيد.",
        ["Plugins.TwinParticles.CheckEngine.Fitment.NeedsDetail.Cta"] = "أكمل تفاصيل السيارة",
        ["Plugins.TwinParticles.CheckEngine.Fitment.SelectVehicle"] = "اختر سيارتك",
        ["Plugins.TwinParticles.CheckEngine.Fitment.SelectVehicle.Hint"] = "اختر سيارة للتحقق من توافق هذه القطعة.",
        ["Plugins.TwinParticles.CheckEngine.Fitment.SelectVehicle.Cta"] = "أضف سيارتك",
        ["Plugins.TwinParticles.CheckEngine.Hero.Eyebrow"] = "توافق قطع السيارات",
        ["Plugins.TwinParticles.CheckEngine.Hero.Title"] = "اعثر على القطعة المناسبة لسيارتك",
        ["Plugins.TwinParticles.CheckEngine.Hero.Lead"] = "ابحث برقم الهيكل أو OEM أو كلمة بحث. احفظ سيارتك وسنتحقق من كل نتيجة.",
        ["Plugins.TwinParticles.CheckEngine.Hero.PrimaryCta"] = "ابحث عن قطع",
        ["Plugins.TwinParticles.CheckEngine.Hero.SecondaryCta"] = "أضف سيارتك",
        ["Plugins.TwinParticles.CheckEngine.L10n.Number"] = "تنسيق الأرقام",
        ["Plugins.TwinParticles.CheckEngine.L10n.Date"] = "تنسيق التاريخ",
        ["Plugins.TwinParticles.CheckEngine.L10n.Unit"] = "تنسيق الوحدات",
        ["Plugins.TwinParticles.CheckEngine.L10n.Preview"] = "معاينة الترجمة",
        ["Plugins.TwinParticles.CheckEngine.Licence.Status"] = "حالة الترخيص",
        ["Plugins.TwinParticles.CheckEngine.Licence.LastHeartbeat"] = "آخر تحقق",
        ["Plugins.TwinParticles.CheckEngine.Licence.ActivationKey"] = "مفتاح التفعيل",
        ["Plugins.TwinParticles.CheckEngine.Uninstall.Warning"] = "يزيل إلغاء التثبيت نهائياً بيانات المركبات وOEM والتوافق والمرآب والتحليلات والتدقيق والتكامل الخاصة بتوافق قطع السيارات.",
        ["Plugins.TwinParticles.CheckEngine.Uninstall.ExportRequired"] = "نزّل تصديراً حديثاً قبل إلغاء التثبيت. يُحظر الإلغاء ما لم يُجهَّز التصدير خلال آخر 24 ساعة.",
        ["Plugins.TwinParticles.CheckEngine.Uninstall.ExportPrepared"] = "يوجد تصدير حديث. يمكنك متابعة إلغاء التثبيت حتى انتهاء صلاحيته.",
        ["Plugins.TwinParticles.CheckEngine.Uninstall.ExportExpired"] = "انتهت صلاحية آخر تصدير. نزّل تصديراً جديداً قبل إلغاء التثبيت.",
        ["Plugins.TwinParticles.CheckEngine.Uninstall.ExportAction"] = "تنزيل تصدير إلغاء التثبيت",
        ["Plugins.TwinParticles.CheckEngine.Uninstall.ConfirmTitle"] = "إلغاء تثبيت توافق قطع السيارات؟",
        ["Plugins.TwinParticles.CheckEngine.Uninstall.ConfirmBody"] = "سيؤدي ذلك إلى إزالة كل جداول وإعدادات وموارد وجداول المهام الخاصة بتوافق قطع السيارات. تبقى طلبات الكتالوج الأساسية، لكن بيانات المركبات وOEM والتوافق تُحذف.",
        ["Plugins.TwinParticles.CheckEngine.Uninstall.ConfirmProceed"] = "متابعة إلغاء التثبيت",
        ["Plugins.TwinParticles.CheckEngine.Uninstall.ConfirmCancel"] = "إلغاء",
        ["Plugins.TwinParticles.CheckEngine.Ai.Review"] = "مراجعة الذكاء الاصطناعي",
        ["Plugins.TwinParticles.CheckEngine.Ai.Review.Candidates"] = "مرشحات المحتوى",
        ["Plugins.TwinParticles.CheckEngine.Ai.Review.Hint"] = "وافق أو ارفض أوصاف وترجمات وSEO المولّدة. لا يُنشر شيء قبل المراجعة.",
        ["Plugins.TwinParticles.CheckEngine.Ai.Review.Fitment"] = "مرشحات توافق الذكاء الاصطناعي",
        ["Plugins.TwinParticles.CheckEngine.Glossary.Admin"] = "معجم قطع الغيار",
        ["Plugins.TwinParticles.CheckEngine.Glossary.Hint"] = "مفردات EN→AR للترجمة الآلية. التعديلات تُدمج مع المعجم المضمّن وتُسجّل في التدقيق.",
        ["Plugins.TwinParticles.CheckEngine.Glossary.English"] = "المصطلح الإنجليزي",
        ["Plugins.TwinParticles.CheckEngine.Glossary.Arabic"] = "المصطلح العربي",
        ["Plugins.TwinParticles.CheckEngine.Glossary.Source"] = "المصدر",
        ["Plugins.TwinParticles.CheckEngine.Glossary.OverridesJson"] = "مصطلحات مخصصة (JSON)",
        ["Plugins.TwinParticles.CheckEngine.SpecKeys.Admin"] = "مفاتيح المواصفات",
        ["Plugins.TwinParticles.CheckEngine.SpecKeys.Hint"] = "مفاتيح السمات المسموح بها لاستخراج المواصفات بالذكاء الاصطناعي. أضف مفاتيح مخصصة أو احذف المضمّنة؛ التغييرات تُسجّل وتؤثر على التحقق من المرشحات.",
        ["Plugins.TwinParticles.CheckEngine.SpecKeys.Key"] = "مفتاح السمة",
        ["Plugins.TwinParticles.CheckEngine.SpecKeys.Source"] = "المصدر",
        ["Plugins.TwinParticles.CheckEngine.SpecKeys.OverridesJson"] = "تعديلات (JSON بمصفوفات add/remove)",
        ["Plugins.TwinParticles.CheckEngine.Ai.Dashboard"] = "لوحة استخدام الذكاء الاصطناعي",
        ["Plugins.TwinParticles.CheckEngine.Ai.Dashboard.Usage"] = "الاستخدام حسب الميزة",
        ["Plugins.TwinParticles.CheckEngine.Ai.AcknowledgeDisclosure"] = "الإقرار بالإفصاح",
        ["Plugins.TwinParticles.CheckEngine.Fitment.AiReview"] = "مراجعة توافق الذكاء الاصطناعي",
        ["Plugins.TwinParticles.CheckEngine.Fitment.AiReview.Infer"] = "تشغيل استنتاج التوافق",
        ["Plugins.TwinParticles.CheckEngine.Fitment.AiReview.Hint"] = "مطالبات الذكاء الاصطناعي تبقى غير منشورة حتى يوافق المراجع. الموافقة ترفع المصدر إلى المنسق اليدوي.",
        ["Plugins.TwinParticles.CheckEngine.Assistant.Title"] = "مساعد القطع",
        ["Plugins.TwinParticles.CheckEngine.Assistant.Placeholder"] = "اسأل عن قطعة في الكتالوج…",
        ["Plugins.TwinParticles.CheckEngine.Assistant.Send"] = "اسأل",
        ["Plugins.TwinParticles.CheckEngine.Assistant.Unavailable"] = "المساعد غير متاح حالياً.",
        ["Plugins.TwinParticles.CheckEngine.Assistant.Open"] = "افتح مساعد القطع",
        ["Plugins.TwinParticles.CheckEngine.Assistant.Sources"] = "مصادر الكتالوج",
        ["Plugins.TwinParticles.CheckEngine.Assistant.Thinking"] = "جاري البحث في الكتالوج…",
        ["Plugins.TwinParticles.CheckEngine.Assistant.RateLimited"] = "أسئلة كثيرة — انتظر قليلاً ثم حاول مرة أخرى.",
        ["Plugins.TwinParticles.CheckEngine.Assistant.VehicleScoped"] = "الإجابات مقتصرة على القطع المُتحقق توافقها مع سيارتك النشطة.",
        ["Plugins.TwinParticles.CheckEngine.Assistant.VehicleUnscoped"] = "اختر سيارة لتصفية الإجابات إلى القطع المُتحقق توافقها فقط.",
        ["Plugins.TwinParticles.CheckEngine.Recommend.Title"] = "يناسب سيارتك أيضاً",
        ["Plugins.TwinParticles.CheckEngine.Recommend.UnscopedTitle"] = "قد يعجبك أيضاً",
        ["Plugins.TwinParticles.CheckEngine.Recommend.FitmentBadge"] = "يناسب سيارتك",
        ["Plugins.TwinParticles.CheckEngine.Portal.Loading"] = "جاري تحميل البوابة…",
        ["Plugins.TwinParticles.CheckEngine.Workshop.Title"] = "بوابة الورشة",
        ["Plugins.TwinParticles.CheckEngine.Workshop.Eyebrow"] = "مساحة عمل التجارة",
        ["Plugins.TwinParticles.CheckEngine.Workshop.Hero"] = "الطلبات القائمة على الأعمال",
        ["Plugins.TwinParticles.CheckEngine.Workshop.Hint"] = "أنشئ أعمالاً، خصّص قطعاً مع فحوصات التوافق، واصدر فواتير على الائتمان التجاري — دون سلة تجزئة.",
        ["Plugins.TwinParticles.CheckEngine.Workshop.CreateJob"] = "إنشاء عمل",
        ["Plugins.TwinParticles.CheckEngine.Workshop.Jobs"] = "الأعمال",
        ["Plugins.TwinParticles.CheckEngine.Workshop.JobId"] = "العمل",
        ["Plugins.TwinParticles.CheckEngine.Workshop.Status"] = "الحالة",
        ["Plugins.TwinParticles.CheckEngine.Workshop.LabourEstimate"] = "تقدير العمالة",
        ["Plugins.TwinParticles.CheckEngine.Workshop.OrderId"] = "الطلب",
        ["Plugins.TwinParticles.CheckEngine.Workshop.VehicleConfigId"] = "تكوين المركبة",
        ["Plugins.TwinParticles.CheckEngine.Workshop.JobDetail"] = "تفاصيل العمل",
        ["Plugins.TwinParticles.CheckEngine.Workshop.Vehicles"] = "المركبات في العمل",
        ["Plugins.TwinParticles.CheckEngine.Workshop.Lines"] = "البنود المخصصة",
        ["Plugins.TwinParticles.CheckEngine.Workshop.Fitment"] = "التوافق",
        ["Plugins.TwinParticles.CheckEngine.Workshop.UnitPrice"] = "سعر الوحدة",
        ["Plugins.TwinParticles.CheckEngine.Workshop.SelectJob"] = "اختر عملاً لعرض التفاصيل.",
        ["Plugins.TwinParticles.CheckEngine.Fleet.Title"] = "بوابة الأسطول",
        ["Plugins.TwinParticles.CheckEngine.Fleet.Eyebrow"] = "عمليات الأسطول",
        ["Plugins.TwinParticles.CheckEngine.Fleet.Hero"] = "التسجيل والموافقة على نطاق واسع",
        ["Plugins.TwinParticles.CheckEngine.Fleet.Hint"] = "استورد أرقام الهيكل بالجملة إلى سجل الأسطول ووجّه المشتريات عبر موافقات مراكز الميزانية.",
        ["Plugins.TwinParticles.CheckEngine.Fleet.ImportVins"] = "استيراد أرقام الهيكل",
        ["Plugins.TwinParticles.CheckEngine.Fleet.VinList"] = "قائمة أرقام الهيكل (مفصولة بمسافات أو أسطر)",
        ["Plugins.TwinParticles.CheckEngine.Fleet.Vehicles"] = "سجل الأسطول",
        ["Plugins.TwinParticles.CheckEngine.Fleet.AssetTag"] = "وسم الأصل",
        ["Plugins.TwinParticles.CheckEngine.Fleet.Approvals"] = "قائمة الموافقات",
        ["Plugins.TwinParticles.CheckEngine.Fleet.MaintenanceForecast"] = "توقعات الصيانة",
        ["Plugins.TwinParticles.CheckEngine.Fleet.VehicleId"] = "المركبة",
        ["Plugins.TwinParticles.CheckEngine.Fleet.Service"] = "الخدمة",
        ["Plugins.TwinParticles.CheckEngine.Fleet.DueDate"] = "تاريخ الاستحقاق",
        ["Plugins.TwinParticles.CheckEngine.Dealer.Title"] = "بوابة الوكيل",
        ["Plugins.TwinParticles.CheckEngine.Dealer.Eyebrow"] = "مشتري الامتياز",
        ["Plugins.TwinParticles.CheckEngine.Dealer.Hero"] = "طلبات واعية بالتخصيص",
        ["Plugins.TwinParticles.CheckEngine.Dealer.Hint"] = "تصفح كتالوج نطاق الامتياز، ضع طلبات ضمن الحصة، وقدّم مطالبات الضمان.",
        ["Plugins.TwinParticles.CheckEngine.Dealer.Catalog"] = "كتالوج الوكيل",
        ["Plugins.TwinParticles.CheckEngine.Dealer.ProductId"] = "المنتج",
        ["Plugins.TwinParticles.CheckEngine.Dealer.ProductName"] = "الاسم",
        ["Plugins.TwinParticles.CheckEngine.Dealer.Price"] = "سعر الوكيل",
        ["Plugins.TwinParticles.CheckEngine.Dealer.Quantity"] = "الكمية",
        ["Plugins.TwinParticles.CheckEngine.Dealer.PlaceOrder"] = "تقديم الطلب",
        ["Plugins.TwinParticles.CheckEngine.Dealer.WarrantyClaim"] = "مطالبات الضمان",
        ["Plugins.TwinParticles.CheckEngine.Dealer.SubmitClaim"] = "تقديم مطالبة",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.Title"] = "حسابات البوابات العمودية",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.Hint"] = "أنشئ حسابات ورشة أو أسطول أو وكيل لعميل nopCommerce. يتطلب تراخيص البوابات.",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.CustomerId"] = "معرّف العميل",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.DisplayName"] = "الاسم المعروض",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.ProvisionWorkshop"] = "إنشاء حساب ورشة",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.ProvisionFleet"] = "إنشاء حساب أسطول",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.ProvisionDealer"] = "إنشاء حساب وكيل",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.DealerSeeding"] = "تخصيص الوكيل والحصة",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.DealerSeeding.Hint"] = "أنشئ تخصيصات منتج أو فئة وحصص إنفاق لحساب وكيل موجود.",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.DealerAccountId"] = "معرّف حساب الوكيل",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.CategoryId"] = "معرّف الفئة (اختياري)",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.AllocationUnits"] = "سقف التخصيص (وحدات)",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.SpendCeiling"] = "سقف الإنفاق",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.SeedAllocation"] = "إنشاء تخصيص",
        ["Plugins.TwinParticles.CheckEngine.Portal.Admin.SeedQuota"] = "إنشاء حصة",
        ["Plugins.TwinParticles.CheckEngine.Portal.Error.Unauthenticated"] = "سجّل الدخول للوصول إلى هذه البوابة.",
        ["Plugins.TwinParticles.CheckEngine.Portal.Error.NotProvisioned"] = "لا يوجد حساب تجاري مرتبط بملفك. اطلب من المشغّل إنشاء حساب.",
        ["Plugins.TwinParticles.CheckEngine.Portal.Error.Generic"] = "تعذّر تحميل لوحة البوابة.",
        ["Plugins.TwinParticles.CheckEngine.Licence.Panel.Title"] = "الترخيص والصلاحيات",
        ["Plugins.TwinParticles.CheckEngine.Licence.Panel.Hint"] = "فعّل حزمة ce-lic-v1 الموقّعة. إصدار Enterprise يفتح بوابات الأسطول والوكيل.",
        ["Plugins.TwinParticles.CheckEngine.Licence.Panel.Key"] = "مفتاح الترخيص",
        ["Plugins.TwinParticles.CheckEngine.Licence.Panel.Activate"] = "تفعيل",
        ["Plugins.TwinParticles.CheckEngine.Licence.Panel.Heartbeat"] = "تسجيل نبضة",
        ["Plugins.TwinParticles.CheckEngine.Licence.Panel.Status"] = "الحالة",
        ["Plugins.TwinParticles.CheckEngine.Dashboard.PortalLinks"] = "البوابات العمودية",
    };

    public override async Task UpdateAsync(string currentVersion, string targetVersion)
    {
        // Apply every pending migration, not only Update-typed ones. All Check Engine migrations are
        // tagged Installation (per convention), and ApplyUpMigrations(..., Update) filters those out,
        // so a version bump that ships new schema would otherwise apply nothing. NoMatter runs all
        // unapplied migrations regardless of process type.
        _migrationManager.ApplyUpMigrations(MigrationAssembly, MigrationProcessType.NoMatter);
        await EnsureScheduleTasksAsync();
        await AddOrUpdateLocaleResourcesAsync();
        await EnsureWidgetActiveAsync();
        await _vehicleSeedLoader.SeedAsync(default);
        await base.UpdateAsync(currentVersion, targetVersion);
    }

    public override async Task UninstallAsync()
    {
        var settings = await _settingService.LoadSettingAsync<CheckEnginePluginSettings>();
        if (!settings.UninstallExportPreparedUtc.HasValue ||
            DateTime.UtcNow - settings.UninstallExportPreparedUtc.Value > TimeSpan.FromHours(24))
        {
            throw new InvalidOperationException(
                "Check Engine uninstall blocked: download a fresh export from " +
                "/Admin/CheckEngine/UninstallAdmin/Export before retrying. The export authorization expires after 24 hours.");
        }

        var erpTask = await _scheduleTaskService.GetTaskByTypeAsync(typeof(Tasks.ErpSyncQueueTask).FullName!);
        if (erpTask is not null)
            await _scheduleTaskService.DeleteTaskAsync(erpTask);
        var auditTask = await _scheduleTaskService.GetTaskByTypeAsync(typeof(Tasks.AuditRetentionTask).FullName!);
        if (auditTask is not null)
            await _scheduleTaskService.DeleteTaskAsync(auditTask);
        var licenceTask = await _scheduleTaskService.GetTaskByTypeAsync(typeof(Tasks.LicenceHeartbeatTask).FullName!);
        if (licenceTask is not null)
            await _scheduleTaskService.DeleteTaskAsync(licenceTask);
        var reconciliationTask = await _scheduleTaskService.GetTaskByTypeAsync(typeof(Tasks.ErpReconciliationTask).FullName!);
        if (reconciliationTask is not null)
            await _scheduleTaskService.DeleteTaskAsync(reconciliationTask);
        var searchIndexTask = await _scheduleTaskService.GetTaskByTypeAsync(typeof(Tasks.SearchIndexRefreshTask).FullName!);
        if (searchIndexTask is not null)
            await _scheduleTaskService.DeleteTaskAsync(searchIndexTask);
        var searchEmbeddingTask = await _scheduleTaskService.GetTaskByTypeAsync(typeof(Tasks.SearchEmbeddingRefreshTask).FullName!);
        if (searchEmbeddingTask is not null)
            await _scheduleTaskService.DeleteTaskAsync(searchEmbeddingTask);

        await DeactivateWidgetAsync();
        await _permissionService.DeletePermissionAsync(CheckEnginePermissionProvider.ManageCheckEngineDealer.SystemName);
        await _permissionService.DeletePermissionAsync(CheckEnginePermissionProvider.ManageCheckEngineFleet.SystemName);
        await _permissionService.DeletePermissionAsync(CheckEnginePermissionProvider.ManageCheckEngineWorkshop.SystemName);
        await _permissionService.DeletePermissionAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);
        await _settingService.DeleteSettingAsync<CheckEnginePluginSettings>();
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.TwinParticles.CheckEngine");

        _migrationManager.ApplyDownMigrations(MigrationAssembly);

        await base.UninstallAsync();
    }

    private async Task EnsureScheduleTasksAsync()
    {
        await EnsureScheduleTaskAsync(
            typeof(Tasks.ErpSyncQueueTask).FullName!,
            "Check Engine ERP synchronization queue",
            5 * 60);
        await EnsureScheduleTaskAsync(
            typeof(Tasks.AuditRetentionTask).FullName!,
            "Check Engine audit retention",
            24 * 60 * 60);
        await EnsureScheduleTaskAsync(
            typeof(Tasks.LicenceHeartbeatTask).FullName!,
            "Check Engine licence heartbeat",
            24 * 60 * 60);
        await EnsureScheduleTaskAsync(
            typeof(Tasks.ErpReconciliationTask).FullName!,
            "Check Engine ERP reconciliation",
            24 * 60 * 60);
        await EnsureScheduleTaskAsync(
            typeof(Tasks.SearchIndexRefreshTask).FullName!,
            "Check Engine search index refresh",
            60);
        await EnsureScheduleTaskAsync(
            typeof(Tasks.SearchEmbeddingRefreshTask).FullName!,
            "Check Engine search embedding refresh",
            15 * 60);
    }

    private async Task EnsureWidgetActiveAsync()
    {
        var widgetSettings = await _settingService.LoadSettingAsync<WidgetSettings>();
        var systemName = PluginDescriptor.SystemName;
        if (widgetSettings.ActiveWidgetSystemNames.Contains(systemName, StringComparer.OrdinalIgnoreCase))
            return;

        widgetSettings.ActiveWidgetSystemNames.Add(systemName);
        await _settingService.SaveSettingAsync(widgetSettings);
    }

    private async Task DeactivateWidgetAsync()
    {
        var widgetSettings = await _settingService.LoadSettingAsync<WidgetSettings>();
        var removed = widgetSettings.ActiveWidgetSystemNames.RemoveAll(name =>
            string.Equals(name, PluginDescriptor.SystemName, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
            await _settingService.SaveSettingAsync(widgetSettings);
    }

    private async Task EnsureScheduleTaskAsync(string type, string name, int seconds)
    {
        if (await _scheduleTaskService.GetTaskByTypeAsync(type) is not null)
            return;

        await _scheduleTaskService.InsertTaskAsync(new ScheduleTask
        {
            Name = name,
            Type = type,
            Seconds = seconds,
            Enabled = true,
            LastEnabledUtc = DateTime.UtcNow
        });
    }
}
