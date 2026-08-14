using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;

public sealed class BmwVinConfigurationResolver
{
    private readonly IVehicleAdminRepository _vehicleRepository;

    public BmwVinConfigurationResolver(IVehicleAdminRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<IReadOnlyList<VinDecodeCandidate>> ResolveAsync(
        VinPattern pattern,
        int? modelYear,
        CancellationToken cancellationToken)
    {
        var makes = await _vehicleRepository.GetMakesAsync(cancellationToken);
        var bmwMake = makes.FirstOrDefault(make => make.Code == BmwReferenceCatalog.MakeCode && make.IsActive);
        if (bmwMake is null)
            return [];

        var models = (await _vehicleRepository.GetModelsAsync(cancellationToken))
            .Where(model => model.MakeId == bmwMake.Id && model.IsActive && model.Code == pattern.ModelCode)
            .ToList();
        if (models.Count == 0)
            return [];

        var modelIds = models.Select(model => model.Id).ToHashSet();
        var generations = (await _vehicleRepository.GetGenerationsAsync(cancellationToken))
            .Where(generation => modelIds.Contains(generation.ModelId)
                                 && generation.IsActive
                                 && generation.Code == pattern.GenerationCode)
            .ToList();
        if (generations.Count == 0)
            return [];

        var generationIds = generations.Select(generation => generation.Id).ToHashSet();
        var configurations = (await _vehicleRepository.GetConfigurationsAsync(cancellationToken))
            .Where(configuration => generationIds.Contains(configuration.GenerationId) && configuration.IsActive)
            .ToList();

        if (!string.IsNullOrWhiteSpace(pattern.EngineCode))
        {
            var engines = await _vehicleRepository.GetEnginesAsync(cancellationToken);
            var engineIds = engines
                .Where(engine => engine.IsActive && engine.Code == pattern.EngineCode)
                .Select(engine => engine.Id)
                .ToHashSet();
            configurations = configurations.Where(configuration => configuration.EngineId.HasValue && engineIds.Contains(configuration.EngineId.Value)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(pattern.TrimSlug))
        {
            configurations = configurations
                .Where(configuration => Slug(configuration.TrimName) == pattern.TrimSlug)
                .ToList();
        }

        if (modelYear.HasValue)
        {
            configurations = configurations.Where(configuration =>
                    (!configuration.ProductionFromYear.HasValue || configuration.ProductionFromYear.Value <= modelYear.Value)
                    && (!configuration.ProductionToYear.HasValue || configuration.ProductionToYear.Value >= modelYear.Value))
                .ToList();
        }

        return configurations
            .Select(configuration => new VinDecodeCandidate
            {
                VehicleConfigurationId = configuration.Id,
                Confidence = Confidence.Create(pattern.Confidence),
                ModelYear = modelYear
            })
            .ToList();
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
