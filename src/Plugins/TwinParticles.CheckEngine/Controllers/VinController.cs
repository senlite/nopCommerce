using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Web.Controllers;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Models;

namespace TwinParticles.CheckEngine.Controllers;

public sealed class VinController : BasePublicController
{
    private readonly VinDecodeApplicationService _decodeService;
    private readonly IVinDecodeRateLimiter _rateLimiter;
    private readonly IWorkContext _workContext;

    public VinController(VinDecodeApplicationService decodeService, IVinDecodeRateLimiter rateLimiter, IWorkContext workContext)
    {
        _decodeService = decodeService;
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
        return Json(result);
    }
}
