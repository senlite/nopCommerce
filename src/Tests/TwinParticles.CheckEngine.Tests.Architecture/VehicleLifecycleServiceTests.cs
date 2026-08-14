using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Vehicle.Admin;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VehicleLifecycleServiceTests
{
    [Test]
    public async Task Archive_Model_Should_Soft_Deactivate_And_Audit_Without_Removing_Descendants()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.ArchiveModelAsync(11, "customer:7", CancellationToken.None);

        result.Success.Should().BeTrue();
        fixture.Repository.Models.Single(model => model.Id == 11).IsActive.Should().BeFalse();
        fixture.Repository.Generations.Should().Contain(generation => generation.Id == 21 && generation.ModelId == 11);
        fixture.Repository.Configurations.Should().Contain(configuration => configuration.Id == 30 && configuration.GenerationId == 21);
        fixture.Audit.Events.Should().ContainSingle(evt =>
            evt.Action == "vehicle.model.archive" &&
            evt.Actor == "customer:7" &&
            evt.EntityId == "11");
        fixture.Cache.InvalidatedLocales.Should().Contain("en");
    }

    [Test]
    public async Task Merge_Model_Should_Reparent_Generations_While_Preserving_Configuration_Ids()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.MergeModelAsync(11, 10, "customer:8", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.MovedChildren.Should().Be(1);
        result.MovedAliases.Should().Be(1);
        fixture.Repository.Models.Single(model => model.Id == 11).IsActive.Should().BeFalse();
        fixture.Repository.Generations.Single(generation => generation.Id == 21).ModelId.Should().Be(10);
        fixture.Repository.Configurations.Single(configuration => configuration.Id == 30)
            .GenerationId.Should().Be(21,
                "configuration ids and generation references remain stable, preserving claims/garage/SEO history");
        fixture.Repository.Aliases.Single(alias => alias.AliasText == "Duplicate 3").NodeId.Should().Be(10);
        fixture.Audit.Events.Should().ContainSingle(evt => evt.Action == "vehicle.model.merge");
    }

    [Test]
    public async Task Merge_Model_Should_Reject_Cross_Make_And_Conflicting_Generation_Codes()
    {
        var fixture = CreateFixture();

        var crossMake = await fixture.Service.MergeModelAsync(12, 10, "customer:9", CancellationToken.None);
        crossMake.Success.Should().BeFalse();
        crossMake.ErrorCode.Should().Be("vehicle.model.merge.cross_make");

        fixture.Repository.ModelMergeConflict = true;
        var conflict = await fixture.Service.MergeModelAsync(11, 10, "customer:9", CancellationToken.None);
        conflict.Success.Should().BeFalse();
        conflict.ErrorCode.Should().Be("vehicle.model.merge.generation_code_conflict");
        fixture.Repository.Generations.Single(generation => generation.Id == 21).ModelId.Should().Be(11);
        fixture.Audit.Events.Should().BeEmpty("failed merges must not be represented as completed audit events");
    }

    [Test]
    public async Task Merge_Generation_Should_Reassign_Children_And_Fitment_Claims_To_Survivor()
    {
        var fixture = CreateFixture();

        // Configuration 31 carries fitment claims via its stable id; reassigning it to generation 20
        // reassigns those claims to the survivor without touching claim foreign keys (AC-012.1 / FR-112).
        var result = await fixture.Service.MergeGenerationAsync(22, 20, "customer:11", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.MovedChildren.Should().Be(2, "one body and one configuration reparent to the survivor");
        result.MovedAliases.Should().Be(1);
        fixture.Repository.Generations.Single(generation => generation.Id == 22).IsActive.Should().BeFalse();
        fixture.Repository.Bodies.Single(body => body.Id == 50).GenerationId.Should().Be(20);
        fixture.Repository.Configurations.Single(configuration => configuration.Id == 31)
            .GenerationId.Should().Be(20, "configuration ids stay stable so fitment claims follow to the survivor generation");
        fixture.Repository.Aliases.Single(alias => alias.AliasText == "Duplicate F30").NodeId.Should().Be(20);
        fixture.Audit.Events.Should().ContainSingle(evt =>
            evt.Action == "vehicle.generation.merge" && evt.Actor == "customer:11");
        fixture.Cache.InvalidatedLocales.Should().Contain("en");
    }

    [Test]
    public async Task Merge_Generation_Should_Reject_Cross_Model_And_Body_Code_Conflicts()
    {
        var fixture = CreateFixture();

        var crossModel = await fixture.Service.MergeGenerationAsync(21, 20, "customer:12", CancellationToken.None);
        crossModel.Success.Should().BeFalse();
        crossModel.ErrorCode.Should().Be("vehicle.generation.merge.cross_model");

        fixture.Repository.GenerationMergeConflict = true;
        var conflict = await fixture.Service.MergeGenerationAsync(22, 20, "customer:12", CancellationToken.None);
        conflict.Success.Should().BeFalse();
        conflict.ErrorCode.Should().Be("vehicle.generation.merge.body_code_conflict");
        fixture.Repository.Configurations.Single(configuration => configuration.Id == 31).GenerationId.Should().Be(22);
        fixture.Audit.Events.Should().BeEmpty("failed merges must not be represented as completed audit events");
    }

    [Test]
    public void Hard_Delete_Generation_With_Descendants_Should_Require_Archive()
    {
        var fixture = CreateFixture();

        var deleteGeneration = async () => await fixture.Service.DeleteGenerationAsync(22, CancellationToken.None);

        deleteGeneration.Should().ThrowAsync<System.InvalidOperationException>()
            .WithMessage("vehicle.generation.archive_required");
    }

    [Test]
    public async Task Merge_Make_Should_Reparent_Models_Move_Aliases_And_Archive_Source()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.MergeMakeAsync(2, 1, "customer:10", CancellationToken.None);

        result.Success.Should().BeTrue();
        fixture.Repository.Makes.Single(make => make.Id == 2).IsActive.Should().BeFalse();
        fixture.Repository.Models.Single(model => model.Id == 12).MakeId.Should().Be(1);
        fixture.Repository.Aliases.Single(alias => alias.AliasText == "Duplicate make").NodeId.Should().Be(1);
        fixture.Audit.Events.Should().ContainSingle(evt => evt.Action == "vehicle.make.merge");
    }

    [Test]
    public void Hard_Delete_With_Descendants_Should_Require_Archive()
    {
        var fixture = CreateFixture();

        var deleteMake = async () => await fixture.Service.DeleteMakeAsync(1, CancellationToken.None);
        var deleteModel = async () => await fixture.Service.DeleteModelAsync(11, CancellationToken.None);

        deleteMake.Should().ThrowAsync<System.InvalidOperationException>()
            .WithMessage("vehicle.make.archive_required");
        deleteModel.Should().ThrowAsync<System.InvalidOperationException>()
            .WithMessage("vehicle.model.archive_required");
    }

    private static Fixture CreateFixture()
    {
        var repository = new StatefulRepository();
        var audit = new RecordingAudit();
        var cache = new RecordingAliasCache();
        return new Fixture(
            new VehicleAdminService(repository, new EmptySeedLoader(), audit, cache),
            repository,
            audit,
            cache);
    }

    private sealed record Fixture(
        VehicleAdminService Service,
        StatefulRepository Repository,
        RecordingAudit Audit,
        RecordingAliasCache Cache);

    private sealed class StatefulRepository : IVehicleAdminRepository
    {
        public List<VehicleMake> Makes { get; } =
        [
            new() { Id = 1, Code = "BMW", Name = "BMW", IsActive = true },
            new() { Id = 2, Code = "BMW-DUP", Name = "Duplicate BMW", IsActive = true }
        ];

        public List<VehicleModel> Models { get; } =
        [
            new() { Id = 10, MakeId = 1, Code = "3ER", Name = "3 Series", IsActive = true },
            new() { Id = 11, MakeId = 1, Code = "3DUP", Name = "Duplicate 3", IsActive = true },
            new() { Id = 12, MakeId = 2, Code = "SRC", Name = "Source model", IsActive = true }
        ];

        public List<VehicleGeneration> Generations { get; } =
        [
            new() { Id = 20, ModelId = 10, Code = "F30", Name = "F30", StartYear = 2012, EndYear = 2019, IsActive = true },
            new() { Id = 21, ModelId = 11, Code = "E90", Name = "E90", StartYear = 2005, EndYear = 2013, IsActive = true },
            new() { Id = 22, ModelId = 10, Code = "F30DUP", Name = "F30 duplicate", StartYear = 2012, EndYear = 2019, IsActive = true }
        ];

        public List<VehicleBody> Bodies { get; } =
        [
            new() { Id = 50, GenerationId = 22, Code = "SEDAN", Name = "Sedan", Doors = 4, IsActive = true }
        ];

        public List<VehicleConfiguration> Configurations { get; } =
        [
            new() { Id = 30, GenerationId = 21, TrimName = "320i", Fingerprint = "preserved-30", IsActive = true },
            new() { Id = 31, GenerationId = 22, BodyId = 50, TrimName = "330i", Fingerprint = "preserved-31", IsActive = true }
        ];

        public List<VehicleAlias> Aliases { get; } =
        [
            new() { Id = 40, NodeType = "model", NodeId = 11, Locale = "en", AliasText = "Duplicate 3", NormalizedAlias = "duplicate 3" },
            new() { Id = 41, NodeType = "make", NodeId = 2, Locale = "en", AliasText = "Duplicate make", NormalizedAlias = "duplicate make" },
            new() { Id = 42, NodeType = "generation", NodeId = 22, Locale = "en", AliasText = "Duplicate F30", NormalizedAlias = "duplicate f30" }
        ];

        public bool ModelMergeConflict { get; set; }

        public bool GenerationMergeConflict { get; set; }

        public Task<IReadOnlyList<VehicleMake>> GetMakesAsync(CancellationToken cancellationToken) => Result(Makes);
        public Task<VehicleMake?> GetMakeByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Makes.FirstOrDefault(item => item.Id == id));
        public Task CreateMakeAsync(VehicleMake entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateMakeAsync(VehicleMake entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteMakeAsync(int id, CancellationToken cancellationToken) { Makes.RemoveAll(item => item.Id == id); return Task.CompletedTask; }

        public Task<VehicleMergeRepositoryResult> MergeMakeAsync(int sourceMakeId, int targetMakeId, CancellationToken cancellationToken)
        {
            var children = Models.Where(model => model.MakeId == sourceMakeId).ToList();
            var conflicts = children.Any(source => Models.Any(target => target.MakeId == targetMakeId && target.Code == source.Code));
            if (conflicts)
                return Task.FromResult(VehicleMergeRepositoryResult.Fail("vehicle.make.merge.model_code_conflict"));
            children.ForEach(model => model.MakeId = targetMakeId);
            var aliases = Aliases.Where(alias => alias.NodeType == "make" && alias.NodeId == sourceMakeId).ToList();
            aliases.ForEach(alias => alias.NodeId = targetMakeId);
            Makes.Single(make => make.Id == sourceMakeId).IsActive = false;
            return Task.FromResult(VehicleMergeRepositoryResult.Ok(children.Count, aliases.Count));
        }

        public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(CancellationToken cancellationToken) => Result(Models);
        public Task<VehicleModel?> GetModelByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Models.FirstOrDefault(item => item.Id == id));
        public Task CreateModelAsync(VehicleModel entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateModelAsync(VehicleModel entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteModelAsync(int id, CancellationToken cancellationToken) { Models.RemoveAll(item => item.Id == id); return Task.CompletedTask; }

        public Task<VehicleMergeRepositoryResult> MergeModelAsync(int sourceModelId, int targetModelId, CancellationToken cancellationToken)
        {
            if (ModelMergeConflict)
                return Task.FromResult(VehicleMergeRepositoryResult.Fail("vehicle.model.merge.generation_code_conflict"));
            var children = Generations.Where(generation => generation.ModelId == sourceModelId).ToList();
            children.ForEach(generation => generation.ModelId = targetModelId);
            var aliases = Aliases.Where(alias => alias.NodeType == "model" && alias.NodeId == sourceModelId).ToList();
            aliases.ForEach(alias => alias.NodeId = targetModelId);
            Models.Single(model => model.Id == sourceModelId).IsActive = false;
            return Task.FromResult(VehicleMergeRepositoryResult.Ok(children.Count, aliases.Count));
        }

        public Task<IReadOnlyList<VehicleGeneration>> GetGenerationsAsync(CancellationToken cancellationToken) => Result(Generations);
        public Task<VehicleGeneration?> GetGenerationByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Generations.FirstOrDefault(item => item.Id == id));
        public Task CreateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteGenerationAsync(int id, CancellationToken cancellationToken) { Generations.RemoveAll(item => item.Id == id); return Task.CompletedTask; }

        public Task<VehicleMergeRepositoryResult> MergeGenerationAsync(int sourceGenerationId, int targetGenerationId, CancellationToken cancellationToken)
        {
            if (GenerationMergeConflict)
                return Task.FromResult(VehicleMergeRepositoryResult.Fail("vehicle.generation.merge.body_code_conflict"));
            var bodies = Bodies.Where(body => body.GenerationId == sourceGenerationId).ToList();
            bodies.ForEach(body => body.GenerationId = targetGenerationId);
            var configurations = Configurations.Where(configuration => configuration.GenerationId == sourceGenerationId).ToList();
            configurations.ForEach(configuration => configuration.GenerationId = targetGenerationId);
            var aliases = Aliases.Where(alias => alias.NodeType == "generation" && alias.NodeId == sourceGenerationId).ToList();
            aliases.ForEach(alias => alias.NodeId = targetGenerationId);
            Generations.Single(generation => generation.Id == sourceGenerationId).IsActive = false;
            return Task.FromResult(VehicleMergeRepositoryResult.Ok(bodies.Count + configurations.Count, aliases.Count));
        }

        public Task<IReadOnlyList<VehicleBody>> GetBodiesAsync(CancellationToken cancellationToken) => Result(Bodies);
        public Task<VehicleBody?> GetBodyByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<VehicleBody?>(null);
        public Task CreateBodyAsync(VehicleBody entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateBodyAsync(VehicleBody entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteBodyAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<VehicleEngine>> GetEnginesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VehicleEngine>>([]);
        public Task<VehicleEngine?> GetEngineByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<VehicleEngine?>(null);
        public Task CreateEngineAsync(VehicleEngine entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateEngineAsync(VehicleEngine entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteEngineAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<VehicleMarket>> GetMarketsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VehicleMarket>>([]);
        public Task<VehicleMarket?> GetMarketByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<VehicleMarket?>(null);
        public Task CreateMarketAsync(VehicleMarket entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateMarketAsync(VehicleMarket entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteMarketAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<VehicleConfiguration>> GetConfigurationsAsync(CancellationToken cancellationToken) => Result(Configurations);
        public Task<VehicleConfiguration?> GetConfigurationByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Configurations.FirstOrDefault(item => item.Id == id));
        public Task CreateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteConfigurationAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<VehicleAlias>> GetAliasesAsync(CancellationToken cancellationToken) => Result(Aliases);
        public Task<VehicleAlias?> GetAliasByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Aliases.FirstOrDefault(item => item.Id == id));
        public Task CreateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAliasAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;

        private static Task<IReadOnlyList<T>> Result<T>(List<T> values)
            => Task.FromResult<IReadOnlyList<T>>(values);
    }

    private sealed class EmptySeedLoader : IVehicleSeedLoader
    {
        public Task<VehicleSeedLoadResult> SeedAsync(CancellationToken cancellationToken)
            => Task.FromResult(new VehicleSeedLoadResult());
    }

    private sealed class RecordingAudit : ICheckEngineAuditService
    {
        public List<(string Actor, string Action, string EntityType, string EntityId)> Events { get; } = [];

        public Task AppendAsync(
            string actor,
            string action,
            string entityType,
            string entityId,
            string? beforeJson,
            string? afterJson,
            CancellationToken cancellationToken = default)
        {
            Events.Add((actor, action, entityType, entityId));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAliasCache : IVehicleAliasCache
    {
        public List<string> InvalidatedLocales { get; } = [];

        public Task<IReadOnlyList<VehicleAliasSearchItem>?> GetAsync(string term, string locale, int take, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<VehicleAliasSearchItem>?>(null);

        public Task SetAsync(string term, string locale, int take, IReadOnlyList<VehicleAliasSearchItem> items, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task InvalidateAsync(string locale, CancellationToken cancellationToken)
        {
            InvalidatedLocales.Add(locale);
            return Task.CompletedTask;
        }
    }
}
