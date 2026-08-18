using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Infrastructure.Marketplace;

public sealed class SqlVendorInventoryStore : IVendorInventoryStore
{
    private readonly INopDataProvider _dataProvider;

    public SqlVendorInventoryStore(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<IReadOnlyList<VendorInventoryItem>> ListAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return [];

        var inList = string.Join(",", productIds.Select(id => id.ToString()));
        var rows = await _dataProvider.QueryAsync<InventoryRow>($@"
SELECT p.Id AS ProductId, p.Name, p.StockQuantity, p.Published
FROM Product p
WHERE p.Deleted = 0 AND p.Id IN ({inList})
ORDER BY p.Name");

        return rows.Select(row => new VendorInventoryItem
        {
            ProductId = row.ProductId,
            Name = row.Name,
            StockQuantity = row.StockQuantity,
            Published = row.Published
        }).ToList();
    }

    public Task UpdateStockAsync(int productId, int stockQuantity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(
            "UPDATE Product SET StockQuantity = @stockQuantity WHERE Id = @productId AND Deleted = 0",
            new DataParameter("stockQuantity", stockQuantity),
            new DataParameter("productId", productId));

    private sealed class InventoryRow
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public bool Published { get; set; }
    }
}
