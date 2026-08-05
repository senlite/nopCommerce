using System;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Infrastructure.Security;

public sealed class SqlCheckEngineAuditService : ICheckEngineAuditService
{
    private readonly INopDataProvider _dataProvider;

    public SqlCheckEngineAuditService(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

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

        return _dataProvider.ExecuteNonQueryAsync(
            @"INSERT INTO TP_CE_AuditEvent (Actor, Action, EntityType, EntityId, BeforeJson, AfterJson, CreatedUtc)
VALUES (@actor, @action, @entityType, @entityId, @beforeJson, @afterJson, @createdUtc)",
            new DataParameter("actor", actor ?? string.Empty),
            new DataParameter("action", action ?? string.Empty),
            new DataParameter("entityType", entityType ?? string.Empty),
            new DataParameter("entityId", entityId ?? string.Empty),
            new DataParameter("beforeJson", (object?)beforeJson ?? DBNull.Value),
            new DataParameter("afterJson", (object?)afterJson ?? DBNull.Value),
            new DataParameter("createdUtc", DateTime.UtcNow));
    }
}
