using System;
using TwinParticles.CheckEngine.Domain.Oem;

namespace TwinParticles.CheckEngine.Models;

public sealed class OemAdminDtos
{
    public sealed class ManufacturerUpsertModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsOeBrand { get; set; } = true;
        public bool IsActive { get; set; } = true;

        public Manufacturer ToEntity() => new()
        {
            Id = Id,
            Code = Code,
            Name = Name,
            IsOeBrand = IsOeBrand,
            IsActive = IsActive
        };
    }

    public sealed class OemNumberUpsertModel
    {
        public int Id { get; set; }
        public int ManufacturerId { get; set; }
        public string DisplayNumber { get; set; } = string.Empty;
        public string NormalizedNumber { get; set; } = string.Empty;
        public bool IsObsolete { get; set; }
        public bool IsActive { get; set; } = true;

        public OemNumber ToEntity() => new()
        {
            Id = Id,
            ManufacturerId = ManufacturerId,
            DisplayNumber = DisplayNumber,
            NormalizedNumber = NormalizedNumber,
            IsObsolete = IsObsolete,
            IsActive = IsActive
        };
    }

    public sealed class OemRelationUpsertModel
    {
        public int Id { get; set; }
        public int FromOemNumberId { get; set; }
        public int ToOemNumberId { get; set; }
        public OemRelationType RelationType { get; set; }
        public DateTime? ValidFromUtc { get; set; }
        public DateTime? ValidToUtc { get; set; }
        public bool IsActive { get; set; } = true;

        public OemRelation ToEntity() => new()
        {
            Id = Id,
            FromOemNumberId = FromOemNumberId,
            ToOemNumberId = ToOemNumberId,
            RelationType = RelationType,
            ValidFromUtc = ValidFromUtc,
            ValidToUtc = ValidToUtc,
            IsActive = IsActive
        };
    }
}
