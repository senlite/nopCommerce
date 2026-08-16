using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Licensing;

namespace TwinParticles.CheckEngine.Application.Licensing;

/// <summary>
/// FR-870 / AC-19.4: marketplace admin and apply routes require MarketplaceModuleEntitlement.
/// A lapsed licence still reports the entitlement; writes stay blocked by <see cref="CheckEngineLicenceGate"/>.
/// </summary>
public sealed class MarketplaceLicenceGate
{
    private readonly ILicenceService _licenceService;

    public MarketplaceLicenceGate(ILicenceService licenceService)
    {
        _licenceService = licenceService;
    }

    public async Task<bool> AllowsMarketplaceAsync(CancellationToken cancellationToken)
    {
        var status = await _licenceService.GetStatusAsync(cancellationToken);
        return status.MarketplaceModuleEntitlement;
    }
}
