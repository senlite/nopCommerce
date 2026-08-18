using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Licensing;

namespace TwinParticles.CheckEngine.Application.Licensing;

/// <summary>
/// EP-25: workshop portal routes require <see cref="LicenceStatus.WorkshopPortalEntitlement"/>.
/// </summary>
public sealed class WorkshopPortalLicenceGate
{
    private readonly ILicenceService _licenceService;

    public WorkshopPortalLicenceGate(ILicenceService licenceService)
    {
        _licenceService = licenceService;
    }

    public async Task<bool> AllowsWorkshopAsync(CancellationToken cancellationToken)
    {
        var status = await _licenceService.GetStatusAsync(cancellationToken);
        return status.WorkshopPortalEntitlement;
    }
}
