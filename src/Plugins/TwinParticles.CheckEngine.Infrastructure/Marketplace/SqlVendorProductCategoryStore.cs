using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Infrastructure.Marketplace;

public sealed class SqlVendorProductCategoryStore : IVendorProductCategoryStore
{
    private readonly INopDataProvider _dataProvider;

    public SqlVendorProductCategoryStore(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<int>>> GetCategoryIdsByProductIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return new Dictionary<int, IReadOnlyList<int>>();

        var inList = string.Join(",", productIds.Select(id => id.ToString()));
        var rows = await _dataProvider.QueryAsync<CategoryRow>($@"
SELECT ProductId, CategoryId
FROM Product_Category_Mapping
WHERE ProductId IN ({inList})");

        return rows
            .GroupBy(row => row.ProductId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<int>)group.Select(row => row.CategoryId).Distinct().OrderBy(id => id).ToList());
    }

    private sealed class CategoryRow
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
    }
}
