using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Tests.Architecture;

internal sealed class InMemoryVehicleAdminRepository : IVehicleAdminRepository
{
    private readonly List<VehicleMake> _makes = [];
    private readonly List<VehicleModel> _models = [];
    private readonly List<VehicleGeneration> _generations = [];
    private readonly List<VehicleBody> _bodies = [];
    private readonly List<VehicleEngine> _engines = [];
    private readonly List<VehicleMarket> _markets = [];
    private readonly List<VehicleConfiguration> _configurations = [];
    private readonly List<VehicleAlias> _aliases = [];
    private int _sequence;

    public HashSet<System.Type> WrittenEntityTypes { get; } = [];

    public Task<IReadOnlyList<VehicleMake>> GetMakesAsync(CancellationToken cancellationToken) => List(_makes);
    public Task<VehicleMake?> GetMakeByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(_makes.FirstOrDefault(x => x.Id == id));
    public Task CreateMakeAsync(VehicleMake entity, CancellationToken cancellationToken) => Add(_makes, entity, e => e.Id = ++_sequence);
    public Task UpdateMakeAsync(VehicleMake entity, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteMakeAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<VehicleMergeRepositoryResult> MergeMakeAsync(int sourceMakeId, int targetMakeId, CancellationToken cancellationToken)
        => Task.FromResult(VehicleMergeRepositoryResult.Fail("not_supported"));

    public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(CancellationToken cancellationToken) => List(_models);
    public Task<VehicleModel?> GetModelByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(_models.FirstOrDefault(x => x.Id == id));
    public Task CreateModelAsync(VehicleModel entity, CancellationToken cancellationToken) => Add(_models, entity, e => e.Id = ++_sequence);
    public Task UpdateModelAsync(VehicleModel entity, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteModelAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<VehicleMergeRepositoryResult> MergeModelAsync(int sourceModelId, int targetModelId, CancellationToken cancellationToken)
        => Task.FromResult(VehicleMergeRepositoryResult.Fail("not_supported"));

    public Task<IReadOnlyList<VehicleGeneration>> GetGenerationsAsync(CancellationToken cancellationToken) => List(_generations);
    public Task<VehicleGeneration?> GetGenerationByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(_generations.FirstOrDefault(x => x.Id == id));
    public Task CreateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken) => Add(_generations, entity, e => e.Id = ++_sequence);
    public Task UpdateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteGenerationAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<VehicleMergeRepositoryResult> MergeGenerationAsync(int sourceGenerationId, int targetGenerationId, CancellationToken cancellationToken)
        => Task.FromResult(VehicleMergeRepositoryResult.Fail("not_supported"));

    public Task<IReadOnlyList<VehicleBody>> GetBodiesAsync(CancellationToken cancellationToken) => List(_bodies);
    public Task<VehicleBody?> GetBodyByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(_bodies.FirstOrDefault(x => x.Id == id));
    public Task CreateBodyAsync(VehicleBody entity, CancellationToken cancellationToken) => Add(_bodies, entity, e => e.Id = ++_sequence);
    public Task UpdateBodyAsync(VehicleBody entity, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteBodyAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<VehicleEngine>> GetEnginesAsync(CancellationToken cancellationToken) => List(_engines);
    public Task<VehicleEngine?> GetEngineByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(_engines.FirstOrDefault(x => x.Id == id));
    public Task CreateEngineAsync(VehicleEngine entity, CancellationToken cancellationToken) => Add(_engines, entity, e => e.Id = ++_sequence);
    public Task UpdateEngineAsync(VehicleEngine entity, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteEngineAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<VehicleMarket>> GetMarketsAsync(CancellationToken cancellationToken) => List(_markets);
    public Task<VehicleMarket?> GetMarketByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(_markets.FirstOrDefault(x => x.Id == id));
    public Task CreateMarketAsync(VehicleMarket entity, CancellationToken cancellationToken) => Add(_markets, entity, e => e.Id = ++_sequence);
    public Task UpdateMarketAsync(VehicleMarket entity, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteMarketAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<VehicleConfiguration>> GetConfigurationsAsync(CancellationToken cancellationToken) => List(_configurations);
    public Task<VehicleConfiguration?> GetConfigurationByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(_configurations.FirstOrDefault(x => x.Id == id));
    public Task CreateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken) => Add(_configurations, entity, e => e.Id = ++_sequence);
    public Task UpdateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteConfigurationAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<VehicleAlias>> GetAliasesAsync(CancellationToken cancellationToken) => List(_aliases);
    public Task<VehicleAlias?> GetAliasByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(_aliases.FirstOrDefault(x => x.Id == id));
    public Task CreateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken) => Add(_aliases, entity, e => e.Id = ++_sequence);
    public Task UpdateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteAliasAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;

    private Task Add<T>(List<T> store, T entity, System.Action<T> assignId)
    {
        assignId(entity);
        store.Add(entity);
        WrittenEntityTypes.Add(typeof(T));
        return Task.CompletedTask;
    }

    private static Task<IReadOnlyList<T>> List<T>(List<T> store) => Task.FromResult<IReadOnlyList<T>>(store.ToList());
}
