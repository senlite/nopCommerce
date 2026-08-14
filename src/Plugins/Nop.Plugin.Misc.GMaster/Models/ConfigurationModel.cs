using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.GMaster.Models;

public sealed record ConfigurationModel : BaseNopModel
{
    [NopResourceDisplayName("Plugins.Misc.GMaster.LastImport")]
    public DateTime? LastImportUtc { get; init; }

    [NopResourceDisplayName("Plugins.Misc.GMaster.SourceVersion")]
    public string CatalogSourceVersion { get; init; } = string.Empty;

    [NopResourceDisplayName("Plugins.Misc.GMaster.ImportedProducts")]
    public int ImportedProductCount { get; init; }

    [NopResourceDisplayName("Plugins.Misc.GMaster.ImportedCategories")]
    public int ImportedCategoryCount { get; init; }

    [NopResourceDisplayName("Plugins.Misc.GMaster.ImportedImages")]
    public int ImportedImageCount { get; init; }

    [NopResourceDisplayName("Plugins.Misc.GMaster.RmbRate")]
    public decimal RmbToEgpRate { get; set; }

    [NopResourceDisplayName("Plugins.Misc.GMaster.ClearedProducts")]
    public int ClearedProductCount { get; init; }

    [NopResourceDisplayName("Plugins.Misc.GMaster.ClearedCategories")]
    public int ClearedCategoryCount { get; init; }

    [NopResourceDisplayName("Plugins.Misc.GMaster.ClearedCartItems")]
    public int ClearedCartItemCount { get; init; }

    [NopResourceDisplayName("Plugins.Misc.GMaster.LastError")]
    public string LastError { get; init; } = string.Empty;

    [NopResourceDisplayName("Plugins.Misc.GMaster.Reimport.Confirmation")]
    public string Confirmation { get; set; } = string.Empty;
}
