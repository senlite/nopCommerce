using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Domain.Vehicle.Admin;

public interface IVehicleAdminRepository
{
    Task<IReadOnlyList<VehicleMake>> GetMakesAsync(CancellationToken cancellationToken);
    Task<VehicleMake?> GetMakeByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateMakeAsync(VehicleMake entity, CancellationToken cancellationToken);
    Task UpdateMakeAsync(VehicleMake entity, CancellationToken cancellationToken);
    Task DeleteMakeAsync(int id, CancellationToken cancellationToken);
    Task<VehicleMergeRepositoryResult> MergeMakeAsync(int sourceMakeId, int targetMakeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleModel>> GetModelsAsync(CancellationToken cancellationToken);
    Task<VehicleModel?> GetModelByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateModelAsync(VehicleModel entity, CancellationToken cancellationToken);
    Task UpdateModelAsync(VehicleModel entity, CancellationToken cancellationToken);
    Task DeleteModelAsync(int id, CancellationToken cancellationToken);
    Task<VehicleMergeRepositoryResult> MergeModelAsync(int sourceModelId, int targetModelId, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleGeneration>> GetGenerationsAsync(CancellationToken cancellationToken);
    Task<VehicleGeneration?> GetGenerationByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken);
    Task UpdateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken);
    Task DeleteGenerationAsync(int id, CancellationToken cancellationToken);
    Task<VehicleMergeRepositoryResult> MergeGenerationAsync(int sourceGenerationId, int targetGenerationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleBody>> GetBodiesAsync(CancellationToken cancellationToken);
    Task<VehicleBody?> GetBodyByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateBodyAsync(VehicleBody entity, CancellationToken cancellationToken);
    Task UpdateBodyAsync(VehicleBody entity, CancellationToken cancellationToken);
    Task DeleteBodyAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleEngine>> GetEnginesAsync(CancellationToken cancellationToken);
    Task<VehicleEngine?> GetEngineByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateEngineAsync(VehicleEngine entity, CancellationToken cancellationToken);
    Task UpdateEngineAsync(VehicleEngine entity, CancellationToken cancellationToken);
    Task DeleteEngineAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleMarket>> GetMarketsAsync(CancellationToken cancellationToken);
    Task<VehicleMarket?> GetMarketByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateMarketAsync(VehicleMarket entity, CancellationToken cancellationToken);
    Task UpdateMarketAsync(VehicleMarket entity, CancellationToken cancellationToken);
    Task DeleteMarketAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleConfiguration>> GetConfigurationsAsync(CancellationToken cancellationToken);
    Task<VehicleConfiguration?> GetConfigurationByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken);
    Task UpdateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken);
    Task DeleteConfigurationAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleAlias>> GetAliasesAsync(CancellationToken cancellationToken);
    Task<VehicleAlias?> GetAliasByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken);
    Task UpdateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken);
    Task DeleteAliasAsync(int id, CancellationToken cancellationToken);
}
