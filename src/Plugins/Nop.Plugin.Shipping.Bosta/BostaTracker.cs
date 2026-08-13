using Nop.Core.Domain.Shipping;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Shipping.Bosta;

/// <summary>
/// Links a Bosta tracking number to Bosta's public tracking page. Event history requires the Bosta
/// API and is intentionally empty in the reference plugin.
/// </summary>
public class BostaTracker : IShipmentTracker
{
    public Task<string> GetUrlAsync(string trackingNumber, Shipment? shipment = null)
        => Task.FromResult($"https://bosta.co/tracking-shipment?id={trackingNumber}");

    public Task<IList<ShipmentStatusEvent>> GetShipmentEventsAsync(string trackingNumber, Shipment? shipment = null)
        => Task.FromResult<IList<ShipmentStatusEvent>>([]);
}
