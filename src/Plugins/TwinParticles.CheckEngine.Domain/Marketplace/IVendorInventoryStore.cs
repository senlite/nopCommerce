using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public interface IVendorInventoryStore
{
    Task<IReadOnlyList<VendorInventoryItem>> ListAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken);

    Task UpdateStockAsync(int productId, int stockQuantity, CancellationToken cancellationToken);
}
