using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Domain.Oem;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class OemResolveServiceTests
{
    [Test]
    public async Task ResolveAsync_Should_Return_NotFound_When_No_Match()
    {
        var service = CreateService(new FakeSearchRepository(), new FakeRelationReadRepository());

        var result = await service.ResolveAsync(new OemResolveQuery { Number = "11-51-7-586-925" }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("oem.not_found");
    }

    [Test]
    public async Task ResolveAsync_Should_Return_Ambiguous_When_Multiple_Manufacturers_Match()
    {
        var service = CreateService(new FakeSearchRepository(
            new OemNumber { Id = 1, ManufacturerId = 10, DisplayNumber = "A", NormalizedNumber = "N" },
            new OemNumber { Id = 2, ManufacturerId = 20, DisplayNumber = "B", NormalizedNumber = "N" }),
            new FakeRelationReadRepository());

        var result = await service.ResolveAsync(new OemResolveQuery { Number = "11-51-7-586-925" }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("oem.ambiguous_manufacturer");
        result.CandidateManufacturerIds.Should().BeEquivalentTo([10, 20]);
    }

    [Test]
    public async Task ResolveAsync_Should_Return_Resolved_Number_And_Current_Id_When_Superseded()
    {
        var service = CreateService(new FakeSearchRepository(
                new OemNumber { Id = 100, ManufacturerId = 10, DisplayNumber = "11-51-7-586-925", NormalizedNumber = "11517586925", IsObsolete = true }),
            new FakeRelationReadRepository(new OemRelation
            {
                FromOemNumberId = 100,
                ToOemNumberId = 200,
                RelationType = OemRelationType.Supersession,
                IsActive = true
            }));

        var result = await service.ResolveAsync(new OemResolveQuery { Number = "11-51-7-586-925", ManufacturerId = 10 }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.OemNumberId.Should().Be(100);
        result.CurrentOemNumberId.Should().Be(200);
        result.NormalizedNumber.Should().Be("11517586925");
    }

    private static OemResolveService CreateService(IOemSearchReadRepository searchRepository, IOemRelationReadRepository relationReadRepository)
    {
        var supersessionService = new OemSupersessionService(relationReadRepository);
        return new OemResolveService(new FakeNormalizationService(), searchRepository, supersessionService, new EmptyProductOemMapRepository());
    }

    private sealed class FakeNormalizationService : IOemNormalizationService
    {
        public string Normalize(string rawNumber)
        {
            return string.IsNullOrWhiteSpace(rawNumber) ? string.Empty : "11517586925";
        }
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
            var result = _matches
                .Where(x => !manufacturerId.HasValue || x.ManufacturerId == manufacturerId.Value)
                .ToList();

            return Task.FromResult<IReadOnlyList<OemNumber>>(result);
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
