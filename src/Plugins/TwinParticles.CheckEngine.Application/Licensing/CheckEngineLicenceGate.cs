using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Licensing;

namespace TwinParticles.CheckEngine.Application.Licensing;

/// <summary>
/// Enforces ADR-009: a lapsed licence blocks admin writes, never the storefront.
/// </summary>
public sealed class CheckEngineLicenceGate
{
    public static readonly TimeSpan GracePeriod = TimeSpan.FromDays(30);

    private readonly ILicenceService _licenceService;

    public CheckEngineLicenceGate(ILicenceService licenceService)
    {
        _licenceService = licenceService;
    }

    public async Task<bool> AllowsAdminWriteAsync(CancellationToken cancellationToken)
    {
        var status = await _licenceService.GetStatusAsync(cancellationToken);
        return status.AllowsAdminWrite;
    }
}
