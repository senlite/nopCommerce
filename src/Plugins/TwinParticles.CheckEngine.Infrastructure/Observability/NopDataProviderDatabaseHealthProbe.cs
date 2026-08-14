using System.Threading;
using System.Threading.Tasks;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Observability;

namespace TwinParticles.CheckEngine.Infrastructure.Observability;

/// <summary>
/// Probes database connectivity via INopDataProvider with a trivial query.
/// </summary>
public sealed class NopDataProviderDatabaseHealthProbe : ICheckEngineDatabaseHealthProbe
{
    private readonly INopDataProvider _dataProvider;

    public NopDataProviderDatabaseHealthProbe(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<bool> CanQueryAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _dataProvider.QueryAsync<ScalarIntRow>("SELECT 1 AS Value");
        return rows is { Count: > 0 };
    }

    private sealed class ScalarIntRow
    {
        public int Value { get; init; }
    }
}
