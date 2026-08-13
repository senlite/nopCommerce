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
    public async Task Reference_Catalog_Should_Meet_Expanded_Launch_Scale_And_Priority_Coverage()
    {
        var repository = new InMemoryVehicleAdminRepository();
        await new BmwReferenceVehicleSeedLoader(repository).SeedAsync(CancellationToken.None);

        var expectedGenerations = BmwReferenceCatalog.Models.Sum(model => model.Generations.Count);
        var expectedConfigurations = BmwReferenceCatalog.Models
            .SelectMany(model => model.Generations)
            .Sum(generation => generation.Trims.Count * BmwReferenceCatalog.Markets.Count);

        var models = await repository.GetModelsAsync(CancellationToken.None);
        var generations = await repository.GetGenerationsAsync(CancellationToken.None);
        var configurations = await repository.GetConfigurationsAsync(CancellationToken.None);

        using var scope = new FluentAssertions.Execution.AssertionScope();
        models.Should().HaveCount(BmwReferenceCatalog.Models.Count).And.HaveCountGreaterThanOrEqualTo(10);
        generations.Should().HaveCount(expectedGenerations).And.HaveCountGreaterThanOrEqualTo(30);
        configurations.Should().HaveCount(expectedConfigurations).And.HaveCountGreaterThanOrEqualTo(250);

        var generationCodes = generations.Select(generation => generation.Code).ToHashSet();
        generationCodes.Should().Contain(
            ["E46", "E90", "F30", "G20", "E39", "E60", "F10", "G30", "E70", "F15", "G05"],
            "the commercially important launch-region generation priority slice must stay covered");
    }

    [Test]
    public async Task Every_Configuration_Should_Carry_English_And_Arabic_Search_Aliases()
    {
        var repository = new InMemoryVehicleAdminRepository();
        await new BmwReferenceVehicleSeedLoader(repository).SeedAsync(CancellationToken.None);

        var configurations = await repository.GetConfigurationsAsync(CancellationToken.None);
        var aliases = await repository.GetAliasesAsync(CancellationToken.None);

        using var scope = new FluentAssertions.Execution.AssertionScope();
        foreach (var configuration in configurations)
        {
            aliases.Should().Contain(alias =>
                alias.NodeType == "configuration" &&
                alias.NodeId == configuration.Id &&
                alias.Locale == "en");
            aliases.Should().Contain(alias =>
                alias.NodeType == "configuration" &&
                alias.NodeId == configuration.Id &&
                alias.Locale == "ar");
        }

        aliases.Where(alias => alias.NodeType == "configuration")
            .Should().HaveCount(configurations.Count * 2);
        aliases.Select(alias => (alias.NodeType, alias.NormalizedAlias))
            .Should().OnlyHaveUniqueItems("the SQL alias index is unique by node type and normalized alias");
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
        (secondRun.ModelsInserted + secondRun.GenerationsInserted + secondRun.BodiesInserted +
         secondRun.EnginesInserted + secondRun.MarketsInserted + secondRun.ConfigurationsInserted +
         secondRun.AliasesInserted).Should().Be(0, "every hierarchy level is incrementally idempotent");
        (await repository.GetConfigurationsAsync(CancellationToken.None)).Count.Should().Be(configurationsAfterFirst);
    }

    [Test]
    public async Task Seeding_Should_Not_Be_Blocked_By_An_Unrelated_Existing_Make()
    {
        var repository = new InMemoryVehicleAdminRepository();
        await repository.CreateMakeAsync(
            new VehicleMake { Code = "OTHER", Name = "Operator make", IsActive = true },
            CancellationToken.None);

        var result = await new BmwReferenceVehicleSeedLoader(repository).SeedAsync(CancellationToken.None);

        result.MakesInserted.Should().Be(1);
        (await repository.GetMakesAsync(CancellationToken.None)).Should().Contain(make => make.Code == "OTHER");
        (await repository.GetMakesAsync(CancellationToken.None)).Should().Contain(make => make.Code == "BMW");
        (await repository.GetConfigurationsAsync(CancellationToken.None)).Should().HaveCountGreaterThanOrEqualTo(250);
    }

    [Test]
    public async Task Seeding_Should_Expand_A_Partial_Bmw_Catalog_Without_Overwriting_Operator_Edits()
    {
        var repository = new InMemoryVehicleAdminRepository();
        await repository.CreateMakeAsync(
            new VehicleMake { Code = "BMW", Name = "Operator BMW", IsActive = true },
            CancellationToken.None);
        var make = (await repository.GetMakesAsync(CancellationToken.None)).Single();
        await repository.CreateModelAsync(
            new VehicleModel { MakeId = make.Id, Code = "3ER", Name = "Operator 3 Series", IsActive = true },
            CancellationToken.None);

        var result = await new BmwReferenceVehicleSeedLoader(repository).SeedAsync(CancellationToken.None);

        using var scope = new FluentAssertions.Execution.AssertionScope();
        result.MakesInserted.Should().Be(0);
        result.ModelsInserted.Should().Be(BmwReferenceCatalog.Models.Count - 1);
        (await repository.GetModelsAsync(CancellationToken.None))
            .Single(model => model.Code == "3ER")
            .Name.Should().Be("Operator 3 Series", "seed upgrades never overwrite curated operator data");
        (await repository.GetConfigurationsAsync(CancellationToken.None)).Should().HaveCountGreaterThanOrEqualTo(250);
    }

    [Test]
    public async Task Seeding_Should_Add_Search_Aliases_To_Preserved_Legacy_Configurations()
    {
        var repository = new InMemoryVehicleAdminRepository();
        await repository.CreateMakeAsync(new VehicleMake { Code = "BMW", Name = "BMW", IsActive = true }, CancellationToken.None);
        var make = (await repository.GetMakesAsync(CancellationToken.None)).Single();
        await repository.CreateModelAsync(new VehicleModel { MakeId = make.Id, Code = "LEGACY", Name = "Legacy Model", IsActive = true }, CancellationToken.None);
        var model = (await repository.GetModelsAsync(CancellationToken.None)).Single();
        await repository.CreateGenerationAsync(new VehicleGeneration { ModelId = model.Id, Code = "Z99", Name = "Z99", StartYear = 2000, EndYear = 2001, IsActive = true }, CancellationToken.None);
        var generation = (await repository.GetGenerationsAsync(CancellationToken.None)).Single();
        await repository.CreateBodyAsync(new VehicleBody { GenerationId = generation.Id, Code = "SEDAN", Name = "Sedan", Doors = 4, IsActive = true }, CancellationToken.None);
        var body = (await repository.GetBodiesAsync(CancellationToken.None)).Single();
        await repository.CreateEngineAsync(new VehicleEngine { BodyId = body.Id, Code = "LEGACY20", Name = "Legacy 2.0", FuelType = "Petrol", DisplacementCc = 2000, PowerHp = 150, IsActive = true }, CancellationToken.None);
        var engine = (await repository.GetEnginesAsync(CancellationToken.None)).Single();
        await repository.CreateMarketAsync(new VehicleMarket { Code = "ECE", Name = "Europe", IsActive = true }, CancellationToken.None);
        var market = (await repository.GetMarketsAsync(CancellationToken.None)).Single();
        await repository.CreateConfigurationAsync(new VehicleConfiguration
        {
            GenerationId = generation.Id,
            BodyId = body.Id,
            EngineId = engine.Id,
            MarketId = market.Id,
            TrimName = "Legacy 20i",
            ProductionFromYear = 2000,
            ProductionToYear = 2001,
            Fingerprint = "operator-preserved-legacy-configuration",
            IsActive = true
        }, CancellationToken.None);
        var legacy = (await repository.GetConfigurationsAsync(CancellationToken.None)).Single();

        await new BmwReferenceVehicleSeedLoader(repository).SeedAsync(CancellationToken.None);

        var aliases = await repository.GetAliasesAsync(CancellationToken.None);
        aliases.Should().Contain(alias =>
            alias.NodeType == "configuration" &&
            alias.NodeId == legacy.Id &&
            alias.Locale == "en" &&
            alias.AliasText.Contains("Z99") &&
            alias.AliasText.Contains("LEGACY20"));
        aliases.Should().Contain(alias =>
            alias.NodeType == "configuration" &&
            alias.NodeId == legacy.Id &&
            alias.Locale == "ar");
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
