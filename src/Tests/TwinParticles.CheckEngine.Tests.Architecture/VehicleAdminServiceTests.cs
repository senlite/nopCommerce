using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Vehicle.Admin;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VehicleAdminServiceTests
{
    [Test]
    public void CreateMakeAsync_Should_Reject_Empty_Code()
    {
        var service = CreateService();

        Func<Task> act = async () => await service.CreateMakeAsync(new VehicleMake
        {
            Code = "",
            Name = "BMW",
            IsActive = true
        }, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public void CreateModelAsync_Should_Reject_Missing_MakeId()
    {
        var service = CreateService();

        Func<Task> act = async () => await service.CreateModelAsync(new VehicleModel
        {
            MakeId = 0,
            Code = "F30",
            Name = "3 Series",
            IsActive = true
        }, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public void CreateGenerationAsync_Should_Reject_EndYear_Before_StartYear()
    {
        var service = CreateService();

        Func<Task> act = async () => await service.CreateGenerationAsync(new VehicleGeneration
        {
            ModelId = 10,
            Code = "GEN1",
            Name = "Generation 1",
            StartYear = 2020,
            EndYear = 2019,
            IsActive = true
        }, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public void CreateEngineAsync_Should_Reject_NonPositive_Displacement()
    {
        var service = CreateService();

        Func<Task> act = async () => await service.CreateEngineAsync(new VehicleEngine
        {
            BodyId = 20,
            Code = "I4",
            Name = "Inline 4",
            FuelType = "Petrol",
            DisplacementCc = 0,
            PowerHp = 150,
            IsActive = true
        }, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public void CreateConfigurationAsync_Should_Reject_End_Production_Year_Before_Start()
    {
        var service = CreateService();

        Func<Task> act = async () => await service.CreateConfigurationAsync(new VehicleConfiguration
        {
            GenerationId = 100,
            TrimName = "Base",
            Fingerprint = "BASE-100",
            ProductionFromYear = 2021,
            ProductionToYear = 2020,
            IsActive = true
        }, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public void CreateAliasAsync_Should_Reject_Empty_NormalizedAlias()
    {
        var service = CreateService();

        Func<Task> act = async () => await service.CreateAliasAsync(new VehicleAlias
        {
            NodeType = "make",
            NodeId = 1,
            Locale = "en",
            AliasText = "Bimmer",
            NormalizedAlias = ""
        }, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task CreateMakeAsync_Should_Persist_When_Valid()
    {
        var repository = new FakeVehicleAdminRepository();
        var service = new VehicleAdminService(repository, new FakeVehicleSeedLoader());

        var entity = new VehicleMake
        {
            Code = "BMW",
            Name = "BMW",
            IsActive = true
        };

        await service.CreateMakeAsync(entity, CancellationToken.None);

        repository.LastCreatedMake.Should().NotBeNull();
        repository.LastCreatedMake!.Code.Should().Be("BMW");
    }

    private static VehicleAdminService CreateService()
    {
        return new VehicleAdminService(new FakeVehicleAdminRepository(), new FakeVehicleSeedLoader());
    }

    private sealed class FakeVehicleSeedLoader : IVehicleSeedLoader
    {
        public Task<VehicleSeedLoadResult> SeedAsync(CancellationToken cancellationToken)
            => Task.FromResult(new VehicleSeedLoadResult());
    }

    private sealed class FakeVehicleAdminRepository : IVehicleAdminRepository
    {
        public VehicleMake? LastCreatedMake { get; private set; }

        public Task<IReadOnlyList<VehicleMake>> GetMakesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VehicleMake>>([]);
        public Task<VehicleMake?> GetMakeByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<VehicleMake?>(null);

        public Task CreateMakeAsync(VehicleMake entity, CancellationToken cancellationToken)
        {
            LastCreatedMake = entity;
            return Task.CompletedTask;
        }

        public Task UpdateMakeAsync(VehicleMake entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteMakeAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VehicleMergeRepositoryResult> MergeMakeAsync(int sourceMakeId, int targetMakeId, CancellationToken cancellationToken)
            => Task.FromResult(VehicleMergeRepositoryResult.Ok(0, 0));

        public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VehicleModel>>([]);
        public Task<VehicleModel?> GetModelByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<VehicleModel?>(null);
        public Task CreateModelAsync(VehicleModel entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateModelAsync(VehicleModel entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteModelAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VehicleMergeRepositoryResult> MergeModelAsync(int sourceModelId, int targetModelId, CancellationToken cancellationToken)
            => Task.FromResult(VehicleMergeRepositoryResult.Ok(0, 0));

        public Task<IReadOnlyList<VehicleGeneration>> GetGenerationsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VehicleGeneration>>([]);
        public Task<VehicleGeneration?> GetGenerationByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<VehicleGeneration?>(null);
        public Task CreateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteGenerationAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<VehicleBody>> GetBodiesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VehicleBody>>([]);
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

        public Task<IReadOnlyList<VehicleConfiguration>> GetConfigurationsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VehicleConfiguration>>([]);
        public Task<VehicleConfiguration?> GetConfigurationByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<VehicleConfiguration?>(null);
        public Task CreateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteConfigurationAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<VehicleAlias>> GetAliasesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VehicleAlias>>([]);
        public Task<VehicleAlias?> GetAliasByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<VehicleAlias?>(null);
        public Task CreateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAliasAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
