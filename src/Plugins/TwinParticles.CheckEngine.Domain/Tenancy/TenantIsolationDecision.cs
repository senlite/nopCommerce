namespace TwinParticles.CheckEngine.Domain.Tenancy;

public sealed class TenantIsolationDecision
{
    public bool Allowed { get; init; }

    public string? ReasonCode { get; init; }

    public static TenantIsolationDecision Allow() => new() { Allowed = true };

    public static TenantIsolationDecision Deny(string reasonCode) => new() { Allowed = false, ReasonCode = reasonCode };
}
