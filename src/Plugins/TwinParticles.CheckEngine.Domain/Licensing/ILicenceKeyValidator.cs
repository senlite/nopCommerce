namespace TwinParticles.CheckEngine.Domain.Licensing;

public sealed class LicenceKeyValidationResult
{
    public bool IsValid { get; init; }

    public string? ReasonCode { get; init; }

    public DateTimeOffset? ExpiresUtc { get; init; }

    public LicenceTier Tier { get; init; }

    public bool MarketplaceModuleEntitlement { get; init; }
}

/// <summary>
/// Validates offline signed licence bundles (H1.36). Production vendor authority remains external;
/// staging/dev stores activate using deployment-keyed bundles.
/// </summary>
public interface ILicenceKeyValidator
{
    LicenceKeyValidationResult Validate(string licenceKey);
}
