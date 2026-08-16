namespace TwinParticles.CheckEngine.Domain.Marketplace;

public static class VendorErrorCodes
{
    public const string LicenceDenied = "marketplace.licence_denied";
    public const string ApplicationsClosed = "marketplace.applications_closed";
    public const string InvalidApplication = "vendor.invalid_application";
    public const string NotFound = "vendor.not_found";
    public const string IllegalTransition = "vendor.illegal_transition";
    public const string AgreementRequired = "vendor.agreement_required";
    public const string AgreementStale = "vendor.agreement_stale";
}

public static class VendorAgreementVersions
{
    public const string Default = "2026-08-16";
}
