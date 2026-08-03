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

public sealed class GarageSaveOemRequestModel
{
    public string OemNumber { get; set; } = string.Empty;

    public int? ManufacturerId { get; set; }
}

public sealed class GarageMigrateRequestModel
{
    public string GuestKey { get; set; } = string.Empty;
}

public sealed class GarageGuestPayloadModel
{
    public List<GarageGuestVehicleModel> Vehicles { get; set; } = [];

    public List<GarageGuestOemModel> Oems { get; set; } = [];

    public int? ActiveVehicleId { get; set; }
}

public sealed class GarageGuestVehicleModel
{
    public int? VehicleConfigurationId { get; set; }

    public string? Vin { get; set; }

    public string Label { get; set; } = string.Empty;
}

public sealed class GarageGuestOemModel
{
    public int OemNumberId { get; set; }

    public string? DisplayNumber { get; set; }
}
