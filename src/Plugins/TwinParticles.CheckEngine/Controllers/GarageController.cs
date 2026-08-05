using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using Nop.Web.Controllers;
using TwinParticles.CheckEngine.Application.Garage;
using TwinParticles.CheckEngine.Domain.Garage;
using TwinParticles.CheckEngine.Models;

namespace TwinParticles.CheckEngine.Controllers;

public sealed class GarageController : BasePublicController
{
    private readonly GarageService _garageService;
    private readonly ICustomerService _customerService;
    private readonly IWorkContext _workContext;

    public GarageController(GarageService garageService, ICustomerService customerService, IWorkContext workContext)
    {
        _garageService = garageService;
        _customerService = customerService;
        _workContext = workContext;
    }

    [HttpGet]
    public async Task<IActionResult> Current(CancellationToken cancellationToken)
    {
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

        var saved = await _garageService.AddVehicleAsync(customer.Id, model.VehicleConfigurationId, model.Vin, model.Label, cancellationToken);
        return Json(saved);
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

    [HttpPost]
    public async Task<IActionResult> SaveOem([FromBody] GarageSaveOemRequestModel model, CancellationToken cancellationToken)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return Unauthorized();

        var ok = await _garageService.SaveOemAsync(customer.Id, model.OemNumber, model.ManufacturerId, cancellationToken);
        return ok ? Ok() : BadRequest(new { reasonCode = "garage.oem_save_failed" });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Migrate([FromBody] GarageMigrateRequestModel model, CancellationToken cancellationToken)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return Unauthorized();

        if (model is null || string.IsNullOrWhiteSpace(model.GuestKey))
            return BadRequest(new { reasonCode = "garage.invalid_guest_key" });

        var ok = await _garageService.MigrateGuestAsync(customer.Id, model.GuestKey.Trim(), cancellationToken);
        return ok ? Ok() : NotFound();
    }

    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Guest(string guestKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(guestKey))
            return BadRequest(new { reasonCode = "garage.invalid_guest_key" });

        var payload = await _garageService.GetGuestAsync(guestKey.Trim(), cancellationToken);
        return Json(payload);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Guest(string guestKey, [FromBody] GarageGuestPayloadModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(guestKey) || model is null)
            return BadRequest(new { reasonCode = "garage.invalid_guest_payload" });

        var payload = new GarageGuestPayload
        {
            ActiveVehicleId = model.ActiveVehicleId,
            Vehicles = model.Vehicles.Select(x => new GarageVehicle
            {
                VehicleConfigurationId = x.VehicleConfigurationId,
                Vin = x.Vin,
                Label = x.Label
            }).ToList(),
            Oems = model.Oems.Select(x => new GarageOem
            {
                OemNumberId = x.OemNumberId,
                DisplayNumber = x.DisplayNumber
            }).ToList()
        };

        await _garageService.SetGuestAsync(guestKey.Trim(), payload, cancellationToken);
        return Ok();
    }
}
