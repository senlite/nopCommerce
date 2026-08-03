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
public class OemSupersessionServiceTests
{
    [Test]
    public async Task GetSupersessionChainAsync_Should_Return_Queried_Node_When_No_Supersession_Exists()
    {
        var service = new OemSupersessionService(new FakeRelationReadRepository());

        var result = await service.GetSupersessionChainAsync(100, 10, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
        result.ChainOemNumberIds.Should().Equal(100);
    }

    [Test]
    public async Task GetSupersessionChainAsync_Should_Return_Transitive_Chain_To_Leaf()
    {
        var relations = new[]
        {
            Supersession(100, 200),
            Supersession(200, 300)
        };

        var service = new OemSupersessionService(new FakeRelationReadRepository(relations));

        var result = await service.GetSupersessionChainAsync(100, 10, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ChainOemNumberIds.Should().Equal(100, 200, 300);
    }

    [Test]
    public async Task GetSupersessionChainAsync_Should_Return_Cycle_Error_When_Relation_Loops()
    {
        var relations = new[]
        {
            Supersession(100, 200),
            Supersession(200, 100)
        };

        var service = new OemSupersessionService(new FakeRelationReadRepository(relations));

        var result = await service.GetSupersessionChainAsync(100, 10, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("oem.supersession_cycle");
        result.ChainOemNumberIds.Should().Equal(100, 200);
    }

    [Test]
    public async Task GetSupersessionChainAsync_Should_Return_Ambiguous_Error_When_Multiple_Supersessions_Exist()
    {
        var relations = new[]
        {
            Supersession(100, 200),
            Supersession(100, 300)
        };

        var service = new OemSupersessionService(new FakeRelationReadRepository(relations));

        var result = await service.GetSupersessionChainAsync(100, 10, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("oem.supersession_ambiguous");
        result.ChainOemNumberIds.Should().Equal(100);
    }

    [Test]
    public async Task GetSupersessionChainAsync_Should_Return_Depth_Exceeded_Error_When_Chain_Exceeds_Cap()
    {
        var relations = new[]
        {
            Supersession(100, 101),
            Supersession(101, 102),
            Supersession(102, 103)
        };

        var service = new OemSupersessionService(new FakeRelationReadRepository(relations));

        var result = await service.GetSupersessionChainAsync(100, 2, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("oem.supersession_depth_exceeded");
        result.ChainOemNumberIds.Should().Equal(100, 101, 102);
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
}
