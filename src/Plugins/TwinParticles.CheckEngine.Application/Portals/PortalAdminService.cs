using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Dealer;
using TwinParticles.CheckEngine.Domain.Fleet;
using TwinParticles.CheckEngine.Domain.Workshop;

namespace TwinParticles.CheckEngine.Application.Portals;

/// <summary>
/// Operator provisioning for vertical portal trade accounts (workshop, fleet, dealer).
/// </summary>
public sealed class PortalAdminService
{
    private readonly IWorkshopJobRepository _workshop;
    private readonly IFleetPortalRepository _fleet;
    private readonly IDealerPortalRepository _dealer;
    private readonly WorkshopPortalLicenceGate _workshopGate;
    private readonly FleetPortalLicenceGate _fleetGate;
    private readonly DealerPortalLicenceGate _dealerGate;

    public PortalAdminService(
        IWorkshopJobRepository workshop,
        IFleetPortalRepository fleet,
        IDealerPortalRepository dealer,
        WorkshopPortalLicenceGate workshopGate,
        FleetPortalLicenceGate fleetGate,
        DealerPortalLicenceGate dealerGate)
    {
        _workshop = workshop;
        _fleet = fleet;
        _dealer = dealer;
        _workshopGate = workshopGate;
        _fleetGate = fleetGate;
        _dealerGate = dealerGate;
    }

    public async Task<PortalProvisionResult> ProvisionWorkshopAccountAsync(ProvisionAccountRequest request, CancellationToken cancellationToken)
    {
        if (!await _workshopGate.AllowsWorkshopAsync(cancellationToken))
            return PortalProvisionResult.Fail("portal.licence_denied");

        var existing = await _workshop.GetAccountByCustomerIdAsync(request.CustomerId, cancellationToken);
        if (existing is not null)
            return PortalProvisionResult.Fail("portal.account_exists");

        var account = new WorkshopAccount
        {
            CustomerId = request.CustomerId,
            DisplayName = request.DisplayName.Trim(),
            CreditLimit = request.CreditLimit ?? 50_000m,
            CreditUsed = 0m,
            IsActive = true
        };

        account.Id = await _workshop.InsertAccountAsync(account, cancellationToken);
        return PortalProvisionResult.Ok(account.Id, "workshop");
    }

    public async Task<PortalProvisionResult> ProvisionFleetAccountAsync(ProvisionAccountRequest request, CancellationToken cancellationToken)
    {
        if (!await _fleetGate.AllowsFleetAsync(cancellationToken))
            return PortalProvisionResult.Fail("portal.licence_denied");

        var existing = await _fleet.GetAccountByCustomerIdAsync(request.CustomerId, cancellationToken);
        if (existing is not null)
            return PortalProvisionResult.Fail("portal.account_exists");

        var account = new FleetAccount
        {
            CustomerId = request.CustomerId,
            DisplayName = request.DisplayName.Trim(),
            IsActive = true
        };

        account.Id = await _fleet.InsertAccountAsync(account, cancellationToken);

        var centre = new FleetBudgetCentre
        {
            FleetAccountId = account.Id,
            Name = "Default",
            SpendLimit = request.CreditLimit ?? 25_000m,
            SpendUsed = 0m
        };

        centre.Id = await _fleet.InsertBudgetCentreAsync(centre, cancellationToken);
        account.DefaultBudgetCentreId = centre.Id;
        return PortalProvisionResult.Ok(account.Id, "fleet", centre.Id);
    }

    public async Task<PortalProvisionResult> ProvisionDealerAccountAsync(ProvisionAccountRequest request, CancellationToken cancellationToken)
    {
        if (!await _dealerGate.AllowsDealerAsync(cancellationToken))
            return PortalProvisionResult.Fail("portal.licence_denied");

        var existing = await _dealer.GetAccountByCustomerIdAsync(request.CustomerId, cancellationToken);
        if (existing is not null)
            return PortalProvisionResult.Fail("portal.account_exists");

        var account = new DealerAccount
        {
            CustomerId = request.CustomerId,
            DisplayName = request.DisplayName.Trim(),
            IsActive = true
        };

        account.Id = await _dealer.InsertAccountAsync(account, cancellationToken);
        return PortalProvisionResult.Ok(account.Id, "dealer");
    }
}

public sealed class ProvisionAccountRequest
{
    public int CustomerId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public decimal? CreditLimit { get; init; }
}

public sealed class PortalProvisionResult
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public int? AccountId { get; init; }

    public string? PortalKind { get; init; }

    public int? DefaultBudgetCentreId { get; init; }

    public static PortalProvisionResult Ok(int accountId, string portalKind, int? budgetCentreId = null)
        => new() { Success = true, AccountId = accountId, PortalKind = portalKind, DefaultBudgetCentreId = budgetCentreId };

    public static PortalProvisionResult Fail(string errorCode)
        => new() { Success = false, ErrorCode = errorCode };
}
