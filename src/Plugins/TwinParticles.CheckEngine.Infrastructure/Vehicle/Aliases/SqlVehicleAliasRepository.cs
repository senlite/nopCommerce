using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases;

public sealed class SqlVehicleAliasRepository : IVehicleAliasWriteRepository, IVehicleAliasReadRepository
{
    private readonly IVehicleAliasSqlExecutor _sqlExecutor;

    public SqlVehicleAliasRepository(IVehicleAliasSqlExecutor sqlExecutor)
    {
        _sqlExecutor = sqlExecutor;
    }

    public async Task UpsertAsync(VehicleAlias alias, CancellationToken cancellationToken)
    {
        const string updateSql = @"UPDATE TP_CE_VehicleAlias
SET AliasText = @aliasText,
    NormalizedAlias = @normalizedAlias
WHERE NodeType = @nodeType
  AND NodeId = @nodeId
  AND Locale = @locale";

        var updateCount = await _sqlExecutor.ExecuteNonQueryAsync(
            updateSql,
            new DataParameter("aliasText", alias.AliasText),
            new DataParameter("normalizedAlias", alias.NormalizedAlias),
            new DataParameter("nodeType", alias.NodeType),
            new DataParameter("nodeId", alias.NodeId),
            new DataParameter("locale", alias.Locale));

        if (updateCount > 0)
            return;

        const string insertSql = @"INSERT INTO TP_CE_VehicleAlias (NodeType, NodeId, Locale, AliasText, NormalizedAlias)
VALUES (@nodeType, @nodeId, @locale, @aliasText, @normalizedAlias)";

        try
        {
            await _sqlExecutor.ExecuteNonQueryAsync(
                insertSql,
                new DataParameter("nodeType", alias.NodeType),
                new DataParameter("nodeId", alias.NodeId),
                new DataParameter("locale", alias.Locale),
                new DataParameter("aliasText", alias.AliasText),
                new DataParameter("normalizedAlias", alias.NormalizedAlias));
        }
        catch (Exception exception) when (IsUniqueConstraintViolation(exception))
        {
            await _sqlExecutor.ExecuteNonQueryAsync(
                updateSql,
                new DataParameter("aliasText", alias.AliasText),
                new DataParameter("normalizedAlias", alias.NormalizedAlias),
                new DataParameter("nodeType", alias.NodeType),
                new DataParameter("nodeId", alias.NodeId),
                new DataParameter("locale", alias.Locale));
        }
    }

    public async Task<IReadOnlyList<VehicleAliasSearchItem>> SearchAsync(VehicleAliasSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var normalizedTerm = criteria.Term.Trim().ToLowerInvariant();
        var likeTerm = $"%{normalizedTerm}%";

        const string searchSql = @"SELECT NodeType, NodeId, Locale, AliasText
FROM TP_CE_VehicleAlias
WHERE Locale = @locale
  AND (LOWER(AliasText) LIKE @term OR NormalizedAlias LIKE @term)
ORDER BY AliasText";

        var rows = await _sqlExecutor.QueryAsync<VehicleAliasSearchRow>(
            searchSql,
            new DataParameter("locale", criteria.Locale),
            new DataParameter("term", likeTerm));

        return rows
            .Take(criteria.Take)
            .Select(x => new VehicleAliasSearchItem
            {
                NodeType = x.NodeType,
                NodeId = x.NodeId,
                Locale = x.Locale,
                AliasText = x.AliasText
            })
            .ToList();
    }

    private static bool IsUniqueConstraintViolation(Exception exception)
    {
        var message = exception.Message;
        if (string.IsNullOrWhiteSpace(message))
            return false;

        return message.Contains("unique", StringComparison.OrdinalIgnoreCase)
               || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
               || message.Contains("constraint", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class VehicleAliasSearchRow
    {
        public string NodeType { get; set; } = string.Empty;

        public int NodeId { get; set; }

        public string Locale { get; set; } = string.Empty;

        public string AliasText { get; set; } = string.Empty;
    }
}
