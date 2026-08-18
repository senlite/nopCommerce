using System;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public sealed class Vendor
{
    public int Id { get; set; }

    public string LegalName { get; set; } = string.Empty;

    public string? TradingName { get; set; }

    public string ContactEmail { get; set; } = string.Empty;

    public int? ApplicantCustomerId { get; set; }

    public string TaxIdsJson { get; set; } = "[]";

    public string CategoriesCsv { get; set; } = string.Empty;

    public VendorStatus Status { get; set; } = VendorStatus.Applied;

    /// <summary>
    /// Well-known operator seller created by the single-supplier → marketplace upgrade (FR-890).
    /// </summary>
    public bool IsOperator { get; set; }

    /// <summary>
    /// SHA-256 hex of the apply-time guest access token. Never serialize this to responses.
    /// </summary>
    public string? ApplicantAccessTokenHash { get; set; }

    /// <summary>
    /// Encrypted banking payload. Never serialize this to admin or storefront responses.
    /// </summary>
    public string? BankingSecretProtected { get; set; }

    public bool HasBankingDetails { get; set; }

    public string? ReviewNotes { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }

    public Vendor RedactSecrets()
    {
        return new Vendor
        {
            Id = Id,
            LegalName = LegalName,
            TradingName = TradingName,
            ContactEmail = ContactEmail,
            ApplicantCustomerId = ApplicantCustomerId,
            TaxIdsJson = TaxIdsJson,
            CategoriesCsv = CategoriesCsv,
            Status = Status,
            IsOperator = IsOperator,
            ApplicantAccessTokenHash = null,
            BankingSecretProtected = null,
            HasBankingDetails = HasBankingDetails || !string.IsNullOrWhiteSpace(BankingSecretProtected),
            ReviewNotes = ReviewNotes,
            CreatedUtc = CreatedUtc,
            UpdatedUtc = UpdatedUtc
        };
    }
}
