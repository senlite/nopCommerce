using Nop.Core;
using Nop.Plugin.Misc.GMaster.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;

namespace Nop.Plugin.Misc.GMaster;

public sealed class GMasterPlugin : BasePlugin, IMiscPlugin
{
    private readonly GMasterCatalogImportService _catalogImportService;
    private readonly ILocalizationService _localizationService;
    private readonly ISettingService _settingService;
    private readonly IWebHelper _webHelper;

    public GMasterPlugin(
        GMasterCatalogImportService catalogImportService,
        ILocalizationService localizationService,
        ISettingService settingService,
        IWebHelper webHelper)
    {
        _catalogImportService = catalogImportService;
        _localizationService = localizationService;
        _settingService = settingService;
        _webHelper = webHelper;
    }

    public override string GetConfigurationPageUrl()
        => $"{_webHelper.GetStoreLocation()}Admin/GMaster/Configure";

    public override async Task InstallAsync()
    {
        await _settingService.SaveSettingAsync(new GMasterSettings());
        await InstallLocaleResourcesAsync();

        // The user explicitly requested replacement during setup. The source is parsed and validated
        // in full before the first existing catalog entity is soft-deleted.
        await _catalogImportService.ReplaceCatalogAsync();

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        // Catalog data deliberately remains after uninstall. Removing a catalog management plugin must
        // never destroy the store's products a second time.
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.GMaster");
        await _settingService.DeleteSettingAsync<GMasterSettings>();
        await base.UninstallAsync();
    }

    private Task InstallLocaleResourcesAsync()
        => _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Misc.GMaster.Title"] = "GMaster Catalog Importer",
            ["Plugins.Misc.GMaster.Description"] = "Imports the curated accessories and fiber price lists into a clean BMW-compatible parts catalog.",
            ["Plugins.Misc.GMaster.LastImport"] = "Last import (UTC)",
            ["Plugins.Misc.GMaster.SourceVersion"] = "Catalog source version",
            ["Plugins.Misc.GMaster.ImportedProducts"] = "Imported products",
            ["Plugins.Misc.GMaster.ImportedCategories"] = "Imported categories",
            ["Plugins.Misc.GMaster.ClearedProducts"] = "Cleared products",
            ["Plugins.Misc.GMaster.ClearedCategories"] = "Cleared categories",
            ["Plugins.Misc.GMaster.ClearedCartItems"] = "Cleared cart / wishlist items",
            ["Plugins.Misc.GMaster.Reimport"] = "Replace catalog now",
            ["Plugins.Misc.GMaster.Reimport.Warning"] = "Destructive action: this stages a replacement, then soft-deletes every active product/category and clears current cart/wishlist items before publishing GMaster. Historical orders remain intact.",
            ["Plugins.Misc.GMaster.Reimport.Confirmation"] = $"Type {GMasterDefaults.ReimportConfirmation} to continue",
            ["Plugins.Misc.GMaster.Reimport.InvalidConfirmation"] = "Confirmation text did not match. The catalog was not changed.",
            ["Plugins.Misc.GMaster.Reimport.Success"] = "GMaster catalog replacement completed: {0} products across {1} categories.",
            ["Plugins.Misc.GMaster.LastError"] = "Last error",
            ["Plugins.Misc.GMaster.Pricing"] = "Egyptian-market pricing policy",
            ["Plugins.Misc.GMaster.Pricing.Detail"] = "Cost ≤ 500: +55%; 501–1,000: +45%; 1,001–3,000: +35%; 3,001–10,000: +28%; above 10,000: +22%. Selling prices are rounded up to the next EGP 10."
        });
}
