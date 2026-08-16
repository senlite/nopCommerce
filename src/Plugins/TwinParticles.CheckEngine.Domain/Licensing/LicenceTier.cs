namespace TwinParticles.CheckEngine.Domain.Licensing;

/// <summary>
/// Licence SKUs from LICENSE.md § 3.1 / docs/43-licensing.md. Marketplace is Business and above.
/// </summary>
public enum LicenceTier
{
    Unknown = 0,
    SingleStore = 1,
    MultiStore = 2,
    Business = 3,
    Enterprise = 4,
    OemRedistribution = 5
}
