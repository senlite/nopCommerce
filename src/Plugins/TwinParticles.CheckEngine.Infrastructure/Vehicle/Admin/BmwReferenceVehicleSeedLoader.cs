using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

/// <summary>
/// Seeds the curated BMW reference dataset (H1.4). Idempotent: if any make already exists the loader
/// does nothing, so it is safe to run repeatedly and never overwrites operator edits.
///
/// The repository's create methods do not return generated identities, so each hierarchy level is
/// inserted and then re-read once to resolve parent identifiers before inserting its children.
/// </summary>
public sealed class BmwReferenceVehicleSeedLoader : IVehicleSeedLoader
{
    private readonly IVehicleAdminRepository _repository;

    public BmwReferenceVehicleSeedLoader(IVehicleAdminRepository repository)
    {
        _repository = repository;
    }

    public async Task<VehicleSeedLoadResult> SeedAsync(CancellationToken cancellationToken)
    {
        var result = new VehicleSeedLoadResult();

        var existingMakes = await _repository.GetMakesAsync(cancellationToken);
        if (existingMakes.Count > 0)
            return result;

        await _repository.CreateMakeAsync(
            new VehicleMake { Code = BmwReferenceCatalog.MakeCode, Name = BmwReferenceCatalog.MakeName, IsActive = true },
            cancellationToken);
        result.MakesInserted++;

        var make = (await _repository.GetMakesAsync(cancellationToken))
            .Single(x => x.Code == BmwReferenceCatalog.MakeCode);

        await SeedMarketsAsync(result, cancellationToken);
        var marketsByCode = (await _repository.GetMarketsAsync(cancellationToken))
            .ToDictionary(x => x.Code, x => x.Id);

        await SeedModelsAsync(make.Id, result, cancellationToken);
        var modelsByCode = (await _repository.GetModelsAsync(cancellationToken))
            .Where(x => x.MakeId == make.Id)
            .ToDictionary(x => x.Code, x => x.Id);

        await SeedGenerationsAsync(modelsByCode, result, cancellationToken);
        var generationsByKey = (await _repository.GetGenerationsAsync(cancellationToken))
            .ToDictionary(x => (x.ModelId, x.Code), x => x.Id);

        await SeedBodiesAsync(modelsByCode, generationsByKey, result, cancellationToken);
        var bodiesByKey = (await _repository.GetBodiesAsync(cancellationToken))
            .ToDictionary(x => (x.GenerationId, x.Code), x => x.Id);

        await SeedEnginesAsync(modelsByCode, generationsByKey, bodiesByKey, result, cancellationToken);
        var enginesByKey = (await _repository.GetEnginesAsync(cancellationToken))
            .ToDictionary(x => (x.BodyId, x.Code), x => x.Id);

        await SeedConfigurationsAsync(modelsByCode, generationsByKey, bodiesByKey, enginesByKey, marketsByCode, result, cancellationToken);

        await SeedAliasesAsync(make.Id, modelsByCode, generationsByKey, result, cancellationToken);

        return result;
    }

    private async Task SeedMarketsAsync(VehicleSeedLoadResult result, CancellationToken cancellationToken)
    {
        foreach (var market in BmwReferenceCatalog.Markets)
        {
            await _repository.CreateMarketAsync(
                new VehicleMarket { Code = market.Code, Name = market.Name, IsActive = true }, cancellationToken);
            result.MarketsInserted++;
        }
    }

    private async Task SeedModelsAsync(int makeId, VehicleSeedLoadResult result, CancellationToken cancellationToken)
    {
        foreach (var model in BmwReferenceCatalog.Models)
        {
            await _repository.CreateModelAsync(
                new VehicleModel { MakeId = makeId, Code = model.Code, Name = model.Name, IsActive = true }, cancellationToken);
            result.ModelsInserted++;
        }
    }

    private async Task SeedGenerationsAsync(
        IReadOnlyDictionary<string, int> modelsByCode,
        VehicleSeedLoadResult result,
        CancellationToken cancellationToken)
    {
        foreach (var model in BmwReferenceCatalog.Models)
        {
            var modelId = modelsByCode[model.Code];
            foreach (var generation in model.Generations)
            {
                await _repository.CreateGenerationAsync(new VehicleGeneration
                {
                    ModelId = modelId,
                    Code = generation.Code,
                    Name = generation.Name,
                    StartYear = generation.StartYear,
                    EndYear = generation.EndYear,
                    IsActive = true
                }, cancellationToken);
                result.GenerationsInserted++;
            }
        }
    }

    private async Task SeedBodiesAsync(
        IReadOnlyDictionary<string, int> modelsByCode,
        IReadOnlyDictionary<(int ModelId, string Code), int> generationsByKey,
        VehicleSeedLoadResult result,
        CancellationToken cancellationToken)
    {
        foreach (var model in BmwReferenceCatalog.Models)
        {
            var modelId = modelsByCode[model.Code];
            foreach (var generation in model.Generations)
            {
                var generationId = generationsByKey[(modelId, generation.Code)];
                foreach (var body in generation.Bodies)
                {
                    await _repository.CreateBodyAsync(new VehicleBody
                    {
                        GenerationId = generationId,
                        Code = body.Code,
                        Name = body.Name,
                        Doors = body.Doors,
                        IsActive = true
                    }, cancellationToken);
                    result.BodiesInserted++;
                }
            }
        }
    }

