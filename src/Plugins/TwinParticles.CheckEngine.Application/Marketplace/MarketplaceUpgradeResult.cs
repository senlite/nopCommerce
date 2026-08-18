namespace TwinParticles.CheckEngine.Application.Marketplace;

public sealed class MarketplaceUpgradeResult
{
    public bool Succeeded { get; init; }

    public string? ReasonCode { get; init; }

    public int OperatorVendorId { get; init; }

    public int ProductCount { get; init; }

    public int NewlyAssigned { get; init; }

    public int AlreadyAssigned { get; init; }

    public bool CatalogPreserved { get; init; }

    public static MarketplaceUpgradeResult Fail(string reasonCode)
        => new() { Succeeded = false, ReasonCode = reasonCode };
}
