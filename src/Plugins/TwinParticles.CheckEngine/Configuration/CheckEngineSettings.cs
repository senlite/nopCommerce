namespace TwinParticles.CheckEngine.Configuration;

public sealed class CheckEngineSettings
{
    public const string SectionName = "CheckEngine";

    public AiOptions Ai { get; init; } = new();
    public ErpOptions Erp { get; init; } = new();
    public SearchOptions Search { get; init; } = new();
    public ImagesOptions Images { get; init; } = new();
    public LicenceOptions Licence { get; init; } = new();

    public sealed class AiOptions
    {
        public bool Enabled { get; init; }
        public int MonthlyBudgetUsd { get; init; }
    }

    public sealed class ErpOptions
    {
        public bool Enabled { get; init; }
        public string BaseUrl { get; init; } = string.Empty;
    }

    public sealed class SearchOptions
    {
        public bool EnableCaching { get; init; } = true;
    }

    public sealed class ImagesOptions
    {
        public string? CdnBaseUrl { get; init; }

        public string[] BlockedHostFragments { get; init; } = ["localhost", "127.0.0.1", "169.254.169.254"];
    }

    public sealed class LicenceOptions
    {
        public bool EnforcementEnabled { get; init; } = true;
    }
}
