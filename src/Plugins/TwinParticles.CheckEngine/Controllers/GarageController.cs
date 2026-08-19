using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using Nop.Web.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Garage;
using TwinParticles.CheckEngine.Application.Vehicle.Admin;
using TwinParticles.CheckEngine.Domain.Garage;
using TwinParticles.CheckEngine.Infrastructure;
using TwinParticles.CheckEngine.Models;

namespace TwinParticles.CheckEngine.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class GarageController : BasePublicController
{
    private const int MaxGuestVehicles = 20;
    private const int MaxGuestOems = 50;

    private readonly GarageService _garageService;
    private readonly GaragePrivacyService _privacyService;
    private readonly TwinParticles.CheckEngine.Application.Privacy.CheckEngineSubjectDataService _subjectDataService;
    private readonly VehicleAdminService _vehicleAdminService;
    private readonly ICustomerService _customerService;
    private readonly IWorkContext _workContext;

    public GarageController(
        GarageService garageService,
        GaragePrivacyService privacyService,
        TwinParticles.CheckEngine.Application.Privacy.CheckEngineSubjectDataService subjectDataService,
        VehicleAdminService vehicleAdminService,
        ICustomerService customerService,
        IWorkContext workContext)
    {
        _garageService = garageService;
        _privacyService = privacyService;
        _subjectDataService = subjectDataService;
        _vehicleAdminService = vehicleAdminService;
        _customerService = customerService;
        _workContext = workContext;
    }

    [HttpGet]
    public async Task<IActionResult> Current(CancellationToken cancellationToken)
    {
        if (!Request.WantsJsonResponse())
            return Redirect("/");

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsGuestAsync(customer))
            return Json(await _garageService.GetAsync(customer.Id, cancellationToken));

        return Unauthorized();
    }

    [HttpPost]
    public async Task<IActionResult> AddVehicle([FromBody] GarageAddVehicleRequestModel model, CancellationToken cancellationToken)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return Unauthorized();

        if (model is null || (!model.VehicleConfigurationId.HasValue && string.IsNullOrWhiteSpace(model.Vin)))
            return BadRequest(new { reasonCode = "garage.invalid_vehicle" });

        try
        {
            var saved = await _garageService.AddVehicleAsync(customer.Id, model.VehicleConfigurationId, model.Vin, model.Label, cancellationToken);
            return Json(saved);
        }
        catch (GarageVinDisambiguationException exception)
        {
            var labels = await _vehicleAdminService.GetConfigurationDisplayLabelsAsync(
                exception.Candidates.Select(candidate => candidate.VehicleConfigurationId),
                cancellationToken);

            return Conflict(new
            {
                reasonCode = exception.Message,
                candidates = exception.Candidates.Select(candidate => new
                {
                    vehicleConfigurationId = candidate.VehicleConfigurationId,
                    confidence = candidate.Confidence.Value,
                    modelYear = candidate.ModelYear,
                    label = labels.TryGetValue(candidate.VehicleConfigurationId, out var label)
                        ? label
                        : $"Vehicle #{candidate.VehicleConfigurationId}"
                })
            });
        }
    }

    [HttpPut]
    public async Task<IActionResult> SetActive([FromBody] GarageSetActiveRequestModel model, CancellationToken cancellationToken)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return Unauthorized();

        var ok = await _garageService.SetActiveVehicleAsync(customer.Id, model.GarageVehicleId, cancellationToken);
        return ok ? Ok() : NotFound();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearActive([FromBody] GarageClearActiveRequestModel model, CancellationToken cancellationToken)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return Unauthorized();

        var ok = await _garageService.ClearActiveVehicleAsync(customer.Id, model.Confirmed, cancellationToken);
        return ok ? Ok() : BadRequest(new { reasonCode = "garage.clear_not_confirmed" });
    }

    [HttpDelete]
    public async Task<IActionResult> RemoveVehicle([FromBody] GarageRemoveVehicleRequestModel model, CancellationToken cancellationToken)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return Unauthorized();

        if (model is null || model.GarageVehicleId <= 0)
            return BadRequest(new { reasonCode = "garage.invalid_vehicle" });

        var ok = await _garageService.RemoveVehicleAsync(customer.Id, model.GarageVehicleId, cancellationToken);
        return ok ? Ok() : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> SaveOem([FromBody] GarageSaveOemRequestModel model, CancellationToken cancellationToken)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return Unauthorized();

        var ok = await _garageService.SaveOemAsync(customer.Id, model.OemNumber, model.ManufacturerId, cancellationToken);
        return ok ? Ok() : BadRequest(new { reasonCode = "garage.oem_save_failed" });
    }

    [HttpGet]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return Unauthorized();

        return Json(await _subjectDataService.ExportAsync(
            customer.Id,
            $"customer:{customer.Id}",
            cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Erase(
        [FromBody] GarageEraseRequestModel model,
        CancellationToken cancellationToken)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return Unauthorized();
        if (model is null || !model.Confirmed)
            return BadRequest(new { reasonCode = "garage.erase_not_confirmed" });

        await _privacyService.EraseAsync(
            customer.Id,
            $"customer:{customer.Id}",
            cancellationToken);
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Migrate([FromBody] GarageMigrateRequestModel model, CancellationToken cancellationToken)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return Unauthorized();

        if (model is null || string.IsNullOrWhiteSpace(model.GuestKey))
            return BadRequest(new { reasonCode = "garage.invalid_guest_key" });

        if (model.Payload is null ||
            model.Payload.Vehicles.Count > MaxGuestVehicles ||
            model.Payload.Oems.Count > MaxGuestOems ||
            model.Payload.Vehicles.Any(x =>
                (!x.VehicleConfigurationId.HasValue && string.IsNullOrWhiteSpace(x.Vin)) ||
                x.Vin?.Length > 64 ||
                x.Label?.Length > 256))
            return BadRequest(new { reasonCode = "garage.invalid_guest_payload" });

        var payload = new GarageGuestPayload
        {
            ActiveVehicleId = model.Payload.ActiveVehicleId,
            Vehicles = model.Payload.Vehicles.Select((x, index) => new GarageVehicle
            {
                Id = x.Id > 0 ? x.Id : index + 1,
                VehicleConfigurationId = x.VehicleConfigurationId,
                Vin = x.Vin,
                Label = x.Label
            }).ToList(),
            Oems = model.Payload.Oems.Select((x, index) => new GarageOem
            {
                Id = x.Id > 0 ? x.Id : index + 1,
                OemNumberId = x.OemNumberId,
                DisplayNumber = x.DisplayNumber
            }).ToList()
        };

        var ok = await _garageService.MigrateGuestAsync(customer.Id, model.GuestKey.Trim(), payload, cancellationToken);
        return ok ? Ok() : BadRequest(new { reasonCode = "garage.invalid_guest_payload" });
    }
}
