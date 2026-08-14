using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Oem.Admin;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class OemSupersessionWriteGuardTests
{
    [Test]
    public async Task CreateRelation_Should_Reject_Bidirectional_Supersession()
    {
        var repository = new StatefulOemRepository();
        repository.Seed(Supersession(200, 100));
        var service = new OemAdminService(repository, new IdentityNormalizationService(), repository);

        var act = async () => await service.CreateRelationAsync(Supersession(100, 200), CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("oem.supersession.bidirectional");
        repository.Relations.Should().ContainSingle("the invalid reverse edge must not be persisted");
    }

    [Test]
    public async Task CreateRelation_Should_Reject_Transitive_Cycle()
    {
        var repository = new StatefulOemRepository();
        repository.Seed(Supersession(200, 300), Supersession(300, 100));
        var service = new OemAdminService(repository, new IdentityNormalizationService(), repository);

        var act = async () => await service.CreateRelationAsync(Supersession(100, 200), CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("oem.supersession.cycle");
    }

    [Test]
    public async Task CreateRelation_Should_Reject_Second_Outgoing_Supersession()
    {
        var repository = new StatefulOemRepository();
        repository.Seed(Supersession(100, 200));
        var service = new OemAdminService(repository, new IdentityNormalizationService(), repository);

        var act = async () => await service.CreateRelationAsync(Supersession(100, 300), CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("oem.supersession.source_already_superseded");
    }

    [Test]
    public async Task CreateRelation_Should_Allow_Valid_Linear_Extension()
    {
        var repository = new StatefulOemRepository();
        repository.Seed(Supersession(200, 300));
        var service = new OemAdminService(repository, new IdentityNormalizationService(), repository);

        await service.CreateRelationAsync(Supersession(100, 200), CancellationToken.None);

        repository.Relations.Should().Contain(relation =>
            relation.FromOemNumberId == 100 && relation.ToOemNumberId == 200);
    }

    [Test]
    public async Task CreateRelation_Should_Not_Constrain_NonSupersession_Types()
    {
        var repository = new StatefulOemRepository();
        // A reverse *supersession* exists, but the new edge is a cross-reference, so it is unconstrained.
        repository.Seed(Supersession(200, 100));
        var service = new OemAdminService(repository, new IdentityNormalizationService(), repository);

        await service.CreateRelationAsync(new OemRelation
        {
            FromOemNumberId = 100,
            ToOemNumberId = 200,
            RelationType = OemRelationType.CrossReference,
            IsActive = true
        }, CancellationToken.None);

        repository.Relations.Should().Contain(relation =>
            relation.RelationType == OemRelationType.CrossReference && relation.FromOemNumberId == 100);
    }

    [Test]
    public async Task BulkUpsert_Should_Normalize_And_Collapse_Duplicate_Keys()
    {
        var repository = new StatefulOemRepository();
        var service = new OemAdminService(repository, new IdentityNormalizationService(), repository);

        await service.BulkUpsertOemNumbersAsync(
        [
            new OemNumber { ManufacturerId = 10, DisplayNumber = "11-51-7-586-925", IsActive = true },
            new OemNumber { ManufacturerId = 10, DisplayNumber = "11517586925", IsActive = true, IsObsolete = true },
            new OemNumber { ManufacturerId = 20, DisplayNumber = "11517586925", IsActive = true }
        ], CancellationToken.None);

        // Manufacturer 10's two rows normalize to the same key and collapse to the last (obsolete) one.
        repository.LastBulkUpsert.Should().HaveCount(2);
        repository.LastBulkUpsert.Should().OnlyContain(number => number.NormalizedNumber == "11517586925");
        repository.LastBulkUpsert.Single(number => number.ManufacturerId == 10).IsObsolete.Should().BeTrue();
    }

    private static OemRelation Supersession(int from, int to) => new()
    {
        FromOemNumberId = from,
        ToOemNumberId = to,
        RelationType = OemRelationType.Supersession,
        IsActive = true
    };

    private sealed class IdentityNormalizationService : IOemNormalizationService
    {
        public string Normalize(string rawNumber)
            => string.IsNullOrWhiteSpace(rawNumber)
                ? string.Empty
                : new string(rawNumber.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    }

    private sealed class StatefulOemRepository : IOemAdminRepository, IOemRelationReadRepository
    {
        public List<OemRelation> Relations { get; } = [];
        public IReadOnlyList<OemNumber> LastBulkUpsert { get; private set; } = [];

        public void Seed(params OemRelation[] relations) => Relations.AddRange(relations);

        public Task<IReadOnlyList<OemRelation>> GetActiveOutgoingRelationsAsync(int fromOemNumberId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<OemRelation>>(
                Relations.Where(relation => relation.IsActive && relation.FromOemNumberId == fromOemNumberId).ToList());

        public Task CreateRelationAsync(OemRelation entity, CancellationToken cancellationToken)
        {
            Relations.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateRelationAsync(OemRelation entity, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<OemBulkUpsertResult> BulkUpsertOemNumbersAsync(IReadOnlyList<OemNumber> numbers, CancellationToken cancellationToken)
        {
            LastBulkUpsert = numbers;
            return Task.FromResult(new OemBulkUpsertResult { Inserted = numbers.Count, Updated = 0 });
        }

        public Task<IReadOnlyList<Manufacturer>> GetManufacturersAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Manufacturer>>([]);
        public Task<Manufacturer?> GetManufacturerByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<Manufacturer?>(null);
        public Task CreateManufacturerAsync(Manufacturer entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateManufacturerAsync(Manufacturer entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteManufacturerAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<OemNumber>> GetOemNumbersAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OemNumber>>([]);
        public Task<OemNumber?> GetOemNumberByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<OemNumber?>(null);
        public Task CreateOemNumberAsync(OemNumber entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateOemNumberAsync(OemNumber entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteOemNumberAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<OemRelation>> GetRelationsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OemRelation>>(Relations);
        public Task<OemRelation?> GetRelationByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<OemRelation?>(null);
        public Task DeleteRelationAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
