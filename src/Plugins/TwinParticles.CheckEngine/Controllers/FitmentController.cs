using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Controllers;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Models;

namespace TwinParticles.CheckEngine.Controllers;

public sealed class FitmentController : BasePublicController
{
    private readonly FitmentEvaluationService _service;

    public FitmentController(FitmentEvaluationService service)
    {
        _service = service;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Evaluate([FromBody] FitmentEvaluateRequestModel model, CancellationToken cancellationToken)
    {
        if (model is null || model.ProductId <= 0 || model.VehicleConfigurationId <= 0)
            return BadRequest(new { reasonCode = "fitment.invalid_request" });

        var result = await _service.EvaluateAsync(new FitmentEvaluationContext
        {
            ProductId = model.ProductId,
            VehicleConfigurationId = model.VehicleConfigurationId,
            ProductionYear = model.ProductionYear,
            SteeringSide = model.SteeringSide,
            MarketRegion = model.MarketRegion
        }, cancellationToken);

        return Json(result);
    }
}
