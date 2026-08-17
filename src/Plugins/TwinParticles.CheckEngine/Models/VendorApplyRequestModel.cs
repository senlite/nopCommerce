namespace TwinParticles.CheckEngine.Models;

public sealed class VendorApplyRequestModel
{
    public string LegalName { get; set; } = string.Empty;

    public string? TradingName { get; set; }

    public string ContactEmail { get; set; } = string.Empty;

    public string TaxIdsJson { get; set; } = "[]";

    public string CategoriesCsv { get; set; } = string.Empty;

    public string? BankingDetails { get; set; }

    public bool SubmitForReview { get; set; } = true;

    public bool AcceptAgreement { get; set; } = true;
}

public sealed class VendorReviewActionModel
{
    public int VendorId { get; set; }

    public string? Notes { get; set; }
}
