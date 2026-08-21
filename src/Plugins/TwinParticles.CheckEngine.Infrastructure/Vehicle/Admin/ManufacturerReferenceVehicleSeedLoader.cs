using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

public sealed class ManufacturerReferenceVehicleSeedLoader
{
    private static readonly (string Code, string Name, string ArabicAlias)[] DefaultMarkets =
    [
        ("ECE", "Europe", "أوروبا"),
        ("GCC", "Gulf", "الخليج")
    ];

    private readonly IVehicleAdminRepository _repository;
    private readonly VehicleReferenceCatalogDocument _catalog;

    public ManufacturerReferenceVehicleSeedLoader(
        IVehicleAdminRepository repository,
        VehicleReferenceCatalogDocument catalog)
    {
        _repository = repository;
        _catalog = catalog;
    }

    public async Task<VehicleSeedLoadResult> SeedAsync(CancellationToken cancellationToken)
    {
        var result = new VehicleSeedLoadResult();

        var existingMakes = await _repository.GetMakesAsync(cancellationToken);
        var make = existingMakes.FirstOrDefault(x => x.Code == _catalog.MakeCode);
        if (make is null)
        {
            await _repository.CreateMakeAsync(
                new VehicleMake { Code = _catalog.MakeCode, Name = _catalog.MakeName, IsActive = true },
                cancellationToken);
            result.MakesInserted++;
            make = (await _repository.GetMakesAsync(cancellationToken))
                .Single(x => x.Code == _catalog.MakeCode);
        }

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
        var existing = (await _repository.GetMarketsAsync(cancellationToken))
            .Select(x => x.Code)
            .ToHashSet();

        foreach (var market in DefaultMarkets)
        {
            if (existing.Contains(market.Code))
                continue;

            await _repository.CreateMarketAsync(
                new VehicleMarket { Code = market.Code, Name = market.Name, IsActive = true }, cancellationToken);
            result.MarketsInserted++;
        }
    }

