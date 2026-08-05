using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Application.Vehicle.Admin;

public sealed class VehicleAdminService
{
    private readonly IVehicleAdminRepository _repository;
    private readonly IVehicleSeedLoader _seedLoader;

    public VehicleAdminService(IVehicleAdminRepository repository, IVehicleSeedLoader seedLoader)
    {
        _repository = repository;
        _seedLoader = seedLoader;
    }

    public Task<IReadOnlyList<VehicleMake>> GetMakesAsync(CancellationToken cancellationToken) => _repository.GetMakesAsync(cancellationToken);
    public Task<VehicleMake?> GetMakeByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetMakeByIdAsync(id, cancellationToken);

    public Task CreateMakeAsync(VehicleMake entity, CancellationToken cancellationToken)
    {
        EnsureMakeInvariant(entity);
        return _repository.CreateMakeAsync(entity, cancellationToken);
    }

    public Task UpdateMakeAsync(VehicleMake entity, CancellationToken cancellationToken)
    {
        EnsureMakeInvariant(entity);
        return _repository.UpdateMakeAsync(entity, cancellationToken);
    }

    public Task DeleteMakeAsync(int id, CancellationToken cancellationToken) => _repository.DeleteMakeAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(CancellationToken cancellationToken) => _repository.GetModelsAsync(cancellationToken);
    public Task<VehicleModel?> GetModelByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetModelByIdAsync(id, cancellationToken);

    public Task CreateModelAsync(VehicleModel entity, CancellationToken cancellationToken)
    {
        EnsureModelInvariant(entity);
        return _repository.CreateModelAsync(entity, cancellationToken);
    }

    public Task UpdateModelAsync(VehicleModel entity, CancellationToken cancellationToken)
    {
        EnsureModelInvariant(entity);
        return _repository.UpdateModelAsync(entity, cancellationToken);
    }

    public Task DeleteModelAsync(int id, CancellationToken cancellationToken) => _repository.DeleteModelAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleGeneration>> GetGenerationsAsync(CancellationToken cancellationToken) => _repository.GetGenerationsAsync(cancellationToken);
    public Task<VehicleGeneration?> GetGenerationByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetGenerationByIdAsync(id, cancellationToken);

