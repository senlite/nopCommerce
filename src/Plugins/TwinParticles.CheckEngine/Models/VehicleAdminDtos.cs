using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Models;

public sealed class VehicleAdminDtos
{
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

    public sealed class MakeUpsertModel : VehicleMake;
    public sealed class ModelUpsertModel : VehicleModel;
    public sealed class GenerationUpsertModel : VehicleGeneration;
    public sealed class BodyUpsertModel : VehicleBody;
    public sealed class EngineUpsertModel : VehicleEngine;
    public sealed class MarketUpsertModel : VehicleMarket;
    public sealed class ConfigurationUpsertModel : VehicleConfiguration;
    public sealed class AliasUpsertModel : VehicleAlias;
}
