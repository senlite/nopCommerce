using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Web.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Models;

namespace TwinParticles.CheckEngine.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class VendorController : BasePublicController
{
    private readonly VendorOnboardingService _onboardingService;
    private readonly MarketplaceLicenceGate _marketplaceGate;
    private readonly IMarketplaceOnboardingPolicy _policy;
    private readonly IWorkContext _workContext;
    private readonly ICustomerService _customerService;

    public VendorController(
        VendorOnboardingService onboardingService,
        MarketplaceLicenceGate marketplaceGate,
        IMarketplaceOnboardingPolicy policy,
        IWorkContext workContext,
        ICustomerService customerService)
    {
        _onboardingService = onboardingService;
        _marketplaceGate = marketplaceGate;
        _policy = policy;
        _workContext = workContext;
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> Apply(CancellationToken cancellationToken)
    {
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken) || !_policy.ApplicationsOpen)
            return NotFound();

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Vendor/Apply.cshtml");
    }

    [HttpPost]
    public async Task<IActionResult> Apply([FromBody] VendorApplyRequestModel model, CancellationToken cancellationToken)
    {
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        var customer = await _workContext.GetCurrentCustomerAsync();
        var applicantId = await _customerService.IsGuestAsync(customer) ? (int?)null : customer.Id;

        var result = await _onboardingService.ApplyAsync(new VendorApplication
        {
            LegalName = model.LegalName,
            TradingName = model.TradingName,
            ContactEmail = model.ContactEmail,
            ApplicantCustomerId = applicantId,
            TaxIdsJson = model.TaxIdsJson,
            CategoriesCsv = model.CategoriesCsv,
            BankingDetails = model.BankingDetails
        }, cancellationToken);

        if (!result.Succeeded || result.Snapshot is null)
            return Denied(result.ReasonCode ?? VendorErrorCodes.InvalidApplication, 400);

        var vendorId = result.Snapshot.Vendor.Id;
        if (model.AcceptAgreement)
            result = await _onboardingService.AcceptAgreementAsync(vendorId, model.ContactEmail, cancellationToken);

        if (result.Succeeded && model.SubmitForReview)
            result = await _onboardingService.SubmitForReviewAsync(vendorId, model.ContactEmail, cancellationToken);

        return result.Succeeded
            ? Json(result.Snapshot)
            : Denied(result.ReasonCode ?? VendorErrorCodes.IllegalTransition, 400);
    }

    [HttpPost]
    public async Task<IActionResult> AcceptAgreement(int vendorId, CancellationToken cancellationToken)
    {
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        var customer = await _workContext.GetCurrentCustomerAsync();
        var result = await _onboardingService.AcceptAgreementAsync(vendorId, customer.Email, cancellationToken);
        return result.Succeeded
            ? Json(result.Snapshot)
            : Denied(result.ReasonCode ?? VendorErrorCodes.IllegalTransition, 400);
    }

    [HttpGet]
    public async Task<IActionResult> Status(int vendorId, CancellationToken cancellationToken)
    {
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        var snapshot = await _onboardingService.GetSnapshotAsync(vendorId, cancellationToken);
        return snapshot is null ? NotFound() : Json(snapshot);
    }

    private static JsonResult Denied(string reasonCode, int statusCode)
        => new(new { reasonCode }) { StatusCode = statusCode };
}
