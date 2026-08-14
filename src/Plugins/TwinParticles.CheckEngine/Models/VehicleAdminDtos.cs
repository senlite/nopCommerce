using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Models;

public sealed class VehicleAdminDtos
{
    public sealed class ArchiveRequestModel
    {
        public int Id { get; set; }
    }

    public sealed class MergeRequestModel
    {
        public int SourceId { get; set; }
        public int TargetId { get; set; }
    }

    public sealed class LifecycleResultModel
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public int MovedChildren { get; set; }
        public int MovedAliases { get; set; }
    }

    public sealed class SeedResultModel
    {
        public int MakesInserted { get; set; }
        public int ModelsInserted { get; set; }
        public int GenerationsInserted { get; set; }
        public int BodiesInserted { get; set; }
        public int EnginesInserted { get; set; }
        public int MarketsInserted { get; set; }
        public int ConfigurationsInserted { get; set; }
        public int AliasesInserted { get; set; }
    }

    public sealed class MakeUpsertModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public VehicleMake ToEntity() => new()
        {
            Id = Id,
            Code = Code,
            Name = Name,
            IsActive = IsActive
        };
    }

    public sealed class ModelUpsertModel
    {
        public int Id { get; set; }
        public int MakeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public VehicleModel ToEntity() => new()
        {
            Id = Id,
            MakeId = MakeId,
            Code = Code,
            Name = Name,
            IsActive = IsActive
        };
    }

    public sealed class GenerationUpsertModel
    {
        public int Id { get; set; }
        public int ModelId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int StartYear { get; set; }
        public int? EndYear { get; set; }
        public bool IsActive { get; set; } = true;

        public VehicleGeneration ToEntity() => new()
        {
            Id = Id,
            ModelId = ModelId,
            Code = Code,
            Name = Name,
            StartYear = StartYear,
            EndYear = EndYear,
            IsActive = IsActive
        };
    }

    public sealed class BodyUpsertModel
    {
        public int Id { get; set; }
        public int GenerationId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Doors { get; set; }
        public bool IsActive { get; set; } = true;

        public VehicleBody ToEntity() => new()
        {
            Id = Id,
            GenerationId = GenerationId,
            Code = Code,
            Name = Name,
            Doors = Doors,
            IsActive = IsActive
        };
    }

    public sealed class EngineUpsertModel
    {
        public int Id { get; set; }
        public int BodyId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FuelType { get; set; } = string.Empty;
        public int DisplacementCc { get; set; }
        public int PowerHp { get; set; }
        public bool IsActive { get; set; } = true;

        public VehicleEngine ToEntity() => new()
        {
            Id = Id,
            BodyId = BodyId,
            Code = Code,
            Name = Name,
            FuelType = FuelType,
            DisplacementCc = DisplacementCc,
            PowerHp = PowerHp,
            IsActive = IsActive
        };
    }

    public sealed class MarketUpsertModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public VehicleMarket ToEntity() => new()
        {
            Id = Id,
            Code = Code,
            Name = Name,
            IsActive = IsActive
        };
    }

    public sealed class ConfigurationUpsertModel
    {
        public int Id { get; set; }
        public int GenerationId { get; set; }
        public int? BodyId { get; set; }
        public int? EngineId { get; set; }
        public int? MarketId { get; set; }
        public string TrimName { get; set; } = string.Empty;
        public int? ProductionFromYear { get; set; }
        public int? ProductionToYear { get; set; }
        public string Fingerprint { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public VehicleConfiguration ToEntity() => new()
        {
            Id = Id,
            GenerationId = GenerationId,
            BodyId = BodyId,
            EngineId = EngineId,
            MarketId = MarketId,
            TrimName = TrimName,
            ProductionFromYear = ProductionFromYear,
            ProductionToYear = ProductionToYear,
            Fingerprint = Fingerprint,
            IsActive = IsActive
        };
    }

    public sealed class AliasUpsertModel
    {
        public int Id { get; set; }
        public string NodeType { get; set; } = string.Empty;
        public int NodeId { get; set; }
        public string Locale { get; set; } = string.Empty;
        public string AliasText { get; set; } = string.Empty;
        public string NormalizedAlias { get; set; } = string.Empty;

        public VehicleAlias ToEntity() => new()
        {
            Id = Id,
            NodeType = NodeType,
            NodeId = NodeId,
            Locale = Locale,
            AliasText = AliasText,
            NormalizedAlias = NormalizedAlias
        };
    }
}
