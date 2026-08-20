namespace TwinParticles.CheckEngine.Domain.Tenancy;

/// <summary>
/// Phase 5.1 uses database-per-tenant. Schema-per-tenant is reserved for a later cost-driven evolution
/// and is not selected by default ([49 SaaS Roadmap] phased recommendation).
/// </summary>
public enum TenantIsolationMode
{
    DatabasePerTenant = 0,
    SchemaPerTenant = 1
}
