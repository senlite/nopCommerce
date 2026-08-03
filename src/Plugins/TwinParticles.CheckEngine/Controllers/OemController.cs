using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Controllers;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Models;

namespace TwinParticles.CheckEngine.Controllers;

public sealed class OemController : BasePublicController
{
    private readonly OemResolveService _resolveService;

    public OemController(OemResolveService resolveService)
    {
        _resolveService = resolveService;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Resolve([FromBody] OemResolveRequestModel model, CancellationToken cancellationToken)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Number))
            return BadRequest(new { reasonCode = "oem.not_found" });

        var result = await _resolveService.ResolveAsync(new OemResolveQuery
        {
            Number = model.Number,
            ManufacturerId = model.ManufacturerId
        }, cancellationToken);

        return Json(result);
    }
}
