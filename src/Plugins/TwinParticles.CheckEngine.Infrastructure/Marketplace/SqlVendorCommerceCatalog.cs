using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Infrastructure.Marketplace;

public sealed class SqlVendorCommerceCatalog : IVendorCommerceCatalog
{
    private readonly INopDataProvider _dataProvider;

    public SqlVendorCommerceCatalog(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<IReadOnlyList<int>> ListSellableProductIdsAsync(CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<int>(@"
SELECT Id AS Value
FROM Product
WHERE Deleted = 0
ORDER BY Id");

        return rows.ToList();
    }
}
