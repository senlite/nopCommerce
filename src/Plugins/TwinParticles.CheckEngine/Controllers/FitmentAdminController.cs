using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Models;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.Admin)]
[AutoValidateAntiforgeryToken]
public sealed class FitmentAdminController : BasePluginController
{
    private readonly FitmentReviewService _reviewService;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public FitmentAdminController(FitmentReviewService reviewService, Nop.Services.Security.IPermissionService permissionService)
    {
        _reviewService = reviewService;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync() => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> Queue(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return Json(await _reviewService.GetQueueAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Approve([FromBody] FitmentReviewActionModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _reviewService.ApproveAsync(model.ClaimId, cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Reject([FromBody] FitmentReviewActionModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _reviewService.RejectAsync(model.ClaimId, cancellationToken);
        return Ok();
    }
}
