using TwinParticles.CheckEngine.Domain.Oem;

namespace TwinParticles.CheckEngine.Models;

public sealed class OemAdminDtos
{
    public sealed class ManufacturerUpsertModel : Manufacturer;

    public sealed class OemNumberUpsertModel : OemNumber;

    public sealed class OemRelationUpsertModel : OemRelation;
}
