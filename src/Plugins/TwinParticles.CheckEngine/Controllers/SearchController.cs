using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;
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
    private readonly ICustomerService _customerService;
    private readonly RecommendationService _recommendationService;
    private readonly SearchAutocompleteService _autocompleteService;
    private readonly ISearchAnalyticsService _searchAnalyticsService;
    private readonly ISearchRateLimiter _searchRateLimiter;
    private readonly IWorkContext _workContext;

    public SearchController(
        GarageContextSearchService garageContextSearchService,
        GarageService garageService,
        ICustomerService customerService,
        ISearchRateLimiter searchRateLimiter,
        IWorkContext workContext,
        RecommendationService recommendationService,
        SearchAutocompleteService autocompleteService,
        ISearchAnalyticsService searchAnalyticsService)
    {
        _garageContextSearchService = garageContextSearchService;
        _garageService = garageService;
        _customerService = customerService;
        _searchRateLimiter = searchRateLimiter;
        _workContext = workContext;
        _recommendationService = recommendationService;
        _autocompleteService = autocompleteService;
        _searchAnalyticsService = searchAnalyticsService;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Query([FromBody] SearchRequestModel model, CancellationToken cancellationToken)
    {
        if (model is null)
            return BadRequest();

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isGuest = await _customerService.IsGuestAsync(customer);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var rateLimitKey = isGuest
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

        if (!isGuest)
        {
            var garage = await _garageService.GetAsync(customer.Id, cancellationToken);
            var activeVehicle = garage.Vehicles.FirstOrDefault(x => x.Id == garage.ActiveGarageVehicleId);
            activeVehicleConfigurationId = activeVehicle?.VehicleConfigurationId;
        }

        return Json(await _garageContextSearchService.SearchWithGarageContextAsync(query, activeVehicleConfigurationId, cancellationToken));
    }

    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Suggest(string? term, string? locale, int take = 6, CancellationToken cancellationToken = default)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var isGuest = await _customerService.IsGuestAsync(customer);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var rateLimitKey = isGuest
            ? $"suggest:ip:{ipAddress}"
            : $"suggest:customer:{customer.Id}";

        if (!_searchRateLimiter.TryAcquire(rateLimitKey, out var retryAfterSeconds))
            return StatusCode(429, new { reasonCode = "search.rate_limited", retryAfterSeconds });

        var result = await _autocompleteService.SuggestAsync(term ?? string.Empty, locale ?? "en", take, cancellationToken);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Click(
        [FromBody] SearchClickModel model,
        CancellationToken cancellationToken)
    {
        if (model is null || model.AnalyticsId <= 0 || model.ProductId <= 0)
            return BadRequest(new { reasonCode = "search.analytics.invalid_click" });

        await _searchAnalyticsService.RecordClickAsync(
            model.AnalyticsId,
            model.ProductId,
            cancellationToken);
        return Ok();
    }

    [HttpGet]
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Recommend(int? vehicleConfigurationId, int take = 8, int? seedProductId = null, CancellationToken cancellationToken = default)
    {
        if (take <= 0)
            take = 8;

        if (!vehicleConfigurationId.HasValue)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            var isGuest = await _customerService.IsGuestAsync(customer);
            if (!isGuest)
            {
                var garage = await _garageService.GetAsync(customer.Id, cancellationToken);
                var activeVehicle = garage.Vehicles.FirstOrDefault(x => x.Id == garage.ActiveGarageVehicleId);
                vehicleConfigurationId = activeVehicle?.VehicleConfigurationId;
            }
        }

        var recommendations = await _recommendationService.GetRecommendationsAsync(
            vehicleConfigurationId,
            take,
            cancellationToken,
            seedProductId);
        return Json(new
        {
            vehicleScoped = recommendations.VehicleScoped,
            hits = recommendations.Hits
        });
    }
}
