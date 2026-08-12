using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Controllers;
using TwinParticles.CheckEngine.Application.Observability;

namespace TwinParticles.CheckEngine.Controllers;

public sealed class HealthController : BasePublicController
{
    private readonly CheckEngineHealthService _healthService;

    public HealthController(CheckEngineHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var snapshot = await _healthService.ProbeAsync(cancellationToken);
        return Json(new
        {
            status = snapshot.Status,
            database = snapshot.Database,
            searchIndex = snapshot.SearchIndex,
            erp = snapshot.Erp,
            licence = snapshot.Licence,
            utc = snapshot.Utc
        });
    }
}
