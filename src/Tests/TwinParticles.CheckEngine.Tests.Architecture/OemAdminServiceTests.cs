using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Oem.Admin;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class OemAdminServiceTests
{
    [Test]
    public async Task CreateOemNumberAsync_Should_Normalize_Display_Number_Before_Save()
    {
        var repository = new FakeRepository();
        var normalization = new FakeNormalizationService();
        var service = new OemAdminService(repository, normalization);

        var entity = new OemNumber
        {
            ManufacturerId = 10,
            DisplayNumber = "11-51-7-586-925",
            IsActive = true
        };

        await service.CreateOemNumberAsync(entity, CancellationToken.None);

        repository.LastCreatedOemNumber.Should().NotBeNull();
        repository.LastCreatedOemNumber!.NormalizedNumber.Should().Be("NORM:11-51-7-586-925");
    }

    [Test]
    public void CreateRelationAsync_Should_Reject_Self_Reference()
    {
        var repository = new FakeRepository();
        var service = new OemAdminService(repository, new FakeNormalizationService());

        Func<Task> act = async () => await service.CreateRelationAsync(new OemRelation
        {
            FromOemNumberId = 100,
            ToOemNumberId = 100,
            RelationType = OemRelationType.Equivalent,
            IsActive = true
        }, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentException>();
    }

    private sealed class FakeNormalizationService : IOemNormalizationService
    {
        public string Normalize(string rawNumber)
        {
            return $"NORM:{rawNumber}";
        }
    }

    private sealed class FakeRepository : IOemAdminRepository
    {
        public OemNumber? LastCreatedOemNumber { get; private set; }

        public Task<IReadOnlyList<Manufacturer>> GetManufacturersAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Manufacturer>>([]);
        public Task<Manufacturer?> GetManufacturerByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<Manufacturer?>(null);
        public Task CreateManufacturerAsync(Manufacturer entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateManufacturerAsync(Manufacturer entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteManufacturerAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<OemNumber>> GetOemNumbersAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OemNumber>>([]);
        public Task<OemNumber?> GetOemNumberByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<OemNumber?>(null);

        public Task CreateOemNumberAsync(OemNumber entity, CancellationToken cancellationToken)
        {
            LastCreatedOemNumber = entity;
            return Task.CompletedTask;
        }

        public Task UpdateOemNumberAsync(OemNumber entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteOemNumberAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<OemBulkUpsertResult> BulkUpsertOemNumbersAsync(IReadOnlyList<OemNumber> numbers, CancellationToken cancellationToken) => Task.FromResult(OemBulkUpsertResult.Empty);
        public Task<IReadOnlyList<OemRelation>> GetRelationsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OemRelation>>([]);
        public Task<OemRelation?> GetRelationByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<OemRelation?>(null);
        public Task CreateRelationAsync(OemRelation entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateRelationAsync(OemRelation entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteRelationAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
