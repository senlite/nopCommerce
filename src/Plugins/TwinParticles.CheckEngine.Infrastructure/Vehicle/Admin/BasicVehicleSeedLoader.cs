using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

public sealed class BasicVehicleSeedLoader : IVehicleSeedLoader
{
    private readonly IVehicleAdminRepository _repository;

    public BasicVehicleSeedLoader(IVehicleAdminRepository repository)
    {
        _repository = repository;
    }

    public async Task<VehicleSeedLoadResult> SeedAsync(CancellationToken cancellationToken)
    {
        var result = new VehicleSeedLoadResult();

        var makes = await _repository.GetMakesAsync(cancellationToken);
        if (makes.Count == 0)
        {
            await _repository.CreateMakeAsync(new VehicleMake { Code = "GEN", Name = "Generic", IsActive = true }, cancellationToken);
            result.MakesInserted++;
        }

        var models = await _repository.GetModelsAsync(cancellationToken);
        if (models.Count == 0)
        {
            var firstMake = (await _repository.GetMakesAsync(cancellationToken))[0];
            await _repository.CreateModelAsync(new VehicleModel { MakeId = firstMake.Id, Code = "BASE", Name = "Base", IsActive = true }, cancellationToken);
            result.ModelsInserted++;
        }

        var generations = await _repository.GetGenerationsAsync(cancellationToken);
        if (generations.Count == 0)
        {
            var firstModel = (await _repository.GetModelsAsync(cancellationToken))[0];
            await _repository.CreateGenerationAsync(new VehicleGeneration { ModelId = firstModel.Id, Code = "GEN1", Name = "Generation 1", StartYear = 2000, EndYear = null, IsActive = true }, cancellationToken);
            result.GenerationsInserted++;
        }

        var bodies = await _repository.GetBodiesAsync(cancellationToken);
        if (bodies.Count == 0)
        {
            var firstGeneration = (await _repository.GetGenerationsAsync(cancellationToken))[0];
            await _repository.CreateBodyAsync(new VehicleBody { GenerationId = firstGeneration.Id, Code = "SEDAN", Name = "Sedan", Doors = 4, IsActive = true }, cancellationToken);
            result.BodiesInserted++;
        }

        var engines = await _repository.GetEnginesAsync(cancellationToken);
        if (engines.Count == 0)
        {
            var firstBody = (await _repository.GetBodiesAsync(cancellationToken))[0];
            await _repository.CreateEngineAsync(new VehicleEngine { BodyId = firstBody.Id, Code = "I4-2000", Name = "Inline 4", FuelType = "Petrol", DisplacementCc = 2000, PowerHp = 150, IsActive = true }, cancellationToken);
            result.EnginesInserted++;
        }

        var markets = await _repository.GetMarketsAsync(cancellationToken);
        if (markets.Count == 0)
        {
            await _repository.CreateMarketAsync(new VehicleMarket { Code = "GLOBAL", Name = "Global", IsActive = true }, cancellationToken);
            result.MarketsInserted++;
        }

        var configurations = await _repository.GetConfigurationsAsync(cancellationToken);
        if (configurations.Count == 0)
        {
            var firstGeneration = (await _repository.GetGenerationsAsync(cancellationToken))[0];
            var firstBody = (await _repository.GetBodiesAsync(cancellationToken))[0];
            var firstEngine = (await _repository.GetEnginesAsync(cancellationToken))[0];
            var firstMarket = (await _repository.GetMarketsAsync(cancellationToken))[0];

            await _repository.CreateConfigurationAsync(new VehicleConfiguration
            {
                GenerationId = firstGeneration.Id,
                BodyId = firstBody.Id,
                EngineId = firstEngine.Id,
                MarketId = firstMarket.Id,
                TrimName = "Base Trim",
                ProductionFromYear = 2000,
                ProductionToYear = null,
                Fingerprint = "GEN-BASE-GEN1-SEDAN-I4-2000-GLOBAL",
                IsActive = true
            }, cancellationToken);

            result.ConfigurationsInserted++;
        }

        var aliases = await _repository.GetAliasesAsync(cancellationToken);
        if (aliases.Count == 0)
        {
            var firstMake = (await _repository.GetMakesAsync(cancellationToken))[0];
            await _repository.CreateAliasAsync(new VehicleAlias
            {
                NodeType = "make",
                NodeId = firstMake.Id,
                Locale = "en",
                AliasText = "Generic",
                NormalizedAlias = "generic"
            }, cancellationToken);
            result.AliasesInserted++;
        }

        return result;
    }
}
