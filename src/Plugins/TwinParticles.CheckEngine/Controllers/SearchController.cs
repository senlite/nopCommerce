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
    private readonly IWorkContext _workContext;

    public SearchController(GarageContextSearchService garageContextSearchService, GarageService garageService, IWorkContext workContext)
    {
        _garageContextSearchService = garageContextSearchService;
        _garageService = garageService;
        _workContext = workContext;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Query([FromBody] SearchRequestModel model, CancellationToken cancellationToken)
    {
        if (model is null)
            return BadRequest();

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

        var customer = await _workContext.GetCurrentCustomerAsync();
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
