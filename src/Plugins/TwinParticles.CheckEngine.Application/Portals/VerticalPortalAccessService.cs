using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Dealer;
using TwinParticles.CheckEngine.Domain.Fleet;
using TwinParticles.CheckEngine.Domain.Workshop;

namespace TwinParticles.CheckEngine.Application.Portals;

/// <summary>
/// Resolves whether a storefront customer may use a vertical portal (provisioned trade account or operator).
/// </summary>
public sealed class VerticalPortalAccessService
{
    private readonly IWorkshopJobRepository _workshop;
    private readonly IFleetPortalRepository _fleet;
    private readonly IDealerPortalRepository _dealer;

    public VerticalPortalAccessService(
        IWorkshopJobRepository workshop,
        IFleetPortalRepository fleet,
        IDealerPortalRepository dealer)
    {
        _workshop = workshop;
        _fleet = fleet;
        _dealer = dealer;
    }

    public async Task<PortalAccessResult> ResolveWorkshopAsync(int customerId, bool isOperator, CancellationToken cancellationToken)
    {
        var account = await _workshop.GetAccountByCustomerIdAsync(customerId, cancellationToken);
        if (account is { IsActive: true })
            return PortalAccessResult.ForAccount(account.Id);

        return isOperator ? PortalAccessResult.Operator() : PortalAccessResult.Denied(PortalErrorCodes.AccountNotProvisioned);
    }

    public async Task<PortalAccessResult> ResolveFleetAsync(int customerId, bool isOperator, CancellationToken cancellationToken)
    {
        var account = await _fleet.GetAccountByCustomerIdAsync(customerId, cancellationToken);
        if (account is { IsActive: true })
            return PortalAccessResult.ForAccount(account.Id);

        return isOperator ? PortalAccessResult.Operator() : PortalAccessResult.Denied(PortalErrorCodes.AccountNotProvisioned);
    }

    public async Task<PortalAccessResult> ResolveDealerAsync(int customerId, bool isOperator, CancellationToken cancellationToken)
    {
        var account = await _dealer.GetAccountByCustomerIdAsync(customerId, cancellationToken);
        if (account is { IsActive: true })
            return PortalAccessResult.ForAccount(account.Id);

        return isOperator ? PortalAccessResult.Operator() : PortalAccessResult.Denied(PortalErrorCodes.AccountNotProvisioned);
    }

    public async Task<bool> OwnsWorkshopAccountAsync(int customerId, int workshopAccountId, bool isOperator, CancellationToken cancellationToken)
    {
        if (isOperator)
            return true;

        var account = await _workshop.GetAccountByIdAsync(workshopAccountId, cancellationToken);
        return account is { IsActive: true, CustomerId: var owner } && owner == customerId;
    }

    public async Task<bool> OwnsFleetAccountAsync(int customerId, int fleetAccountId, bool isOperator, CancellationToken cancellationToken)
    {
        if (isOperator)
            return true;

        var account = await _fleet.GetAccountByIdAsync(fleetAccountId, cancellationToken);
        return account is { IsActive: true, CustomerId: var owner } && owner == customerId;
    }

    public async Task<bool> OwnsDealerAccountAsync(int customerId, int dealerAccountId, bool isOperator, CancellationToken cancellationToken)
    {
        if (isOperator)
            return true;

        var account = await _dealer.GetAccountByIdAsync(dealerAccountId, cancellationToken);
        return account is { IsActive: true, CustomerId: var owner } && owner == customerId;
    }
}

public sealed class PortalAccessResult
{
    public bool Allowed { get; init; }

    public bool IsOperator { get; init; }

    public int? AccountId { get; init; }

    public string? ErrorCode { get; init; }

    public static PortalAccessResult ForAccount(int accountId)
        => new() { Allowed = true, AccountId = accountId };

    public static PortalAccessResult Operator()
        => new() { Allowed = true, IsOperator = true };

    public static PortalAccessResult Denied(string errorCode)
        => new() { Allowed = false, ErrorCode = errorCode };
}

public static class PortalErrorCodes
{
    public const string AccessUnauthenticated = "portal.access_unauthenticated";

    public const string AccountNotProvisioned = "portal.account_not_provisioned";

    public const string AccessDenied = "portal.access_denied";
}
