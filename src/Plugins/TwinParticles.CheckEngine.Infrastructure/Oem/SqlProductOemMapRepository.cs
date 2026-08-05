using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Oem;

namespace TwinParticles.CheckEngine.Infrastructure.Oem;

public sealed class SqlProductOemMapRepository : IProductOemMapRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlProductOemMapRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task UpsertAsync(ProductOemMap map, CancellationToken cancellationToken)
    {
        const string updateSql = @"UPDATE TP_CE_ProductOemMap
SET IsPrimary = @isPrimary
WHERE ProductId = @productId AND OemNumberId = @oemNumberId";

        var updated = await _dataProvider.ExecuteNonQueryAsync(updateSql,
            new DataParameter("isPrimary", map.IsPrimary),
            new DataParameter("productId", map.ProductId),
            new DataParameter("oemNumberId", map.OemNumberId));

        if (updated > 0)
            return;

        await _dataProvider.ExecuteNonQueryAsync(
            @"INSERT INTO TP_CE_ProductOemMap (ProductId, OemNumberId, IsPrimary, CreatedUtc)
VALUES (@productId, @oemNumberId, @isPrimary, @createdUtc)",
            new DataParameter("productId", map.ProductId),
            new DataParameter("oemNumberId", map.OemNumberId),
            new DataParameter("isPrimary", map.IsPrimary),
            new DataParameter("createdUtc", map.CreatedUtc == default ? System.DateTime.UtcNow : map.CreatedUtc));
    }

    public async Task<IReadOnlyList<ProductOemMap>> GetByProductIdAsync(int productId, CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<ProductOemMap>(
            "SELECT Id, ProductId, OemNumberId, IsPrimary, CreatedUtc FROM TP_CE_ProductOemMap WHERE ProductId = @productId ORDER BY Id",
            new DataParameter("productId", productId))).ToList();

    public async Task<IReadOnlyList<ProductOemMap>> GetByOemNumberIdAsync(int oemNumberId, CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<ProductOemMap>(
            "SELECT Id, ProductId, OemNumberId, IsPrimary, CreatedUtc FROM TP_CE_ProductOemMap WHERE OemNumberId = @oemNumberId ORDER BY Id",
            new DataParameter("oemNumberId", oemNumberId))).ToList();
}
