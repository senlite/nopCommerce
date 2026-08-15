using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Nop.Core;
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

    public CheckEnginePlugin(ILocalizationService localizationService,
        ILanguageService languageService,
        IMigrationManager migrationManager,
        IPermissionService permissionService,
        IScheduleTaskService scheduleTaskService,
        ISettingService settingService,
        IWebHelper webHelper)
    {
        _localizationService = localizationService;
        _languageService = languageService;
        _migrationManager = migrationManager;
        _permissionService = permissionService;
        _scheduleTaskService = scheduleTaskService;
        _settingService = settingService;
        _webHelper = webHelper;
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
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiProviderKind"] = "Provider",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiBaseUrl"] = "Base URL",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiApiKey"] = "API key",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiModel"] = "Completion model",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingModel"] = "Embedding model",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiAzureDeploymentName"] = "Azure deployment name",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiAzureEmbeddingDeploymentName"] = "Azure embedding deployment name",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiDailyTokenCeiling"] = "Daily token ceiling",
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
            ["Plugins.TwinParticles.CheckEngine.Recommend.Title"] = "Also fits your vehicle",
            ["Plugins.TwinParticles.CheckEngine.Recommend.UnscopedTitle"] = "You may also like",
            ["Plugins.TwinParticles.CheckEngine.Recommend.FitmentBadge"] = "Fits your vehicle"
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
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiProviderKind"] = "المزود",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiBaseUrl"] = "عنوان الأساس",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiApiKey"] = "مفتاح API",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiModel"] = "نموذج الإكمال",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingModel"] = "نموذج التضمين",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiAzureDeploymentName"] = "اسم نشر Azure",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiAzureEmbeddingDeploymentName"] = "اسم نشر تضمين Azure",
        ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiDailyTokenCeiling"] = "حد الرموز اليومي",
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
        ["Plugins.TwinParticles.CheckEngine.Recommend.Title"] = "يناسب سيارتك أيضاً",
        ["Plugins.TwinParticles.CheckEngine.Recommend.UnscopedTitle"] = "قد يعجبك أيضاً",
        ["Plugins.TwinParticles.CheckEngine.Recommend.FitmentBadge"] = "يناسب سيارتك",
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
