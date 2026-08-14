using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Web.Controllers;
using TwinParticles.CheckEngine.Application.Vehicle.Admin;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Models;

namespace TwinParticles.CheckEngine.Controllers;

public sealed class VinController : BasePublicController
{
    private readonly VinDecodeApplicationService _decodeService;
    private readonly VehicleAdminService _vehicleAdminService;
    private readonly IVinDecodeRateLimiter _rateLimiter;
    private readonly IWorkContext _workContext;

    public VinController(
        VinDecodeApplicationService decodeService,
        VehicleAdminService vehicleAdminService,
        IVinDecodeRateLimiter rateLimiter,
        IWorkContext workContext)
    {
        _decodeService = decodeService;
        _vehicleAdminService = vehicleAdminService;
        _rateLimiter = rateLimiter;
        _workContext = workContext;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Decode([FromBody] VinDecodeRequestModel model, CancellationToken cancellationToken)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Vin))
        {
            return BadRequest(new { reasonCode = "vin.invalid_length" });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!_rateLimiter.TryAcquire($"vin:ip:{ipAddress}", out var retryAfterSecondsByIp))
        {
            return StatusCode(429, new VinDecodeRateLimitedModel
            {
                RetryAfterSeconds = retryAfterSecondsByIp
            });
        }

        var customer = await _workContext.GetCurrentCustomerAsync();
        var customerId = customer?.Id ?? 0;
        if (!_rateLimiter.TryAcquire($"vin:customer:{customerId}", out var retryAfterSecondsByCustomer))
        {
            return StatusCode(429, new VinDecodeRateLimitedModel
            {
                RetryAfterSeconds = retryAfterSecondsByCustomer
            });
        }

        var result = await _decodeService.DecodeAsync(model.Vin, cancellationToken);
        if (string.Equals(result.Outcome, "NeedsDisambiguation", StringComparison.Ordinal) && result.Candidates.Count > 0)
        {
            var labels = await _vehicleAdminService.GetConfigurationDisplayLabelsAsync(
                result.Candidates.Select(candidate => candidate.VehicleConfigurationId),
                cancellationToken);

            return Json(new
            {
                result.Outcome,
                result.NormalizedVin,
                result.CheckDigitValid,
                result.Wmi,
                result.ReasonCode,
                candidates = result.Candidates.Select(candidate => new
                {
                    vehicleConfigurationId = candidate.VehicleConfigurationId,
                    confidence = candidate.Confidence.Value,
                    modelYear = candidate.ModelYear,
                    label = labels.TryGetValue(candidate.VehicleConfigurationId, out var label)
                        ? label
                        : $"Vehicle #{candidate.VehicleConfigurationId}"
                })
            });
        }

        return Json(result);
    }
}
