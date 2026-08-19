using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Vehicle.Admin;
using TwinParticles.CheckEngine.Infrastructure;
using TwinParticles.CheckEngine.Models;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class VehicleAdminController : BasePluginController
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly VehicleAdminService _service;
    private readonly Nop.Services.Security.IPermissionService _permissionService;
    private readonly IWorkContext _workContext;

    public VehicleAdminController(
        VehicleAdminService service,
        Nop.Services.Security.IPermissionService permissionService,
        IWorkContext workContext)
    {
        _service = service;
        _permissionService = permissionService;
        _workContext = workContext;
    }

    private async Task<bool> AuthorizedAsync() => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    private IActionResult? PageOrJsonApi()
        => Request.WantsJsonResponse() ? null : RedirectToAction(nameof(Index));

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/VehicleAdmin.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Makes(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (PageOrJsonApi() is { } page) return page;
        return Json(await _service.GetMakesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateMake([FromBody] VehicleAdminDtos.MakeUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.CreateMakeAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateMake([FromBody] VehicleAdminDtos.MakeUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.UpdateMakeAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMake(int id, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        try
        {
            await _service.DeleteMakeAsync(id, cancellationToken);
            return Ok();
        }
        catch (System.InvalidOperationException exception)
        {
            return Conflict(new { reasonCode = exception.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ArchiveMake(int id = 0, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        id = await ResolveIdAsync(id, cancellationToken);
        var result = await _service.ArchiveMakeAsync(id, await GetActorAsync(), cancellationToken);
        return LifecycleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> MergeMake(
        int sourceId = 0,
        int targetId = 0,
        CancellationToken cancellationToken = default)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        (sourceId, targetId) = await ResolveMergeIdsAsync(sourceId, targetId, cancellationToken);
        var result = await _service.MergeMakeAsync(sourceId, targetId, await GetActorAsync(), cancellationToken);
        return LifecycleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> Models(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (PageOrJsonApi() is { } page) return page;
        return Json(await _service.GetModelsAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateModel([FromBody] VehicleAdminDtos.ModelUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.CreateModelAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateModel([FromBody] VehicleAdminDtos.ModelUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.UpdateModelAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteModel(int id, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        try
        {
            await _service.DeleteModelAsync(id, cancellationToken);
            return Ok();
        }
        catch (System.InvalidOperationException exception)
        {
            return Conflict(new { reasonCode = exception.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ArchiveModel(int id = 0, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        id = await ResolveIdAsync(id, cancellationToken);
        var result = await _service.ArchiveModelAsync(id, await GetActorAsync(), cancellationToken);
        return LifecycleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> MergeModel(
        int sourceId = 0,
        int targetId = 0,
        CancellationToken cancellationToken = default)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        (sourceId, targetId) = await ResolveMergeIdsAsync(sourceId, targetId, cancellationToken);
        var result = await _service.MergeModelAsync(sourceId, targetId, await GetActorAsync(), cancellationToken);
        return LifecycleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> Generations(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (PageOrJsonApi() is { } page) return page;
        return Json(await _service.GetGenerationsAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateGeneration([FromBody] VehicleAdminDtos.GenerationUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.CreateGenerationAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateGeneration([FromBody] VehicleAdminDtos.GenerationUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.UpdateGenerationAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteGeneration(int id, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        try
        {
            await _service.DeleteGenerationAsync(id, cancellationToken);
            return Ok();
        }
        catch (System.InvalidOperationException exception)
        {
            return Conflict(new { reasonCode = exception.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ArchiveGeneration(int id = 0, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        id = await ResolveIdAsync(id, cancellationToken);
        var result = await _service.ArchiveGenerationAsync(id, await GetActorAsync(), cancellationToken);
        return LifecycleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> MergeGeneration(
        int sourceId = 0,
        int targetId = 0,
        CancellationToken cancellationToken = default)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        (sourceId, targetId) = await ResolveMergeIdsAsync(sourceId, targetId, cancellationToken);
        var result = await _service.MergeGenerationAsync(sourceId, targetId, await GetActorAsync(), cancellationToken);
        return LifecycleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> Bodies(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (PageOrJsonApi() is { } page) return page;
        return Json(await _service.GetBodiesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateBody([FromBody] VehicleAdminDtos.BodyUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.CreateBodyAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateBody([FromBody] VehicleAdminDtos.BodyUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.UpdateBodyAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteBody(int id, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.DeleteBodyAsync(id, cancellationToken);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Engines(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (PageOrJsonApi() is { } page) return page;
        return Json(await _service.GetEnginesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateEngine([FromBody] VehicleAdminDtos.EngineUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.CreateEngineAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateEngine([FromBody] VehicleAdminDtos.EngineUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.UpdateEngineAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEngine(int id, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.DeleteEngineAsync(id, cancellationToken);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Markets(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (PageOrJsonApi() is { } page) return page;
        return Json(await _service.GetMarketsAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateMarket([FromBody] VehicleAdminDtos.MarketUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.CreateMarketAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateMarket([FromBody] VehicleAdminDtos.MarketUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.UpdateMarketAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMarket(int id, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.DeleteMarketAsync(id, cancellationToken);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Configurations(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (PageOrJsonApi() is { } page) return page;
        return Json(await _service.GetConfigurationsAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateConfiguration([FromBody] VehicleAdminDtos.ConfigurationUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.CreateConfigurationAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateConfiguration([FromBody] VehicleAdminDtos.ConfigurationUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.UpdateConfigurationAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteConfiguration(int id, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.DeleteConfigurationAsync(id, cancellationToken);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Aliases(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (PageOrJsonApi() is { } page) return page;
        return Json(await _service.GetAliasesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAlias([FromBody] VehicleAdminDtos.AliasUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.CreateAliasAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateAlias([FromBody] VehicleAdminDtos.AliasUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.UpdateAliasAsync(model.ToEntity(), cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAlias(int id, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.DeleteAliasAsync(id, cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        var result = await _service.SeedAsync(cancellationToken);
        return Json(new VehicleAdminDtos.SeedResultModel
        {
            MakesInserted = result.MakesInserted,
            ModelsInserted = result.ModelsInserted,
            GenerationsInserted = result.GenerationsInserted,
            BodiesInserted = result.BodiesInserted,
            EnginesInserted = result.EnginesInserted,
            MarketsInserted = result.MarketsInserted,
            ConfigurationsInserted = result.ConfigurationsInserted,
            AliasesInserted = result.AliasesInserted
        });
    }

    private async Task<string> GetActorAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        return $"customer:{customer.Id}";
    }

    private IActionResult LifecycleResult(VehicleLifecycleResult result)
    {
        var model = new VehicleAdminDtos.LifecycleResultModel
        {
            Success = result.Success,
            ErrorCode = result.ErrorCode,
            MovedChildren = result.MovedChildren,
            MovedAliases = result.MovedAliases
        };

        return result.Success ? Json(model) : StatusCode(409, model);
    }

    // Lifecycle actions accept form/query (browser FormData + antiforgery field) and JSON bodies.
    // [FromBody]-only binding rejects multipart/form-data with 415 before the action runs.
    private async Task<int> ResolveIdAsync(int id, CancellationToken cancellationToken)
    {
        if (id > 0)
            return id;

        var model = await TryReadJsonAsync<VehicleAdminDtos.ArchiveRequestModel>(cancellationToken);
        return model?.Id ?? 0;
    }

    private async Task<(int SourceId, int TargetId)> ResolveMergeIdsAsync(
        int sourceId,
        int targetId,
        CancellationToken cancellationToken)
    {
        if (sourceId > 0 && targetId > 0)
            return (sourceId, targetId);

        var model = await TryReadJsonAsync<VehicleAdminDtos.MergeRequestModel>(cancellationToken);
        return (model?.SourceId ?? sourceId, model?.TargetId ?? targetId);
    }

    private async Task<T?> TryReadJsonAsync<T>(CancellationToken cancellationToken)
        where T : class
    {
        if (Request.ContentType is null
            || Request.ContentType.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) < 0)
            return null;

        if (!Request.Body.CanSeek)
            Request.EnableBuffering();

        if (Request.Body.CanSeek)
            Request.Body.Position = 0;

        try
        {
            return await JsonSerializer.DeserializeAsync<T>(Request.Body, JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
