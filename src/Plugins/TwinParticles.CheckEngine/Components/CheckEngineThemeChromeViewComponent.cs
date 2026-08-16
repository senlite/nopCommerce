using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Seo;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Models.Catalog;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Infrastructure.Ai;

namespace TwinParticles.CheckEngine.Components;

public sealed class CheckEngineThemeChromeViewComponent : NopViewComponent
{
    private readonly ICategoryService _categoryService;
    private readonly ILocalizationService _localizationService;
    private readonly IStoreContext _storeContext;
    private readonly IUrlRecordService _urlRecordService;

    public CheckEngineThemeChromeViewComponent(
        ICategoryService categoryService,
        ILocalizationService localizationService,
        IStoreContext storeContext,
        IUrlRecordService urlRecordService)
    {
        _categoryService = categoryService;
        _localizationService = localizationService;
        _storeContext = storeContext;
        _urlRecordService = urlRecordService;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object? additionalData)
    {
        var model = new ThemeChromeModel
        {
            WidgetZone = widgetZone,
            ProductId = ResolveProductId(additionalData),
            MenuGroups = await BuildMenuGroupsAsync(widgetZone),
            MenuText = await BuildMenuTextAsync(widgetZone),
            EnableSearchNaturalLanguage = IsFeatureEnabled(AiFeatureKeys.SearchNaturalLanguage),
            EnableSearchSemantic = IsFeatureEnabled(AiFeatureKeys.SearchSemantic),
            EnableCustomerAssistant = IsFeatureEnabled(AiFeatureKeys.CustomerAssistant)
        };

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Shared/Components/CheckEngineThemeChrome/Default.cshtml", model);
    }

    private async Task<MegaMenuText> BuildMenuTextAsync(string widgetZone)
    {
        if (!string.Equals(widgetZone, PublicWidgetZones.HeaderAfter, StringComparison.OrdinalIgnoreCase))
            return new MegaMenuText();

        return new MegaMenuText
        {
            AriaLabel = await ResourceOrFallbackAsync(
                "Plugins.TwinParticles.CheckEngine.Menu.AriaLabel", "Parts navigation"),
            AllParts = await ResourceOrFallbackAsync(
                "Plugins.TwinParticles.CheckEngine.Menu.AllParts", "All parts"),
            Eyebrow = await ResourceOrFallbackAsync(
                "Plugins.TwinParticles.CheckEngine.Menu.Eyebrow", "Parts catalog"),
            Title = await ResourceOrFallbackAsync(
                "Plugins.TwinParticles.CheckEngine.Menu.Title", "Browse by category"),
            Hint = await ResourceOrFallbackAsync(
                "Plugins.TwinParticles.CheckEngine.Menu.Hint",
                "Choose a category, or add your vehicle to see verified-fit parts first."),
            Empty = await ResourceOrFallbackAsync(
                "Plugins.TwinParticles.CheckEngine.Menu.Empty",
                "Categories will appear here when the catalog is published."),
            AddVehicle = await ResourceOrFallbackAsync(
                "Plugins.TwinParticles.CheckEngine.Menu.AddVehicle", "Add your vehicle"),
            SearchByOem = await ResourceOrFallbackAsync(
                "Plugins.TwinParticles.CheckEngine.Menu.SearchByOem", "Search by OEM number")
        };
    }

    private async Task<string> ResourceOrFallbackAsync(string key, string fallback)
    {
        var value = await _localizationService.GetResourceAsync(key);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase)
            ? fallback
            : value;
    }

    private async Task<IReadOnlyList<MegaMenuGroup>> BuildMenuGroupsAsync(string widgetZone)
    {
        // The stock nopCommerce 4.90 theme invokes HeaderAfter but not HeaderMenuAfter. Render both
        // the menu and search rail from that supported extension point without patching host views.
        if (!string.Equals(widgetZone, PublicWidgetZones.HeaderAfter, StringComparison.OrdinalIgnoreCase))
            return [];

        var store = await _storeContext.GetCurrentStoreAsync();
        var categories = (await _categoryService.GetAllCategoriesAsync(store.Id))
            .Where(category => category.Published && !category.Deleted)
            .ToList();
        var roots = categories.Where(category => category.ParentCategoryId == 0)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Take(8)
            .ToList();

        var groups = new List<MegaMenuGroup>();
        foreach (var root in roots)
        {
            var children = categories.Where(category => category.ParentCategoryId == root.Id)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .Take(6)
                .ToList();
            groups.Add(new MegaMenuGroup
            {
                Name = await _localizationService.GetLocalizedAsync(root, category => category.Name),
                SeName = await _urlRecordService.GetSeNameAsync(root),
                Children = await Task.WhenAll(children.Select(async child => new MegaMenuLink
                {
                    Name = await _localizationService.GetLocalizedAsync(child, category => category.Name),
                    SeName = await _urlRecordService.GetSeNameAsync(child)
                }))
            });
        }

        return groups;
    }

    private static bool IsFeatureEnabled(string featureKey)
    {
        var enabled = CheckEngineAiOptions.Current.EnabledFeatures;
        return enabled?.Contains(featureKey, StringComparer.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// The product detail widget zones hand us the page's view model. Reading the product id here
    /// keeps the fitment band working regardless of the host theme's markup.
    /// </summary>
    private static int ResolveProductId(object? additionalData) => additionalData switch
    {
        ProductDetailsModel details => details.Id,
        int id when id > 0 => id,
        _ => 0
    };

    public sealed class ThemeChromeModel
    {
        public string WidgetZone { get; init; } = string.Empty;

        /// <summary>Zero when the current zone is not product-scoped.</summary>
        public int ProductId { get; init; }

        public IReadOnlyList<MegaMenuGroup> MenuGroups { get; init; } = [];

        public MegaMenuText MenuText { get; init; } = new();

        public bool EnableSearchNaturalLanguage { get; init; }

        public bool EnableSearchSemantic { get; init; }

        public bool EnableCustomerAssistant { get; init; }
    }

    public sealed class MegaMenuGroup
    {
        public string Name { get; init; } = string.Empty;
        public string SeName { get; init; } = string.Empty;
        public IReadOnlyList<MegaMenuLink> Children { get; init; } = [];
    }

    public sealed class MegaMenuLink
    {
        public string Name { get; init; } = string.Empty;
        public string SeName { get; init; } = string.Empty;
    }

    public sealed class MegaMenuText
    {
        public string AriaLabel { get; init; } = "Parts navigation";
        public string AllParts { get; init; } = "All parts";
        public string Eyebrow { get; init; } = "Parts catalog";
        public string Title { get; init; } = "Browse by category";
        public string Hint { get; init; } = "Choose a category, or add your vehicle to see verified-fit parts first.";
        public string Empty { get; init; } = "Categories will appear here when the catalog is published.";
        public string AddVehicle { get; init; } = "Add your vehicle";
        public string SearchByOem { get; init; } = "Search by OEM number";
    }
}
