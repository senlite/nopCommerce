using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Application.Marketplace;

/// <summary>
/// FR-857 / AC-19.1: vendors cannot read or modify another vendor's catalog, orders, or customers.
/// Operator admins bypass isolation. Shopper evaluation stays global.
/// </summary>
public sealed class VendorIsolationService
{
    private readonly IVendorOwnershipStore _ownership;
    private readonly IVendorOrderReadStore _orders;
    private readonly MarketplaceLicenceGate _licenceGate;
    private readonly ICheckEngineAuditService _auditService;

    public VendorIsolationService(
        IVendorOwnershipStore ownership,
        IVendorOrderReadStore orders,
        MarketplaceLicenceGate licenceGate,
        ICheckEngineAuditService auditService)
    {
        _ownership = ownership;
        _orders = orders;
        _licenceGate = licenceGate;
        _auditService = auditService;
    }

    public Task<VendorIsolationDecision> AuthorizeProductAsync(
        VendorActor actor,
        int productId,
        bool write,
        CancellationToken cancellationToken)
        => AuthorizeAsync(actor, "Product", productId.ToString(), write, () => OwnsProductAsync(actor, productId, cancellationToken), cancellationToken);

    public Task<VendorIsolationDecision> AuthorizeOrderAsync(
        VendorActor actor,
        int orderId,
        CancellationToken cancellationToken)
        => AuthorizeAsync(actor, "Order", orderId.ToString(), write: false, () => OwnsOrderAsync(actor, orderId, cancellationToken), cancellationToken);

    public Task<VendorIsolationDecision> AuthorizeCustomerAsync(
        VendorActor actor,
        int customerId,
        CancellationToken cancellationToken)
        => AuthorizeAsync(actor, "Customer", customerId.ToString(), write: false, () => OwnsCustomerAsync(actor, customerId, cancellationToken), cancellationToken);

    public async Task<IReadOnlyList<int>> ListCatalogAsync(VendorActor actor, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return [];
        if (actor.CanBypassIsolation)
            return await _ownership.GetAllMappedProductIdsAsync(cancellationToken);
        if (actor.VendorId is not int vendorId)
            return [];

        return await _ownership.GetProductIdsAsync(vendorId, cancellationToken);
    }

    public async Task<IReadOnlyList<VendorOrderRecord>> ListOrdersAsync(VendorActor actor, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return [];

        var productIds = await ListCatalogAsync(actor, cancellationToken);
        if (productIds.Count == 0)
            return [];

        return await _orders.GetOrdersForProductsAsync(productIds, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> ListCustomersAsync(VendorActor actor, CancellationToken cancellationToken)
    {
        var orders = await ListOrdersAsync(actor, cancellationToken);
        return orders.Select(order => order.CustomerId).Distinct().OrderBy(id => id).ToList();
    }

    public Task AssignProductAsync(int vendorId, int productId, CancellationToken cancellationToken)
        => _ownership.AssignProductAsync(vendorId, productId, cancellationToken);

    private async Task<VendorIsolationDecision> AuthorizeAsync(
        VendorActor actor,
        string entityType,
        string entityId,
        bool write,
        System.Func<Task<bool>> owns,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return await DenyAsync(actor, entityType, entityId, write, VendorErrorCodes.LicenceDenied, cancellationToken);

        if (actor.CanBypassIsolation)
            return VendorIsolationDecision.Allow();

        if (actor.VendorId is null)
            return await DenyAsync(actor, entityType, entityId, write, VendorErrorCodes.IsolationUnauthenticated, cancellationToken);

        if (await owns())
            return VendorIsolationDecision.Allow();

        return await DenyAsync(actor, entityType, entityId, write, VendorErrorCodes.IsolationDenied, cancellationToken);
    }

    private async Task<bool> OwnsProductAsync(VendorActor actor, int productId, CancellationToken cancellationToken)
    {
        var owner = await _ownership.GetProductVendorIdAsync(productId, cancellationToken);
        return owner.HasValue && owner.Value == actor.VendorId;
    }

    private async Task<bool> OwnsOrderAsync(VendorActor actor, int orderId, CancellationToken cancellationToken)
    {
        var orders = await ListOrdersAsync(actor, cancellationToken);
        return orders.Any(order => order.OrderId == orderId);
    }

    private async Task<bool> OwnsCustomerAsync(VendorActor actor, int customerId, CancellationToken cancellationToken)
    {
        var customers = await ListCustomersAsync(actor, cancellationToken);
        return customers.Contains(customerId);
    }

    private async Task<VendorIsolationDecision> DenyAsync(
        VendorActor actor,
        string entityType,
        string entityId,
        bool write,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        await _auditService.AppendAsync(
            actor.VendorId?.ToString() ?? "anonymous",
            write ? "vendor.isolation.write_denied" : "vendor.isolation.read_denied",
            entityType,
            entityId,
            beforeJson: null,
            afterJson: $"{{\"reasonCode\":\"{reasonCode}\"}}",
            cancellationToken);

        return VendorIsolationDecision.Deny(reasonCode);
    }
}

public sealed class VendorIsolationDecision
{
    public bool Allowed { get; init; }

    public string? ReasonCode { get; init; }

    public static VendorIsolationDecision Allow() => new() { Allowed = true };

    public static VendorIsolationDecision Deny(string reasonCode) => new() { Allowed = false, ReasonCode = reasonCode };
}