    public Task CreateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken)
    {
        EnsureGenerationInvariant(entity);
        return _repository.CreateGenerationAsync(entity, cancellationToken);
    }

    public Task UpdateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken)
    {
        EnsureGenerationInvariant(entity);
        return _repository.UpdateGenerationAsync(entity, cancellationToken);
    }

    public Task DeleteGenerationAsync(int id, CancellationToken cancellationToken) => _repository.DeleteGenerationAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleBody>> GetBodiesAsync(CancellationToken cancellationToken) => _repository.GetBodiesAsync(cancellationToken);
    public Task<VehicleBody?> GetBodyByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetBodyByIdAsync(id, cancellationToken);

    public Task CreateBodyAsync(VehicleBody entity, CancellationToken cancellationToken)
    {
        EnsureBodyInvariant(entity);
        return _repository.CreateBodyAsync(entity, cancellationToken);
    }

    public Task UpdateBodyAsync(VehicleBody entity, CancellationToken cancellationToken)
    {
        EnsureBodyInvariant(entity);
        return _repository.UpdateBodyAsync(entity, cancellationToken);
    }

    public Task DeleteBodyAsync(int id, CancellationToken cancellationToken) => _repository.DeleteBodyAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleEngine>> GetEnginesAsync(CancellationToken cancellationToken) => _repository.GetEnginesAsync(cancellationToken);
    public Task<VehicleEngine?> GetEngineByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetEngineByIdAsync(id, cancellationToken);

    public Task CreateEngineAsync(VehicleEngine entity, CancellationToken cancellationToken)
    {
        EnsureEngineInvariant(entity);
        return _repository.CreateEngineAsync(entity, cancellationToken);
    }

    public Task UpdateEngineAsync(VehicleEngine entity, CancellationToken cancellationToken)
    {
        EnsureEngineInvariant(entity);
        return _repository.UpdateEngineAsync(entity, cancellationToken);
    }

    public Task DeleteEngineAsync(int id, CancellationToken cancellationToken) => _repository.DeleteEngineAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleMarket>> GetMarketsAsync(CancellationToken cancellationToken) => _repository.GetMarketsAsync(cancellationToken);
    public Task<VehicleMarket?> GetMarketByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetMarketByIdAsync(id, cancellationToken);

    public Task CreateMarketAsync(VehicleMarket entity, CancellationToken cancellationToken)
    {
        EnsureMarketInvariant(entity);
        return _repository.CreateMarketAsync(entity, cancellationToken);
    }

    public Task UpdateMarketAsync(VehicleMarket entity, CancellationToken cancellationToken)
    {
        EnsureMarketInvariant(entity);
        return _repository.UpdateMarketAsync(entity, cancellationToken);
    }

    public Task DeleteMarketAsync(int id, CancellationToken cancellationToken) => _repository.DeleteMarketAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleConfiguration>> GetConfigurationsAsync(CancellationToken cancellationToken) => _repository.GetConfigurationsAsync(cancellationToken);
    public Task<VehicleConfiguration?> GetConfigurationByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetConfigurationByIdAsync(id, cancellationToken);

    public Task CreateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken)
    {
        EnsureConfigurationInvariant(entity);
        return _repository.CreateConfigurationAsync(entity, cancellationToken);
    }

    public Task UpdateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken)
    {
        EnsureConfigurationInvariant(entity);
        return _repository.UpdateConfigurationAsync(entity, cancellationToken);
    }

    public Task DeleteConfigurationAsync(int id, CancellationToken cancellationToken) => _repository.DeleteConfigurationAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleAlias>> GetAliasesAsync(CancellationToken cancellationToken) => _repository.GetAliasesAsync(cancellationToken);
    public Task<VehicleAlias?> GetAliasByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetAliasByIdAsync(id, cancellationToken);

    public Task CreateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken)
    {
        EnsureAliasInvariant(entity);
        return _repository.CreateAliasAsync(entity, cancellationToken);
    }

    public Task UpdateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken)
    {
        EnsureAliasInvariant(entity);
        return _repository.UpdateAliasAsync(entity, cancellationToken);
    }

    public Task DeleteAliasAsync(int id, CancellationToken cancellationToken) => _repository.DeleteAliasAsync(id, cancellationToken);

    public Task<VehicleSeedLoadResult> SeedAsync(CancellationToken cancellationToken) => _seedLoader.SeedAsync(cancellationToken);

    private static void EnsureMakeInvariant(VehicleMake entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("vehicle.make.code_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("vehicle.make.name_required", nameof(entity));
    }

    private static void EnsureModelInvariant(VehicleModel entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (entity.MakeId <= 0)
            throw new ArgumentException("vehicle.model.make_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("vehicle.model.code_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("vehicle.model.name_required", nameof(entity));
    }

    private static void EnsureGenerationInvariant(VehicleGeneration entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (entity.ModelId <= 0)
            throw new ArgumentException("vehicle.generation.model_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("vehicle.generation.code_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("vehicle.generation.name_required", nameof(entity));
        if (entity.StartYear <= 0)
            throw new ArgumentException("vehicle.generation.start_year_required", nameof(entity));
        if (entity.EndYear.HasValue && entity.EndYear.Value < entity.StartYear)
            throw new ArgumentException("vehicle.generation.invalid_year_range", nameof(entity));
    }

    private static void EnsureBodyInvariant(VehicleBody entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (entity.GenerationId <= 0)
            throw new ArgumentException("vehicle.body.generation_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("vehicle.body.code_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("vehicle.body.name_required", nameof(entity));
    }

    private static void EnsureEngineInvariant(VehicleEngine entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (entity.BodyId <= 0)
            throw new ArgumentException("vehicle.engine.body_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("vehicle.engine.code_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("vehicle.engine.name_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.FuelType))
            throw new ArgumentException("vehicle.engine.fuel_required", nameof(entity));
        if (entity.DisplacementCc <= 0)
            throw new ArgumentException("vehicle.engine.displacement_required", nameof(entity));
        if (entity.PowerHp <= 0)
            throw new ArgumentException("vehicle.engine.power_required", nameof(entity));
    }

    private static void EnsureMarketInvariant(VehicleMarket entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("vehicle.market.code_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("vehicle.market.name_required", nameof(entity));
    }

    private static void EnsureConfigurationInvariant(VehicleConfiguration entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (entity.GenerationId <= 0)
            throw new ArgumentException("vehicle.configuration.generation_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.TrimName))
            throw new ArgumentException("vehicle.configuration.trim_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Fingerprint))
            throw new ArgumentException("vehicle.configuration.fingerprint_required", nameof(entity));
        if (entity.ProductionFromYear.HasValue && entity.ProductionFromYear.Value <= 0)
            throw new ArgumentException("vehicle.configuration.production_from_invalid", nameof(entity));
        if (entity.ProductionToYear.HasValue && entity.ProductionToYear.Value <= 0)
            throw new ArgumentException("vehicle.configuration.production_to_invalid", nameof(entity));
        if (entity.ProductionFromYear.HasValue && entity.ProductionToYear.HasValue && entity.ProductionToYear.Value < entity.ProductionFromYear.Value)
            throw new ArgumentException("vehicle.configuration.invalid_year_range", nameof(entity));
    }

    private static void EnsureAliasInvariant(VehicleAlias entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.NodeType))
            throw new ArgumentException("vehicle.alias.node_type_required", nameof(entity));
        if (entity.NodeId <= 0)
            throw new ArgumentException("vehicle.alias.node_id_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Locale))
            throw new ArgumentException("vehicle.alias.locale_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.AliasText))
            throw new ArgumentException("vehicle.alias.text_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.NormalizedAlias))
            throw new ArgumentException("vehicle.alias.normalized_required", nameof(entity));
    }
}
