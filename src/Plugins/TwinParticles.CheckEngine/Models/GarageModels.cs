using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Models;

public sealed class GarageAddVehicleRequestModel
{
    public int? VehicleConfigurationId { get; set; }

    public string? Vin { get; set; }

    public string? Label { get; set; }
}

public sealed class GarageSetActiveRequestModel
{
    public int GarageVehicleId { get; set; }
}

public sealed class GarageClearActiveRequestModel
{
    public bool Confirmed { get; set; }
}

public sealed class GarageRemoveVehicleRequestModel
{
    public int GarageVehicleId { get; set; }
}

public sealed class GarageSaveOemRequestModel
{
    public string OemNumber { get; set; } = string.Empty;

    public int? ManufacturerId { get; set; }
}

public sealed class GarageMigrateRequestModel
{
    public string GuestKey { get; set; } = string.Empty;

    /// <summary>
    /// The browser-local guest garage. Guest data intentionally remains in localStorage until the
    /// customer signs in, then travels with this authenticated migration request. This makes the
    /// merge durable across app restarts and multi-node deployments without storing guest VINs on
    /// the server before account creation.
    /// </summary>
    public GarageGuestPayloadModel Payload { get; set; } = new();
}

public sealed class GarageGuestPayloadModel
{
    public List<GarageGuestVehicleModel> Vehicles { get; set; } = [];

    public List<GarageGuestOemModel> Oems { get; set; } = [];

    public int? ActiveVehicleId { get; set; }
}

public sealed class GarageGuestVehicleModel
{
    public int Id { get; set; }

    public int? VehicleConfigurationId { get; set; }

    public string? Vin { get; set; }

    public string Label { get; set; } = string.Empty;
}

public sealed class GarageGuestOemModel
{
    public int Id { get; set; }

    public int OemNumberId { get; set; }

    public string? DisplayNumber { get; set; }
}
