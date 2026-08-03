using Microsoft.AspNetCore.Mvc;
using Nop.Web.Controllers;
using TwinParticles.CheckEngine.Application.L10n;
using TwinParticles.CheckEngine.Models;

namespace TwinParticles.CheckEngine.Controllers;

public sealed class L10nController : BasePublicController
{
    private readonly LocaleFormattingService _service;

    public L10nController(LocaleFormattingService service)
    {
        _service = service;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult Preview([FromBody] L10nPreviewRequestModel model)
    {
        if (model is null)
            return BadRequest();

        var result = _service.Preview(model.Number, model.Date, model.UnitCode, model.Locale);
        return Json(result);
    }
}
