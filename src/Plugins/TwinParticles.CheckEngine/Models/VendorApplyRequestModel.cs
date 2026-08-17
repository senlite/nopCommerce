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

public sealed class VendorAssignProductModel
{
    public int VendorId { get; set; }

    public int ProductId { get; set; }
}

public sealed class VendorProductEditModel
{
    public int ProductId { get; set; }
}

public sealed class VendorInventoryUpdateModel
{
    public int ProductId { get; set; }

    public int StockQuantity { get; set; }
}

public sealed class VendorFitmentProposalModel
{
    public int ProductId { get; set; }

    public int VehicleConfigurationId { get; set; }

    public int? VendorId { get; set; }
}

public sealed class VendorOnboardingAccessModel
{
    public string? AccessToken { get; set; }
}
