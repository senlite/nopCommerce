using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Oem;

namespace TwinParticles.CheckEngine.Application.Oem;

public sealed class OemSupersessionService
{
    private readonly IOemRelationReadRepository _relationReadRepository;

    public OemSupersessionService(IOemRelationReadRepository relationReadRepository)
    {
        _relationReadRepository = relationReadRepository;
    }

    public async Task<OemSupersessionChainResult> GetSupersessionChainAsync(int oemNumberId, int maxDepth, CancellationToken cancellationToken)
    {
        if (oemNumberId <= 0)
            return OemSupersessionChainResult.Fail("oem.invalid_id", []);

        if (maxDepth <= 0)
            return OemSupersessionChainResult.Fail("oem.invalid_depth", [oemNumberId]);

        var chain = new List<int> { oemNumberId };
        var visited = new HashSet<int> { oemNumberId };
        var current = oemNumberId;

        for (var depth = 0; depth < maxDepth; depth++)
        {
            var outgoing = await _relationReadRepository.GetActiveOutgoingRelationsAsync(current, cancellationToken);
            var supersessions = outgoing
                .Where(relation => relation.RelationType == OemRelationType.Supersession)
                .ToList();

            if (supersessions.Count == 0)
                return OemSupersessionChainResult.Ok(chain);

            if (supersessions.Count > 1)
                return OemSupersessionChainResult.Fail("oem.supersession_ambiguous", chain);

            var next = supersessions[0].ToOemNumberId;
            if (!visited.Add(next))
                return OemSupersessionChainResult.Fail("oem.supersession_cycle", chain);

            chain.Add(next);
            current = next;
        }

        var afterCapRelations = await _relationReadRepository.GetActiveOutgoingRelationsAsync(current, cancellationToken);
        var hasMoreSupersession = afterCapRelations.Any(relation => relation.RelationType == OemRelationType.Supersession);

        return hasMoreSupersession
            ? OemSupersessionChainResult.Fail("oem.supersession_depth_exceeded", chain)
            : OemSupersessionChainResult.Ok(chain);
    }
}
