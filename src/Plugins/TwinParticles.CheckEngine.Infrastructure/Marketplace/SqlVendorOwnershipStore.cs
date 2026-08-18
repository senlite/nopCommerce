using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Infrastructure.Marketplace;

public sealed class SqlVendorOwnershipStore : IVendorOwnershipStore
{
    private readonly INopDataProvider _dataProvider;

    public SqlVendorOwnershipStore(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task AssignProductAsync(int vendorId, int productId, CancellationToken cancellationToken)
    {
        var updated = await _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_VendorProductMap
SET VendorId = @vendorId
WHERE ProductId = @productId",
            new DataParameter("vendorId", vendorId),
            new DataParameter("productId", productId));

        if (updated > 0)
            return;

        await _dataProvider.ExecuteNonQueryAsync(@"
INSERT INTO TP_CE_VendorProductMap (VendorId, ProductId)
VALUES (@vendorId, @productId)",
            new DataParameter("vendorId", vendorId),
            new DataParameter("productId", productId));
    }

    public async Task<int?> GetProductVendorIdAsync(int productId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<int>(@"
SELECT VendorId AS Value
FROM TP_CE_VendorProductMap
WHERE ProductId = @productId",
            new DataParameter("productId", productId));

        var vendorId = rows.FirstOrDefault();
        return vendorId > 0 ? vendorId : null;
    }

    public async Task<IReadOnlyList<int>> GetProductIdsAsync(int vendorId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<int>(@"
SELECT ProductId AS Value
FROM TP_CE_VendorProductMap
WHERE VendorId = @vendorId
ORDER BY ProductId",
            new DataParameter("vendorId", vendorId));

        return rows.ToList();
    }

    public async Task<IReadOnlyList<int>> GetAllMappedProductIdsAsync(CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<int>(@"
SELECT ProductId AS Value
FROM TP_CE_VendorProductMap
ORDER BY ProductId");

        return rows.ToList();
    }

    public async Task<int> AssignUnmappedProductsAsync(int operatorVendorId, IReadOnlyList<int> productIds, CancellationToken cancellationToken)
    {
        var assigned = 0;
        foreach (var productId in productIds)
        {
            var owner = await GetProductVendorIdAsync(productId, cancellationToken);
            if (owner.HasValue)
                continue;

            await AssignProductAsync(operatorVendorId, productId, cancellationToken);
            assigned++;
        }

        return assigned;
    }
}
