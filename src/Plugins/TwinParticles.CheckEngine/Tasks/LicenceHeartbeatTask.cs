using System.Threading.Tasks;
using Nop.Services.ScheduleTasks;
using TwinParticles.CheckEngine.Domain.Licensing;

namespace TwinParticles.CheckEngine.Tasks;

public sealed class LicenceHeartbeatTask : IScheduleTask
{
    private readonly ILicenceService _licenceService;

    public LicenceHeartbeatTask(ILicenceService licenceService)
    {
        _licenceService = licenceService;
    }

    public Task ExecuteAsync()
    {
        return _licenceService.HeartbeatAsync(default);
    }
}
