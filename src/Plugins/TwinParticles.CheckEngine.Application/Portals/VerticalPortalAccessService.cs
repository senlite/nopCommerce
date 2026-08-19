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
        var account = await _workshop.ResolveAccountForPortalUserAsync(customerId, cancellationToken);
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
        if (account is { IsActive: true, CustomerId: var owner } && owner == customerId)
            return true;

        var technician = await _workshop.GetTechnicianAsync(workshopAccountId, customerId, cancellationToken);
        return technician is not null;
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

    public async Task<bool> CanApproveFleetAsync(int customerId, int fleetAccountId, bool isOperator, CancellationToken cancellationToken)
    {
        if (isOperator)
            return true;

        var account = await _fleet.GetAccountByIdAsync(fleetAccountId, cancellationToken);
        if (account is not { IsActive: true })
            return false;

        if (account.CustomerId == customerId)
            return true;

        var member = await _fleet.GetMemberAsync(fleetAccountId, customerId, cancellationToken);
        return member is { CanApprove: true };
    }

    public async Task<bool> CanRaiseWorkshopInvoiceAsync(int customerId, int workshopAccountId, bool isOperator, CancellationToken cancellationToken)
    {
        if (isOperator)
            return true;

        var account = await _workshop.GetAccountByIdAsync(workshopAccountId, cancellationToken);
        if (account is not { IsActive: true })
            return false;

        if (account.CustomerId == customerId)
            return true;

        var technician = await _workshop.GetTechnicianAsync(workshopAccountId, customerId, cancellationToken);
        return technician is { CanRaiseInvoice: true };
    }

    public async Task<bool> CanAssignWorkshopTechnicianAsync(int customerId, int workshopAccountId, bool isOperator, CancellationToken cancellationToken)
    {
        if (isOperator)
            return true;

        var account = await _workshop.GetAccountByIdAsync(workshopAccountId, cancellationToken);
        if (account is not { IsActive: true })
            return false;

        if (account.CustomerId == customerId)
            return true;

        var technician = await _workshop.GetTechnicianAsync(workshopAccountId, customerId, cancellationToken);
        return technician is { IsFrontDesk: true };
    }

    public async Task<bool> CanViewWorkshopCreditAsync(int customerId, int workshopAccountId, bool isOperator, CancellationToken cancellationToken)
    {
        if (isOperator)
            return true;

        var account = await _workshop.GetAccountByIdAsync(workshopAccountId, cancellationToken);
        return account is { IsActive: true, CustomerId: var owner } && owner == customerId;
    }

    public async Task<bool> CanExportWorkshopCustomerAsync(int customerId, int workshopAccountId, bool isOperator, CancellationToken cancellationToken)
    {
        if (isOperator)
            return true;

        var account = await _workshop.GetAccountByIdAsync(workshopAccountId, cancellationToken);
        if (account is not { IsActive: true })
            return false;

        if (account.CustomerId == customerId)
            return true;

        var technician = await _workshop.GetTechnicianAsync(workshopAccountId, customerId, cancellationToken);
        return technician is { IsFrontDesk: true };
    }

    public async Task<bool> CanModifyWorkshopJobAsync(
        int customerId,
        WorkshopJob job,
        bool isOperator,
        CancellationToken cancellationToken)
    {
        if (isOperator)
            return true;

        var account = await _workshop.GetAccountByIdAsync(job.WorkshopAccountId, cancellationToken);
        if (account is not { IsActive: true })
            return false;

        if (account.CustomerId == customerId)
            return true;

        var technician = await _workshop.GetTechnicianAsync(job.WorkshopAccountId, customerId, cancellationToken);
        if (technician is { IsFrontDesk: true })
            return true;

        return technician is not null
               && job.AssignedTechnicianCustomerId == customerId;
    }

    public async Task<bool> CanCreateWorkshopJobAsync(int customerId, int workshopAccountId, bool isOperator, CancellationToken cancellationToken)
    {
        if (isOperator)
            return true;

        var account = await _workshop.GetAccountByIdAsync(workshopAccountId, cancellationToken);
        if (account is not { IsActive: true })
            return false;

        if (account.CustomerId == customerId)
            return true;

        var technician = await _workshop.GetTechnicianAsync(workshopAccountId, customerId, cancellationToken);
        return technician is not null;
    }

    public async Task<int?> ResolveTechnicianJobFilterAsync(
        int customerId,
        int workshopAccountId,
        bool isOperator,
        CancellationToken cancellationToken)
    {
        if (isOperator)
            return null;

        var account = await _workshop.GetAccountByIdAsync(workshopAccountId, cancellationToken);
        if (account is not { IsActive: true })
            return null;

        if (account.CustomerId == customerId)
            return null;

        var technician = await _workshop.GetTechnicianAsync(workshopAccountId, customerId, cancellationToken);
        if (technician is null || technician.IsFrontDesk)
            return null;

        return customerId;
    }

    public async Task<WorkshopPortalCapabilities> ResolveWorkshopCapabilitiesAsync(
        int customerId,
        int workshopAccountId,
        bool isOperator,
        CancellationToken cancellationToken)
    {
        var account = await _workshop.GetAccountByIdAsync(workshopAccountId, cancellationToken);
        var technician = await _workshop.GetTechnicianAsync(workshopAccountId, customerId, cancellationToken);
        var isOwner = account is { IsActive: true, CustomerId: var owner } && owner == customerId;
        var isTechnicianOnly = technician is not null && !isOwner && !technician.IsFrontDesk;

        return new WorkshopPortalCapabilities
        {
            CanViewCredit = await CanViewWorkshopCreditAsync(customerId, workshopAccountId, isOperator, cancellationToken),
            CanRaiseInvoice = await CanRaiseWorkshopInvoiceAsync(customerId, workshopAccountId, isOperator, cancellationToken),
            CanAssignTechnician = await CanAssignWorkshopTechnicianAsync(customerId, workshopAccountId, isOperator, cancellationToken),
            IsFrontDesk = technician?.IsFrontDesk ?? false,
            IsTechnicianOnly = isTechnicianOnly,
            CanExportCustomer = await CanExportWorkshopCustomerAsync(customerId, workshopAccountId, isOperator, cancellationToken)
        };
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
