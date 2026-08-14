namespace TwinParticles.CheckEngine.Domain.Security;

public sealed class AuditIntegrityResult
{
    public bool IsValid { get; init; }
    public int CheckedEntries { get; init; }
    public int InvalidEntries { get; init; }
}
