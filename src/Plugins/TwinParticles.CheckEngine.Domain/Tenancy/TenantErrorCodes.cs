namespace TwinParticles.CheckEngine.Domain.Tenancy;

public static class TenantErrorCodes
{
    public const string IsolationDenied = "tenant.isolation.denied";
    public const string IsolationUnauthenticated = "tenant.isolation.unauthenticated";
    public const string TenantNotFound = "tenant.not_found";
    public const string TenantSuspended = "tenant.suspended";
    public const string TenantClosed = "tenant.closed";
    public const string CrossConnectionDenied = "tenant.connection.denied";
    public const string InvalidSlug = "tenant.invalid_slug";
    public const string DuplicateSlug = "tenant.duplicate_slug";
    public const string PublicApiUnauthenticated = "public_api.unauthenticated";
    public const string PublicApiKeyInvalid = "public_api.key_invalid";
    public const string PublicApiScopeDenied = "public_api.scope_denied";
    public const string PublicApiRateLimited = "public_api.rate_limited";
    public const string UsageLimitExceeded = "tenant.usage.limit_exceeded";
    public const string WebhookInvalidUrl = "webhook.invalid_url";
}
