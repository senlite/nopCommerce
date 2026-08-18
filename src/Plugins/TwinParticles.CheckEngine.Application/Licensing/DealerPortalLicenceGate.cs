using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Licensing;

namespace TwinParticles.CheckEngine.Application.Licensing;

/// <summary>
/// EP-27: dealer portal routes require <see cref="LicenceStatus.DealerPortalEntitlement"/>.
/// </summary>
public sealed class DealerPortalLicenceGate
{
    private readonly ILicenceService _licenceService;

    public DealerPortalLicenceGate(ILicenceService licenceService)
    {
        _licenceService = licenceService;
    }

    public async Task<bool> AllowsDealerAsync(CancellationToken cancellationToken)
    {
        var status = await _licenceService.GetStatusAsync(cancellationToken);
        return status.DealerPortalEntitlement;
    }
}
