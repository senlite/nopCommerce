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
    public Task CreateMakeAsync(VehicleMake entity, CancellationToken cancellationToken) => _repository.CreateMakeAsync(entity, cancellationToken);
    public Task UpdateMakeAsync(VehicleMake entity, CancellationToken cancellationToken) => _repository.UpdateMakeAsync(entity, cancellationToken);
    public Task DeleteMakeAsync(int id, CancellationToken cancellationToken) => _repository.DeleteMakeAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(CancellationToken cancellationToken) => _repository.GetModelsAsync(cancellationToken);
    public Task<VehicleModel?> GetModelByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetModelByIdAsync(id, cancellationToken);
    public Task CreateModelAsync(VehicleModel entity, CancellationToken cancellationToken) => _repository.CreateModelAsync(entity, cancellationToken);
    public Task UpdateModelAsync(VehicleModel entity, CancellationToken cancellationToken) => _repository.UpdateModelAsync(entity, cancellationToken);
    public Task DeleteModelAsync(int id, CancellationToken cancellationToken) => _repository.DeleteModelAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleGeneration>> GetGenerationsAsync(CancellationToken cancellationToken) => _repository.GetGenerationsAsync(cancellationToken);
    public Task<VehicleGeneration?> GetGenerationByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetGenerationByIdAsync(id, cancellationToken);
    public Task CreateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken) => _repository.CreateGenerationAsync(entity, cancellationToken);
    public Task UpdateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken) => _repository.UpdateGenerationAsync(entity, cancellationToken);
    public Task DeleteGenerationAsync(int id, CancellationToken cancellationToken) => _repository.DeleteGenerationAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleBody>> GetBodiesAsync(CancellationToken cancellationToken) => _repository.GetBodiesAsync(cancellationToken);
    public Task<VehicleBody?> GetBodyByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetBodyByIdAsync(id, cancellationToken);
    public Task CreateBodyAsync(VehicleBody entity, CancellationToken cancellationToken) => _repository.CreateBodyAsync(entity, cancellationToken);
    public Task UpdateBodyAsync(VehicleBody entity, CancellationToken cancellationToken) => _repository.UpdateBodyAsync(entity, cancellationToken);
    public Task DeleteBodyAsync(int id, CancellationToken cancellationToken) => _repository.DeleteBodyAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleEngine>> GetEnginesAsync(CancellationToken cancellationToken) => _repository.GetEnginesAsync(cancellationToken);
    public Task<VehicleEngine?> GetEngineByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetEngineByIdAsync(id, cancellationToken);
    public Task CreateEngineAsync(VehicleEngine entity, CancellationToken cancellationToken) => _repository.CreateEngineAsync(entity, cancellationToken);
    public Task UpdateEngineAsync(VehicleEngine entity, CancellationToken cancellationToken) => _repository.UpdateEngineAsync(entity, cancellationToken);
    public Task DeleteEngineAsync(int id, CancellationToken cancellationToken) => _repository.DeleteEngineAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleMarket>> GetMarketsAsync(CancellationToken cancellationToken) => _repository.GetMarketsAsync(cancellationToken);
    public Task<VehicleMarket?> GetMarketByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetMarketByIdAsync(id, cancellationToken);
    public Task CreateMarketAsync(VehicleMarket entity, CancellationToken cancellationToken) => _repository.CreateMarketAsync(entity, cancellationToken);
    public Task UpdateMarketAsync(VehicleMarket entity, CancellationToken cancellationToken) => _repository.UpdateMarketAsync(entity, cancellationToken);
    public Task DeleteMarketAsync(int id, CancellationToken cancellationToken) => _repository.DeleteMarketAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleConfiguration>> GetConfigurationsAsync(CancellationToken cancellationToken) => _repository.GetConfigurationsAsync(cancellationToken);
    public Task<VehicleConfiguration?> GetConfigurationByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetConfigurationByIdAsync(id, cancellationToken);
    public Task CreateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken) => _repository.CreateConfigurationAsync(entity, cancellationToken);
    public Task UpdateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken) => _repository.UpdateConfigurationAsync(entity, cancellationToken);
    public Task DeleteConfigurationAsync(int id, CancellationToken cancellationToken) => _repository.DeleteConfigurationAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleAlias>> GetAliasesAsync(CancellationToken cancellationToken) => _repository.GetAliasesAsync(cancellationToken);
    public Task<VehicleAlias?> GetAliasByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetAliasByIdAsync(id, cancellationToken);
    public Task CreateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken) => _repository.CreateAliasAsync(entity, cancellationToken);
    public Task UpdateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken) => _repository.UpdateAliasAsync(entity, cancellationToken);
    public Task DeleteAliasAsync(int id, CancellationToken cancellationToken) => _repository.DeleteAliasAsync(id, cancellationToken);

    public Task<VehicleSeedLoadResult> SeedAsync(CancellationToken cancellationToken) => _seedLoader.SeedAsync(cancellationToken);
}
