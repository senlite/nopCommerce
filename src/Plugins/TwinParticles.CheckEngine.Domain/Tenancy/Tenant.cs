using System;

namespace TwinParticles.CheckEngine.Domain.Tenancy;

/// <summary>
/// Control-plane tenant registry row. Tenant-plane <c>Ce*</c> tables stay unchanged in phase 5.1;
/// isolation is physical (connection routing) plus request-scoped context (FR-1310).
/// </summary>
public sealed class Tenant
{
    public int Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public TenantStatus Status { get; set; } = TenantStatus.Provisioning;

    public TenantIsolationMode IsolationMode { get; set; } = TenantIsolationMode.DatabasePerTenant;

    /// <summary>
    /// Named connection for database-per-tenant routing. Empty means the host default connection
    /// (self-hosted and the early hosted pilot share this shape).
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Comma-separated hostnames used to resolve tenant context from the request host.</summary>
    public string HostnamesCsv { get; set; } = string.Empty;

    public bool IsSelfHosted { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }
}
