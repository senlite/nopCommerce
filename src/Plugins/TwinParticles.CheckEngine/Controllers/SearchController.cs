using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Controllers;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Models;

namespace TwinParticles.CheckEngine.Controllers;

public sealed class SearchController : BasePublicController
{
    private readonly UnifiedSearchService _service;

    public SearchController(UnifiedSearchService service)
    {
        _service = service;
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

        return Json(await _service.SearchAsync(query, cancellationToken));
    }
}
