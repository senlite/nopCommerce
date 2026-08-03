using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Web.Controllers;
using TwinParticles.CheckEngine.Application.Garage;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Models;

namespace TwinParticles.CheckEngine.Controllers;

public sealed class SearchController : BasePublicController
{
    private readonly GarageContextSearchService _garageContextSearchService;
    private readonly GarageService _garageService;
    private readonly ISearchRateLimiter _searchRateLimiter;
    private readonly IWorkContext _workContext;

    public SearchController(GarageContextSearchService garageContextSearchService, GarageService garageService, ISearchRateLimiter searchRateLimiter, IWorkContext workContext)
    {
        _garageContextSearchService = garageContextSearchService;
        _garageService = garageService;
        _searchRateLimiter = searchRateLimiter;
        _workContext = workContext;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Query([FromBody] SearchRequestModel model, CancellationToken cancellationToken)
    {
        if (model is null)
            return BadRequest();

        var customer = await _workContext.GetCurrentCustomerAsync();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var rateLimitKey = customer is null || customer.IsGuest()
            ? $"search:ip:{ipAddress}"
            : $"search:customer:{customer.Id}";

        if (!_searchRateLimiter.TryAcquire(rateLimitKey, out var retryAfterSeconds))
        {
            return StatusCode(429, new { reasonCode = "search.rate_limited", retryAfterSeconds });
        }

        var query = new SearchQuery
        {
            RawText = model.RawText,
            Mode = model.Mode,
            VehicleConfigurationId = model.VehicleConfigurationId,
            WidenFitment = model.WidenFitment,
            Filters = new SearchFilters
            {
                CategoryId = model.CategoryId,
                Brand = model.Brand,
                PriceMin = model.PriceMin,
                PriceMax = model.PriceMax
            },
            Page = model.Page,
            PageSize = model.PageSize,
            Locale = model.Locale
        };

        int? activeVehicleConfigurationId = null;

        if (customer is not null && !customer.IsGuest())
        {
            var garage = await _garageService.GetAsync(customer.Id, cancellationToken);
            var activeVehicle = garage.Vehicles.FirstOrDefault(x => x.Id == garage.ActiveGarageVehicleId);
            activeVehicleConfigurationId = activeVehicle?.VehicleConfigurationId;
        }

        return Json(await _garageContextSearchService.SearchWithGarageContextAsync(query, activeVehicleConfigurationId, cancellationToken));
    }
}
