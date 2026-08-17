namespace TwinParticles.CheckEngine.Domain.Marketplace;

public sealed class VendorApplication
{
    public string LegalName { get; init; } = string.Empty;

    public string? TradingName { get; init; }

    public string ContactEmail { get; init; } = string.Empty;

    public int? ApplicantCustomerId { get; init; }

    public string TaxIdsJson { get; init; } = "[]";

    public string CategoriesCsv { get; init; } = string.Empty;

    /// <summary>Plaintext banking details accepted only at apply time; stored encrypted.</summary>
    public string? BankingDetails { get; init; }
}
