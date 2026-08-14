namespace Nop.Plugin.Misc.GMaster.Services;

public sealed record GMasterCatalogItem(
    string Sku,
    string ArabicName,
    string EnglishName,
    string Oem,
    string VehicleModels,
    string CategoryKey,
    decimal CostRmb,
    string ImageFile,
    string SourceFile);

public sealed record GMasterCatalogImportResult(
    int ClearedProducts,
    int ClearedCategories,
    int ClearedCartItems,
    int ImportedProducts,
    int ImportedCategories,
    int ImportedImages,
    DateTime CompletedUtc);
