using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public interface IVendorOrderReadStore
{
    Task<IReadOnlyList<VendorOrderRecord>> GetOrdersForProductsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken);
}
