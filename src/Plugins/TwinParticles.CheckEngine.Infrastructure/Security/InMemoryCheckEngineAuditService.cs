using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Infrastructure.Security;

public sealed class InMemoryCheckEngineAuditService : ICheckEngineAuditService
{
    private readonly ConcurrentQueue<AuditEntry> _entries = new();

    public Task AppendAsync(
        string actor,
        string action,
        string entityType,
        string entityId,
        string? beforeJson,
        string? afterJson,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _entries.Enqueue(new AuditEntry(
            actor ?? string.Empty,
            action ?? string.Empty,
            entityType ?? string.Empty,
            entityId ?? string.Empty,
            beforeJson,
            afterJson));

        return Task.CompletedTask;
    }

    public IReadOnlyList<AuditEntry> Snapshot() => _entries.ToArray();

    public sealed record AuditEntry(
        string Actor,
        string Action,
        string EntityType,
        string EntityId,
        string? BeforeJson,
        string? AfterJson);
}
