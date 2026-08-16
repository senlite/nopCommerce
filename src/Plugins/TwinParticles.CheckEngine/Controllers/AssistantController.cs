using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Controllers;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Models;

namespace TwinParticles.CheckEngine.Controllers;

public sealed class AssistantController : BasePublicController
{
    private readonly CustomerAssistantService _assistantService;

    public AssistantController(CustomerAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Ask([FromBody] AssistantRequestModel model, CancellationToken cancellationToken)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Question))
            return BadRequest();

        var response = await _assistantService.AskAsync(
            model.Question,
            model.VehicleConfigurationId,
            cancellationToken,
            string.IsNullOrWhiteSpace(model.Locale) ? "en" : model.Locale.Trim());
        return Json(response);
    }
}
