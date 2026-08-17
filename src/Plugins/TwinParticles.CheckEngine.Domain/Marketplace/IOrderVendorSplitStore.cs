using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public interface IOrderVendorSplitStore
{
    Task<bool> ExistsForOrderAsync(int orderId, CancellationToken cancellationToken);

    Task SaveCheckoutGroupAsync(OrderCheckoutGroup group, CancellationToken cancellationToken);

    Task<OrderCheckoutGroup?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken);

    Task<bool> ShipmentMapExistsAsync(int shipmentId, CancellationToken cancellationToken);

    Task SaveShipmentVendorMapsAsync(IReadOnlyCollection<ShipmentVendorMap> maps, CancellationToken cancellationToken);

    Task<IReadOnlyList<ShipmentVendorMap>> GetShipmentMapsByOrderIdAsync(int orderId, CancellationToken cancellationToken);
}
