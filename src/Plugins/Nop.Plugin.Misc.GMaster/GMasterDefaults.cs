namespace Nop.Plugin.Misc.GMaster;

public static class GMasterDefaults
{
    public const string SystemName = "Misc.GMaster";

    public const string CatalogRelativePath = "Plugins/Misc.GMaster/Content/gmaster-catalog.csv";

    public const string ImageRelativeDirectory = "Plugins/Misc.GMaster/Content/parts";

    public const string SourceVersion = "excel-containers-2026-01";

    public const string ReimportConfirmation = "REPLACE CATALOG";

    /// <summary>
    /// Fallback RMB→EGP conversion rate used when the operator has not set one. The supplier lists
    /// are priced in Chinese yuan; this converts cost to EGP before the retail margin is applied.
    /// </summary>
    public const decimal DefaultRmbToEgpRate = 7.0m;
}