    private async Task SeedEnginesAsync(
        IReadOnlyDictionary<string, int> modelsByCode,
        IReadOnlyDictionary<(int ModelId, string Code), int> generationsByKey,
        IReadOnlyDictionary<(int GenerationId, string Code), int> bodiesByKey,
        VehicleSeedLoadResult result,
        CancellationToken cancellationToken)
    {
        foreach (var model in BmwReferenceCatalog.Models)
        {
            var modelId = modelsByCode[model.Code];
            foreach (var generation in model.Generations)
            {
                var generationId = generationsByKey[(modelId, generation.Code)];
                var engineByCode = generation.Engines.ToDictionary(x => x.Code);

                // An engine row is scoped to a body (BodyId). Create only the (body, engine) pairs the
                // generation's trims actually use, so every engine row is reachable from a configuration.
                var requiredPairs = generation.Trims
                    .Select(trim => (trim.BodyCode, trim.EngineCode))
                    .Distinct();

                foreach (var (bodyCode, engineCode) in requiredPairs)
                {
                    var engine = engineByCode[engineCode];
                    await _repository.CreateEngineAsync(new VehicleEngine
                    {
                        BodyId = bodiesByKey[(generationId, bodyCode)],
                        Code = engine.Code,
                        Name = engine.Name,
                        FuelType = engine.FuelType,
                        DisplacementCc = engine.DisplacementCc,
                        PowerHp = engine.PowerHp,
                        IsActive = true
                    }, cancellationToken);
                    result.EnginesInserted++;
                }
            }
        }
    }

    private async Task SeedConfigurationsAsync(
        IReadOnlyDictionary<string, int> modelsByCode,
        IReadOnlyDictionary<(int ModelId, string Code), int> generationsByKey,
        IReadOnlyDictionary<(int GenerationId, string Code), int> bodiesByKey,
        IReadOnlyDictionary<(int BodyId, string Code), int> enginesByKey,
        IReadOnlyDictionary<string, int> marketsByCode,
        VehicleSeedLoadResult result,
        CancellationToken cancellationToken)
    {
        foreach (var model in BmwReferenceCatalog.Models)
        {
            var modelId = modelsByCode[model.Code];
            foreach (var generation in model.Generations)
            {
                var generationId = generationsByKey[(modelId, generation.Code)];
                foreach (var trim in generation.Trims)
                {
                    var bodyId = bodiesByKey[(generationId, trim.BodyCode)];
                    var engineId = enginesByKey[(bodyId, trim.EngineCode)];

                    foreach (var market in BmwReferenceCatalog.Markets)
                    {
                        var fingerprint = string.Join('-',
                            BmwReferenceCatalog.MakeCode,
                            model.Code,
                            generation.Code,
                            trim.BodyCode,
                            trim.EngineCode,
                            market.Code,
                            Slug(trim.TrimName));

                        await _repository.CreateConfigurationAsync(new VehicleConfiguration
                        {
                            GenerationId = generationId,
                            BodyId = bodyId,
                            EngineId = engineId,
                            MarketId = marketsByCode[market.Code],
                            TrimName = trim.TrimName,
                            ProductionFromYear = generation.StartYear,
                            ProductionToYear = generation.EndYear,
                            Fingerprint = fingerprint,
                            IsActive = true
                        }, cancellationToken);
                        result.ConfigurationsInserted++;
                    }
                }
            }
        }
    }

    private async Task SeedAliasesAsync(
        int makeId,
        IReadOnlyDictionary<string, int> modelsByCode,
        IReadOnlyDictionary<(int ModelId, string Code), int> generationsByKey,
        VehicleSeedLoadResult result,
        CancellationToken cancellationToken)
    {
        await AddAliasAsync("make", makeId, "en", BmwReferenceCatalog.MakeName, result, cancellationToken);
        await AddAliasAsync("make", makeId, "ar", BmwReferenceCatalog.MakeArabicAlias, result, cancellationToken);

        foreach (var model in BmwReferenceCatalog.Models)
        {
            var modelId = modelsByCode[model.Code];
            await AddAliasAsync("model", modelId, "en", model.Name, result, cancellationToken);
            await AddAliasAsync("model", modelId, "ar", model.ArabicAlias, result, cancellationToken);

            // The generation code (e.g. F30) is a first-class searchable alias (AC-014.1).
            foreach (var generation in model.Generations)
            {
                var generationId = generationsByKey[(modelId, generation.Code)];
                await AddAliasAsync("generation", generationId, "en", generation.Code, result, cancellationToken);
            }
        }
    }

    private async Task AddAliasAsync(
        string nodeType,
        int nodeId,
        string locale,
        string aliasText,
        VehicleSeedLoadResult result,
        CancellationToken cancellationToken)
    {
        await _repository.CreateAliasAsync(new VehicleAlias
        {
            NodeType = nodeType,
            NodeId = nodeId,
            Locale = locale,
            AliasText = aliasText,
            NormalizedAlias = aliasText.Trim().ToLowerInvariant()
        }, cancellationToken);
        result.AliasesInserted++;
    }

    private static string Slug(string value)
    {
        var buffer = new char[value.Length];
        var index = 0;
        foreach (var character in value.Trim().ToLower(CultureInfo.InvariantCulture))
        {
            if (char.IsLetterOrDigit(character))
                buffer[index++] = character;
            else if (index > 0 && buffer[index - 1] != '-')
                buffer[index++] = '-';
        }

        return new string(buffer, 0, index).Trim('-');
    }
}
