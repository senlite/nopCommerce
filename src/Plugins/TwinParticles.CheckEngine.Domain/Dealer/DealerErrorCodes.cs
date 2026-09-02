namespace TwinParticles.CheckEngine.Domain.Dealer;

public static class DealerErrorCodes
{
    public const string LicenceDenied = "dealer.licence_denied";
    public const string NotFound = "dealer.not_found";
    public const string QuotaExceeded = "dealer.quota_exceeded";
    public const string AllocationExceeded = "dealer.allocation_exceeded";
    public const string TerritoryDenied = "dealer.territory_denied";
    public const string InvalidTransition = "dealer.invalid_transition";
    public const string EmptyOrder = "dealer.empty_order";
    public const string FitmentBlocked = "dealer.fitment_blocked";
}
