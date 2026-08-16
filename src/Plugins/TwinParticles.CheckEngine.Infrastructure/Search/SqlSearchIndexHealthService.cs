using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

/// <summary>
/// Durable, web-farm-shared search index state (health, last rebuild, incremental cursor) plus an
/// incremental rebuild that materializes the <c>TP_CE_SearchIndex</c> keyword projection from the
/// nopCommerce catalog. Replaces the process-local in-memory flag so degradation and rebuilds survive
/// restarts and are visible to every node.
/// </summary>
public sealed class SqlSearchIndexHealthService : ISearchIndexHealthService, ISearchIndexStateReader
{
    private readonly INopDataProvider _dataProvider;

    public SqlSearchIndexHealthService(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            var state = await LoadStateAsync();
            return state.IsHealthy;
        }
        catch
        {
            // If the state row itself cannot be read, treat search as healthy so the live-catalog
            // path still serves; a genuine repository failure degrades via ReportDegradedAsync.
            return true;
        }
    }

    public async Task<SearchIndexState> GetStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await LoadStateAsync();
        }
        catch
        {
            return new SearchIndexState { IsHealthy = true };
        }
    }

    public Task ReportDegradedAsync(string reason, CancellationToken cancellationToken)
        => UpsertAsync(
            $@"UPDATE TP_CE_SearchIndexState
SET IsHealthy = 0, LastDegradedReason = @reason, LastDegradedUtc = {CheckEngineSql.UtcNow()}
WHERE Id = 1;",
            new DataParameter("reason", reason ?? "unknown"));

    public async Task RebuildAsync(CancellationToken cancellationToken)
    {
        await EnsureStateRowAsync();

        // Full rebuild: re-project the whole published catalog and reset the incremental cursor.
        await _dataProvider.ExecuteNonQueryAsync("DELETE FROM TP_CE_SearchIndex;");
        await ProjectCatalogAsync(sinceCursor: false);

        var count = (await _dataProvider.QueryAsync<CountRow>("SELECT COUNT(*) AS Value FROM TP_CE_SearchIndex;"))
            .First().Value;

        await _dataProvider.ExecuteNonQueryAsync(
            $@"UPDATE TP_CE_SearchIndexState
SET IsHealthy = 1, LastRebuildUtc = {CheckEngineSql.UtcNow()},
    LastCursorUtc = (SELECT MAX(UpdatedUtc) FROM TP_CE_SearchIndex),
    IndexedCount = @count, LastDegradedReason = NULL, LastDegradedUtc = NULL
WHERE Id = 1;",
            new DataParameter("count", count));
    }

    /// <summary>Incrementally projects only products changed since the recorded cursor.</summary>
    public async Task RefreshIncrementalAsync(CancellationToken cancellationToken)
    {
        await EnsureStateRowAsync();
        await ProjectCatalogAsync(sinceCursor: true);

        var count = (await _dataProvider.QueryAsync<CountRow>("SELECT COUNT(*) AS Value FROM TP_CE_SearchIndex;"))
            .First().Value;

        await _dataProvider.ExecuteNonQueryAsync(
            @"UPDATE TP_CE_SearchIndexState
SET IsHealthy = 1,
    LastCursorUtc = (SELECT MAX(UpdatedUtc) FROM TP_CE_SearchIndex),
    IndexedCount = @count
WHERE Id = 1;",
            new DataParameter("count", count));
    }

    // Delete+insert keeps existing projection rows current without MERGE, which MySQL does not
    // support. Deleted/unpublished products are removed so they never surface. NormalizedText is
    // lowercased name+sku+mpn for LIKE matching.
    private async Task ProjectCatalogAsync(bool sinceCursor)
    {
        var cursorFilter = sinceCursor
            ? "AND p.UpdatedOnUtc > COALESCE((SELECT LastCursorUtc FROM TP_CE_SearchIndexState WHERE Id = 1), '1900-01-01')"
            : string.Empty;
        var utcNow = CheckEngineSql.UtcNow();

        await _dataProvider.ExecuteNonQueryAsync($@"
DELETE si FROM TP_CE_SearchIndex si
INNER JOIN Product p ON p.Id = si.ProductId
WHERE p.Deleted = 0 AND p.Published = 1 AND p.VisibleIndividually = 1 {cursorFilter}");

        await _dataProvider.ExecuteNonQueryAsync($@"
INSERT INTO TP_CE_SearchIndex (ProductId, Name, NormalizedText, Sku, Mpn, Price, UpdatedUtc, IndexedUtc)
SELECT p.Id,
       p.Name,
       LOWER(CONCAT(p.Name, ' ', COALESCE(p.Sku, ''), ' ', COALESCE(p.ManufacturerPartNumber, ''))),
       p.Sku,
       p.ManufacturerPartNumber,
       p.Price,
       p.UpdatedOnUtc,
       {utcNow}
FROM Product p
WHERE p.Deleted = 0 AND p.Published = 1 AND p.VisibleIndividually = 1 {cursorFilter}");

        await _dataProvider.ExecuteNonQueryAsync(@"
DELETE si FROM TP_CE_SearchIndex si
WHERE NOT EXISTS (
    SELECT 1 FROM Product p
    WHERE p.Id = si.ProductId AND p.Deleted = 0 AND p.Published = 1 AND p.VisibleIndividually = 1)");
    }

    private async Task<SearchIndexState> LoadStateAsync()
    {
        await EnsureStateRowAsync();
        var rows = await _dataProvider.QueryAsync<StateRow>(
            "SELECT IsHealthy, LastRebuildUtc, LastCursorUtc, IndexedCount FROM TP_CE_SearchIndexState WHERE Id = 1;");
        var row = rows.First();
        return new SearchIndexState
        {
            IsHealthy = row.IsHealthy,
            LastRebuildUtc = row.LastRebuildUtc,
            LastCursorUtc = row.LastCursorUtc,
            IndexedCount = row.IndexedCount
        };
    }

    private Task EnsureStateRowAsync()
        => _dataProvider.ExecuteNonQueryAsync(
            @"INSERT INTO TP_CE_SearchIndexState (Id, IsHealthy, IndexedCount)
SELECT 1, 1, 0
WHERE NOT EXISTS (SELECT 1 FROM TP_CE_SearchIndexState WHERE Id = 1);");

    private async Task UpsertAsync(string sql, params DataParameter[] parameters)
    {
        await EnsureStateRowAsync();
        await _dataProvider.ExecuteNonQueryAsync(sql, parameters);
    }

    private sealed class StateRow
    {
        public bool IsHealthy { get; set; }
        public DateTime? LastRebuildUtc { get; set; }
        public DateTime? LastCursorUtc { get; set; }
        public int IndexedCount { get; set; }
    }

    private sealed class CountRow
    {
        public int Value { get; set; }
    }
}
