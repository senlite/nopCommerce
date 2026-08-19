using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Licensing;

namespace TwinParticles.CheckEngine.Application.Licensing;

/// <summary>
/// EP-26: fleet portal routes require <see cref="LicenceStatus.FleetPortalEntitlement"/>.
/// </summary>
public sealed class FleetPortalLicenceGate
{
    private readonly ILicenceService _licenceService;

    public FleetPortalLicenceGate(ILicenceService licenceService)
    {
        _licenceService = licenceService;
    }

    public async Task<bool> AllowsFleetAsync(CancellationToken cancellationToken)
    {
        var status = await _licenceService.GetStatusAsync(cancellationToken);
        return status.FleetPortalEntitlement;
    }
}
