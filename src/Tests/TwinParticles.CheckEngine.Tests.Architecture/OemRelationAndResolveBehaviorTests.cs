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
public class OemRelationAndResolveBehaviorTests
{
    [Test]
    public void CreateManufacturerAsync_Should_Reject_Empty_Code()
    {
        var service = new OemAdminService(new FakeAdminRepository(), new FakeNormalizationService());

        Func<Task> act = async () => await service.CreateManufacturerAsync(new Manufacturer
        {
            Code = "",
            Name = "BMW",
            IsActive = true
        }, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public void CreateOemNumberAsync_Should_Reject_Missing_Manufacturer()
    {
        var service = new OemAdminService(new FakeAdminRepository(), new FakeNormalizationService());

        Func<Task> act = async () => await service.CreateOemNumberAsync(new OemNumber
        {
            ManufacturerId = 0,
            DisplayNumber = "11-51-7-586-925",
            IsActive = true
        }, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public void CreateOemNumberAsync_Should_Reject_When_Normalized_Number_Is_Empty()
    {
        var service = new OemAdminService(new FakeAdminRepository(), new EmptyNormalizationService());

        Func<Task> act = async () => await service.CreateOemNumberAsync(new OemNumber
        {
            ManufacturerId = 10,
            DisplayNumber = "**",
            IsActive = true
        }, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public void CreateRelationAsync_Should_Reject_Missing_Endpoints()
    {
        var service = new OemAdminService(new FakeAdminRepository(), new FakeNormalizationService());

        Func<Task> act = async () => await service.CreateRelationAsync(new OemRelation
        {
            FromOemNumberId = 0,
            ToOemNumberId = 100,
            RelationType = OemRelationType.CrossReference,
            IsActive = true
        }, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task ResolveAsync_Should_Not_Set_CurrentOemNumberId_When_Supersession_Is_Ambiguous()
    {
        var service = CreateResolveService(
            new FakeSearchRepository(
                new OemNumber { Id = 100, ManufacturerId = 10, DisplayNumber = "11-51-7-586-925", NormalizedNumber = "11517586925", IsObsolete = true }),
            new FakeRelationReadRepository(
                Supersession(100, 200),
                Supersession(100, 300)));

        var result = await service.ResolveAsync(new OemResolveQuery { Number = "11-51-7-586-925", ManufacturerId = 10 }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.CurrentOemNumberId.Should().BeNull();
    }

    [Test]
    public async Task ResolveAsync_Should_Not_Set_CurrentOemNumberId_When_Supersession_Depth_Exceeded()
    {
        var service = CreateResolveService(
            new FakeSearchRepository(
                new OemNumber { Id = 100, ManufacturerId = 10, DisplayNumber = "11-51-7-586-925", NormalizedNumber = "11517586925", IsObsolete = true }),
            BuildDepthExceededRelations());

        var result = await service.ResolveAsync(new OemResolveQuery { Number = "11-51-7-586-925", ManufacturerId = 10 }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.CurrentOemNumberId.Should().BeNull();
    }

    private static OemResolveService CreateResolveService(IOemSearchReadRepository searchRepository, IOemRelationReadRepository relationRepository)
    {
        var supersessionService = new OemSupersessionService(relationRepository);
        return new OemResolveService(new FakeNormalizationService(), searchRepository, supersessionService, new EmptyProductOemMapRepository());
    }

    private static FakeRelationReadRepository BuildDepthExceededRelations()
    {
        var relations = new List<OemRelation>();
        for (var i = 100; i <= 110; i++)
        {
            relations.Add(Supersession(i, i + 1));
        }

        return new FakeRelationReadRepository(relations.ToArray());
    }

    private static OemRelation Supersession(int from, int to)
    {
        return new OemRelation
        {
            FromOemNumberId = from,
            ToOemNumberId = to,
            RelationType = OemRelationType.Supersession,
            IsActive = true
        };
    }

    private sealed class FakeNormalizationService : IOemNormalizationService
    {
        public string Normalize(string rawNumber)
        {
            if (string.IsNullOrWhiteSpace(rawNumber))
                return string.Empty;

            return new string(rawNumber.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        }
    }

    private sealed class EmptyNormalizationService : IOemNormalizationService
    {
        public string Normalize(string rawNumber) => string.Empty;
    }

    private sealed class FakeAdminRepository : IOemAdminRepository
    {
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
        public Task<OemBulkUpsertResult> BulkUpsertOemNumbersAsync(IReadOnlyList<OemNumber> numbers, CancellationToken cancellationToken) => Task.FromResult(OemBulkUpsertResult.Empty);
        public Task<IReadOnlyList<OemRelation>> GetRelationsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OemRelation>>([]);
        public Task<OemRelation?> GetRelationByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<OemRelation?>(null);
        public Task CreateRelationAsync(OemRelation entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateRelationAsync(OemRelation entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteRelationAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeSearchRepository : IOemSearchReadRepository
    {
        private readonly IReadOnlyList<OemNumber> _matches;

        public FakeSearchRepository(params OemNumber[] matches)
        {
            _matches = matches;
        }

        public Task<IReadOnlyList<OemNumber>> FindByNormalizedNumberAsync(string normalizedNumber, int? manufacturerId, CancellationToken cancellationToken)
        {
            var results = _matches
                .Where(x => x.NormalizedNumber == normalizedNumber)
                .Where(x => !manufacturerId.HasValue || x.ManufacturerId == manufacturerId.Value)
                .ToList();

            return Task.FromResult<IReadOnlyList<OemNumber>>(results);
        }
    }

    private sealed class FakeRelationReadRepository : IOemRelationReadRepository
    {
        private readonly IReadOnlyList<OemRelation> _relations;

        public FakeRelationReadRepository(params OemRelation[] relations)
        {
            _relations = relations;
        }

        public Task<IReadOnlyList<OemRelation>> GetActiveOutgoingRelationsAsync(int fromOemNumberId, CancellationToken cancellationToken)
        {
            var result = _relations
                .Where(relation => relation.IsActive && relation.FromOemNumberId == fromOemNumberId)
                .ToList();

            return Task.FromResult<IReadOnlyList<OemRelation>>(result);
        }
    }

    private sealed class EmptyProductOemMapRepository : IProductOemMapRepository
    {
        public Task UpsertAsync(ProductOemMap map, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<IReadOnlyList<ProductOemMap>> GetByProductIdAsync(int productId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ProductOemMap>>([]);

        public Task<IReadOnlyList<ProductOemMap>> GetByOemNumberIdAsync(int oemNumberId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ProductOemMap>>([]);
    }
}
