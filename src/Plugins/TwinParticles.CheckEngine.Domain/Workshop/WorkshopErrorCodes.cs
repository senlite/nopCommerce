namespace TwinParticles.CheckEngine.Domain.Workshop;

public static class WorkshopErrorCodes
{
    public const string LicenceDenied = "workshop.licence_denied";
    public const string NotFound = "workshop.not_found";
    public const string InvalidTransition = "workshop.invalid_transition";
    public const string FitmentBlocked = "workshop.fitment_blocked";
    public const string CreditExceeded = "workshop.credit_exceeded";
    public const string EmptyJob = "workshop.empty_job";
    public const string AlreadyInvoiced = "workshop.already_invoiced";
    public const string InvoiceDenied = "workshop.invoice_denied";
}
