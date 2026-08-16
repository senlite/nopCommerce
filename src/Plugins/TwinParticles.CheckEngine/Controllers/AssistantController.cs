using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Web.Controllers;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Models;

namespace TwinParticles.CheckEngine.Controllers;

public sealed class AssistantController : BasePublicController
{
    private readonly CustomerAssistantService _assistantService;
    private readonly ICustomerService _customerService;
    private readonly ISearchRateLimiter _searchRateLimiter;
    private readonly IWorkContext _workContext;

    public AssistantController(
        CustomerAssistantService assistantService,
        ICustomerService customerService,
        ISearchRateLimiter searchRateLimiter,
        IWorkContext workContext)
    {
        _assistantService = assistantService;
        _customerService = customerService;
        _searchRateLimiter = searchRateLimiter;
        _workContext = workContext;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Ask([FromBody] AssistantRequestModel model, CancellationToken cancellationToken)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Question))
            return BadRequest();

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isGuest = await _customerService.IsGuestAsync(customer);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var rateLimitKey = isGuest
            ? $"assistant:ip:{ipAddress}"
            : $"assistant:customer:{customer.Id}";

        if (!_searchRateLimiter.TryAcquire(rateLimitKey, out var retryAfterSeconds))
            return StatusCode(429, new { reasonCode = "assistant.rate_limited", retryAfterSeconds });

        var response = await _assistantService.AskAsync(
            model.Question,
            model.VehicleConfigurationId,
            cancellationToken,
            string.IsNullOrWhiteSpace(model.Locale) ? "en" : model.Locale.Trim());
        return Json(response);
    }
}
