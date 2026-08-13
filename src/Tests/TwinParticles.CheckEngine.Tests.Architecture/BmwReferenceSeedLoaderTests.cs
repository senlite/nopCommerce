using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// Runs the BMW reference seed loader against an in-memory repository (H1.4). Verifies the launch
/// dataset is referentially consistent, brand-neutral in shape, bilingual, and idempotent.
/// </summary>
[TestFixture]
public class BmwReferenceSeedLoaderTests
{
    [Test]
    public async Task Seed_Should_Build_A_Consistent_Bmw_Hierarchy()
    {
        var repository = new InMemoryVehicleAdminRepository();
        var loader = new BmwReferenceVehicleSeedLoader(repository);

        var result = await loader.SeedAsync(CancellationToken.None);

        using var scope = new FluentAssertions.Execution.AssertionScope();

        result.MakesInserted.Should().Be(1);
        result.ModelsInserted.Should().Be(BmwReferenceCatalog.Models.Count);
        result.ConfigurationsInserted.Should().BeGreaterThan(0);

        var makes = await repository.GetMakesAsync(CancellationToken.None);
        makes.Should().ContainSingle(m => m.Code == "BMW");

        // Every child references a parent that exists (referential integrity in the absence of FKs here).
        var modelIds = (await repository.GetModelsAsync(CancellationToken.None)).Select(m => m.Id).ToHashSet();
        var makeIds = makes.Select(m => m.Id).ToHashSet();
        (await repository.GetModelsAsync(CancellationToken.None)).Should().OnlyContain(m => makeIds.Contains(m.MakeId));

        var generations = await repository.GetGenerationsAsync(CancellationToken.None);
        generations.Should().OnlyContain(g => modelIds.Contains(g.ModelId));
        var generationIds = generations.Select(g => g.Id).ToHashSet();

        var bodies = await repository.GetBodiesAsync(CancellationToken.None);
        bodies.Should().OnlyContain(b => generationIds.Contains(b.GenerationId));
        var bodyIds = bodies.Select(b => b.Id).ToHashSet();

        var engines = await repository.GetEnginesAsync(CancellationToken.None);
        engines.Should().OnlyContain(e => bodyIds.Contains(e.BodyId));
        var engineIds = engines.Select(e => e.Id).ToHashSet();

        var markets = await repository.GetMarketsAsync(CancellationToken.None);
        var marketIds = markets.Select(m => m.Id).ToHashSet();

        var configurations = await repository.GetConfigurationsAsync(CancellationToken.None);
        configurations.Should().OnlyContain(c =>
            generationIds.Contains(c.GenerationId)
            && c.BodyId.HasValue && bodyIds.Contains(c.BodyId.Value)
            && c.EngineId.HasValue && engineIds.Contains(c.EngineId.Value)
            && c.MarketId.HasValue && marketIds.Contains(c.MarketId.Value));
    }

    [Test]
    public async Task Every_Configuration_Should_Have_A_Unique_Fingerprint_And_Be_Active()
    {
        var repository = new InMemoryVehicleAdminRepository();
        await new BmwReferenceVehicleSeedLoader(repository).SeedAsync(CancellationToken.None);

        var configurations = await repository.GetConfigurationsAsync(CancellationToken.None);

        using var scope = new FluentAssertions.Execution.AssertionScope();
        configurations.Should().NotBeEmpty();
        configurations.Select(c => c.Fingerprint).Should().OnlyHaveUniqueItems("INV-004 requires globally unique fingerprints");
        configurations.Should().OnlyContain(c => c.IsActive, "selectable configurations must be active or hidden");
        configurations.Should().OnlyContain(c => c.ProductionFromYear.HasValue, "years come from production windows, never invented");
    }

    [Test]
    public async Task Every_Model_Should_Carry_English_And_Arabic_Aliases()
    {
        var repository = new InMemoryVehicleAdminRepository();
        await new BmwReferenceVehicleSeedLoader(repository).SeedAsync(CancellationToken.None);

        var aliases = await repository.GetAliasesAsync(CancellationToken.None);
        var models = await repository.GetModelsAsync(CancellationToken.None);

        using var scope = new FluentAssertions.Execution.AssertionScope();
        foreach (var model in models)
        {
            aliases.Should().Contain(a => a.NodeType == "model" && a.NodeId == model.Id && a.Locale == "en");
            aliases.Should().Contain(a => a.NodeType == "model" && a.NodeId == model.Id && a.Locale == "ar");
        }

        aliases.Should().Contain(a => a.NodeType == "make" && a.Locale == "ar",
            "the make must have an Arabic alias for the launch region");
        aliases.Should().Contain(a => a.NodeType == "generation",
            "generation codes must be searchable aliases (AC-014.1)");
        aliases.Should().OnlyContain(a => !string.IsNullOrWhiteSpace(a.NormalizedAlias));
    }

    [Test]
    public async Task Catalog_Should_Not_Introduce_Manufacturer_Specific_Schema()
    {
        // BMW is data. The loader only ever writes the generic vehicle entities; there is no BMW-typed
        // table or column. This guards FR-130 / AC-010.1 at the seam where the data enters the schema.
        var repository = new InMemoryVehicleAdminRepository();
        await new BmwReferenceVehicleSeedLoader(repository).SeedAsync(CancellationToken.None);

        repository.WrittenEntityTypes.Should().OnlyContain(t => t.Namespace == "TwinParticles.CheckEngine.Domain.Vehicle");
        repository.WrittenEntityTypes.Should().NotContain(t => t.Name.Contains("Bmw"));
    }

    [Test]
    public async Task Seeding_Should_Be_Idempotent()
    {
        var repository = new InMemoryVehicleAdminRepository();
        var loader = new BmwReferenceVehicleSeedLoader(repository);

        await loader.SeedAsync(CancellationToken.None);
        var configurationsAfterFirst = (await repository.GetConfigurationsAsync(CancellationToken.None)).Count;

        var secondRun = await loader.SeedAsync(CancellationToken.None);

        secondRun.MakesInserted.Should().Be(0, "an already-seeded store must not be reseeded");
        (await repository.GetConfigurationsAsync(CancellationToken.None)).Count.Should().Be(configurationsAfterFirst);
    }

    private sealed class InMemoryVehicleAdminRepository : IVehicleAdminRepository
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

        public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(CancellationToken cancellationToken) => List(_models);
        public Task<VehicleModel?> GetModelByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(_models.FirstOrDefault(x => x.Id == id));
        public Task CreateModelAsync(VehicleModel entity, CancellationToken cancellationToken) => Add(_models, entity, e => e.Id = ++_sequence);
        public Task UpdateModelAsync(VehicleModel entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteModelAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<VehicleGeneration>> GetGenerationsAsync(CancellationToken cancellationToken) => List(_generations);
        public Task<VehicleGeneration?> GetGenerationByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(_generations.FirstOrDefault(x => x.Id == id));
        public Task CreateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken) => Add(_generations, entity, e => e.Id = ++_sequence);
        public Task UpdateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteGenerationAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;

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
}