    private async Task SeedModelsAsync(int makeId, VehicleSeedLoadResult result, CancellationToken cancellationToken)
    {
        var existing = (await _repository.GetModelsAsync(cancellationToken))
            .Where(x => x.MakeId == makeId)
            .Select(x => x.Code)
            .ToHashSet();

        foreach (var model in _catalog.Models)
        {
            if (existing.Contains(model.Code))
                continue;

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
        var existing = (await _repository.GetGenerationsAsync(cancellationToken))
            .Select(x => (x.ModelId, x.Code))
            .ToHashSet();

        foreach (var model in _catalog.Models)
        {
            var modelId = modelsByCode[model.Code];
            foreach (var generation in model.Generations)
            {
                if (existing.Contains((modelId, generation.Code)))
                    continue;

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
        var existing = (await _repository.GetBodiesAsync(cancellationToken))
            .Select(x => (x.GenerationId, x.Code))
            .ToHashSet();

        foreach (var model in _catalog.Models)
        {
            var modelId = modelsByCode[model.Code];
            foreach (var generation in model.Generations)
            {
                var generationId = generationsByKey[(modelId, generation.Code)];
                foreach (var body in generation.Bodies)
                {
                    if (existing.Contains((generationId, body.Code)))
                        continue;

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
        var existing = (await _repository.GetEnginesAsync(cancellationToken))
            .Select(x => (x.BodyId, x.Code))
            .ToHashSet();

        foreach (var model in _catalog.Models)
        {
            var modelId = modelsByCode[model.Code];
            foreach (var generation in model.Generations)
            {
                var generationId = generationsByKey[(modelId, generation.Code)];
                var engineByCode = generation.Engines.ToDictionary(x => x.Code);
                var requiredPairs = generation.Trims
                    .Select(trim => (trim.BodyCode, trim.EngineCode))
                    .Distinct();

                foreach (var (bodyCode, engineCode) in requiredPairs)
                {
                    var bodyId = bodiesByKey[(generationId, bodyCode)];
                    if (existing.Contains((bodyId, engineCode)))
                        continue;

                    var engine = engineByCode[engineCode];
                    await _repository.CreateEngineAsync(new VehicleEngine
                    {
                        BodyId = bodyId,
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
        var existingFingerprints = (await _repository.GetConfigurationsAsync(cancellationToken))
            .Select(x => x.Fingerprint)
            .ToHashSet();

        foreach (var model in _catalog.Models)
        {
            var modelId = modelsByCode[model.Code];
            foreach (var generation in model.Generations)
            {
                var generationId = generationsByKey[(modelId, generation.Code)];
                foreach (var trim in generation.Trims)
                {
                    var bodyId = bodiesByKey[(generationId, trim.BodyCode)];
                    var engineId = enginesByKey[(bodyId, trim.EngineCode)];

                    foreach (var market in DefaultMarkets)
                    {
                        var fingerprint = BuildFingerprint(model, generation, trim, market.Code);
                        if (existingFingerprints.Contains(fingerprint))
                            continue;

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
                        existingFingerprints.Add(fingerprint);
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
        var existingNormalized = (await _repository.GetAliasesAsync(cancellationToken))
            .Select(alias => (alias.NodeType, alias.NormalizedAlias))
            .ToHashSet();

        await AddAliasIfMissingAsync("make", makeId, "en", _catalog.MakeName, existingNormalized, result, cancellationToken);
        await AddAliasIfMissingAsync("make", makeId, "ar", _catalog.MakeArabicAlias, existingNormalized, result, cancellationToken);

        foreach (var model in _catalog.Models)
        {
            var modelId = modelsByCode[model.Code];
            await AddAliasIfMissingAsync("model", modelId, "en", model.Name, existingNormalized, result, cancellationToken);
            await AddAliasIfMissingAsync("model", modelId, "ar", model.ArabicAlias, existingNormalized, result, cancellationToken);

            foreach (var generation in model.Generations)
            {
                var generationId = generationsByKey[(modelId, generation.Code)];
                await AddAliasIfMissingAsync("generation", generationId, "en", generation.Code, existingNormalized, result, cancellationToken);
            }
        }

        await SeedConfigurationAliasesAsync(makeId, existingNormalized, result, cancellationToken);
    }

    private async Task SeedConfigurationAliasesAsync(
        int makeId,
        HashSet<(string NodeType, string NormalizedAlias)> existingNormalized,
        VehicleSeedLoadResult result,
        CancellationToken cancellationToken)
    {
        var models = (await _repository.GetModelsAsync(cancellationToken))
            .Where(model => model.MakeId == makeId)
            .ToDictionary(model => model.Id);
        var generations = (await _repository.GetGenerationsAsync(cancellationToken))
            .Where(generation => models.ContainsKey(generation.ModelId))
            .ToDictionary(generation => generation.Id);
        var bodies = (await _repository.GetBodiesAsync(cancellationToken)).ToDictionary(body => body.Id);
        var engines = (await _repository.GetEnginesAsync(cancellationToken)).ToDictionary(engine => engine.Id);
        var markets = (await _repository.GetMarketsAsync(cancellationToken)).ToDictionary(market => market.Id);

        var modelArabicByCode = _catalog.Models
            .ToDictionary(model => model.Code, model => model.ArabicAlias);
        var marketArabicByCode = DefaultMarkets.ToDictionary(market => market.Code, market => market.ArabicAlias);

        foreach (var configuration in (await _repository.GetConfigurationsAsync(cancellationToken))
                     .Where(configuration => generations.ContainsKey(configuration.GenerationId)))
        {
            var generation = generations[configuration.GenerationId];
            var model = models[generation.ModelId];
            var bodyCode = configuration.BodyId.HasValue && bodies.TryGetValue(configuration.BodyId.Value, out var body)
                ? body.Code
                : "ANY";
            var engineCode = configuration.EngineId.HasValue && engines.TryGetValue(configuration.EngineId.Value, out var engine)
                ? engine.Code
                : "ANY";
            var marketCode = configuration.MarketId.HasValue && markets.TryGetValue(configuration.MarketId.Value, out var market)
                ? market.Code
                : "ALL";
            var modelArabic = modelArabicByCode.GetValueOrDefault(model.Code, model.Name);
            var marketArabic = marketArabicByCode.GetValueOrDefault(marketCode, marketCode);
            var trim = string.IsNullOrWhiteSpace(configuration.TrimName) ? bodyCode : configuration.TrimName;

            var en = $"{_catalog.MakeName} {model.Name} {generation.Code} {trim} {bodyCode} {engineCode} {marketCode}";
            var ar = $"{_catalog.MakeArabicAlias} {modelArabic} {generation.Code} {trim} {bodyCode} {engineCode} {marketArabic}";

            await AddAliasIfMissingAsync("configuration", configuration.Id, "en", en, existingNormalized, result, cancellationToken);
            await AddAliasIfMissingAsync("configuration", configuration.Id, "ar", ar, existingNormalized, result, cancellationToken);
        }
    }

    private async Task AddAliasIfMissingAsync(
        string nodeType,
        int nodeId,
        string locale,
        string aliasText,
        HashSet<(string NodeType, string NormalizedAlias)> existingNormalized,
        VehicleSeedLoadResult result,
        CancellationToken cancellationToken)
    {
        var normalizedAlias = aliasText.Trim().ToLowerInvariant();
        if (existingNormalized.Contains((nodeType, normalizedAlias)))
            return;

        await _repository.CreateAliasAsync(new VehicleAlias
        {
            NodeType = nodeType,
            NodeId = nodeId,
            Locale = locale,
            AliasText = aliasText,
            NormalizedAlias = normalizedAlias
        }, cancellationToken);
        result.AliasesInserted++;
        existingNormalized.Add((nodeType, normalizedAlias));
    }

    private string BuildFingerprint(
        VehicleReferenceModelDocument model,
        VehicleReferenceGenerationDocument generation,
        VehicleReferenceTrimDocument trim,
        string marketCode)
    {
        return string.Join('-',
            _catalog.MakeCode,
            model.Code,
            generation.Code,
            trim.BodyCode,
            trim.EngineCode,
            marketCode,
            Slug(trim.TrimName));
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
