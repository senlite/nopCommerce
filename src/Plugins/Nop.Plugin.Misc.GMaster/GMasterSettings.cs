using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.GMaster;

public sealed class GMasterSettings : ISettings
{
    public DateTime? LastImportUtc { get; set; }

    public int ImportedProductCount { get; set; }

    public int ImportedCategoryCount { get; set; }

    public int ClearedProductCount { get; set; }

    public int ClearedCategoryCount { get; set; }

    public int ClearedCartItemCount { get; set; }

    public string CatalogSourceVersion { get; set; } = string.Empty;

    public string LastError { get; set; } = string.Empty;
}
