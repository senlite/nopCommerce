using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Application.Marketplace;

/// <summary>
/// FR-890 / AC-19.5: enabling marketplace assigns the existing catalog to the operator vendor
/// without dropping products.
/// </summary>
public sealed class MarketplaceUpgradeService
{
    public const string OperatorLegalName = "Operator";
    public const string OperatorEmail = "operator@checkengine.local";

    private readonly IVendorRepository _vendors;
    private readonly IVendorOwnershipStore _ownership;
    private readonly IVendorCommerceCatalog _catalog;
    private readonly MarketplaceLicenceGate _licenceGate;
    private readonly ICheckEngineClock _clock;
    private readonly ICheckEngineAuditService _auditService;
    private readonly IVehicleSeedLoader _vehicleSeedLoader;

    public MarketplaceUpgradeService(
        IVendorRepository vendors,
        IVendorOwnershipStore ownership,
        IVendorCommerceCatalog catalog,
        MarketplaceLicenceGate licenceGate,
        ICheckEngineClock clock,
        ICheckEngineAuditService auditService,
        IVehicleSeedLoader vehicleSeedLoader)
    {
        _vendors = vendors;
        _ownership = ownership;
        _catalog = catalog;
        _licenceGate = licenceGate;
        _clock = clock;
        _auditService = auditService;
        _vehicleSeedLoader = vehicleSeedLoader;
    }

    public async Task<MarketplaceUpgradeResult> EnableAsync(CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return MarketplaceUpgradeResult.Fail(VendorErrorCodes.LicenceDenied);

        var operatorVendor = await EnsureOperatorAsync(cancellationToken);
        await _vehicleSeedLoader.SeedAsync(cancellationToken);
        var productIds = await _catalog.ListSellableProductIdsAsync(cancellationToken);
        var newlyAssigned = await _ownership.AssignUnmappedProductsAsync(operatorVendor.Id, productIds, cancellationToken);

        var preserved = true;
        foreach (var productId in productIds)
        {
            if (await _ownership.GetProductVendorIdAsync(productId, cancellationToken) is null)
            {
                preserved = false;
                break;
            }
        }

        await _auditService.AppendAsync(
            "check-engine",
            "marketplace.upgrade",
            "Vendor",
            operatorVendor.Id.ToString(),
            beforeJson: null,
            afterJson: $"{{\"productCount\":{productIds.Count},\"newlyAssigned\":{newlyAssigned},\"catalogPreserved\":{preserved.ToString().ToLowerInvariant()}}}",
            cancellationToken);

        return new MarketplaceUpgradeResult
        {
            Succeeded = preserved,
            ReasonCode = preserved ? null : "marketplace.upgrade.unassigned_products",
            OperatorVendorId = operatorVendor.Id,
            ProductCount = productIds.Count,
            NewlyAssigned = newlyAssigned,
            AlreadyAssigned = productIds.Count - newlyAssigned,
            CatalogPreserved = preserved
        };
    }

    private async Task<Vendor> EnsureOperatorAsync(CancellationToken cancellationToken)
    {
        var existing = await _vendors.GetOperatorAsync(cancellationToken);
        if (existing is not null)
            return existing;

        var now = _clock.UtcNow;
        var vendor = new Vendor
        {
            LegalName = OperatorLegalName,
            TradingName = OperatorLegalName,
            ContactEmail = OperatorEmail,
            TaxIdsJson = "[\"operator\"]",
            CategoriesCsv = "all",
            Status = VendorStatus.Active,
            IsOperator = true,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        vendor.Id = await _vendors.InsertAsync(vendor, cancellationToken);
        return vendor;
    }
}
