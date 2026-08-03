using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases;

public sealed class InMemoryVehicleAliasRepository : IVehicleAliasWriteRepository, IVehicleAliasReadRepository
{
    private readonly ConcurrentDictionary<string, VehicleAlias> _aliases = new(StringComparer.OrdinalIgnoreCase);

    public Task UpsertAsync(VehicleAlias alias, CancellationToken cancellationToken)
    {
        var key = BuildUniqueKey(alias.NodeType, alias.NodeId, alias.Locale, alias.NormalizedAlias);
        _aliases[key] = alias;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VehicleAliasSearchItem>> SearchAsync(VehicleAliasSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var term = criteria.Term.Trim();

        var results = _aliases.Values
            .Where(x => string.Equals(x.Locale, criteria.Locale, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.AliasText.Contains(term, StringComparison.OrdinalIgnoreCase) || x.NormalizedAlias.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Take(criteria.Take)
            .Select(x => new VehicleAliasSearchItem
            {
                NodeType = x.NodeType,
                NodeId = x.NodeId,
                Locale = x.Locale,
                AliasText = x.AliasText
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<VehicleAliasSearchItem>>(results);
    }

    private static string BuildUniqueKey(string nodeType, int nodeId, string locale, string normalizedAlias)
        => $"{nodeType}|{nodeId}|{locale}|{normalizedAlias}";
}
