using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Portals;

/// <summary>
/// Creates nopCommerce orders for vertical portal workflows without routing through the retail cart.
/// </summary>
public interface IPortalOrderBridge
{
    Task<int> CreateTradeOrderAsync(
        int customerId,
        IReadOnlyList<PortalOrderLine> lines,
        decimal supplementaryLabourFee,
        CancellationToken cancellationToken);
}
