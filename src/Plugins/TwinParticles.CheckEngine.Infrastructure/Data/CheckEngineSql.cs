using System;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.MySql;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.SqlQuery;
using Nop.Data;
using Nop.Data.Configuration;
using Nop.Data.DataProviders.LinqToDB;

namespace TwinParticles.CheckEngine.Infrastructure.Data;

/// <summary>
/// Portable fragments for Check Engine raw SQL. Production Check Engine is documented against
/// SQL Server; this VM and AGENTS.md use MySQL, so TOP / SCOPE_IDENTITY / ISNULL must not appear
/// in shared statements.
/// </summary>
public static class CheckEngineSql
{
    public static bool IsMySql()
    {
        try
        {
            return DataSettingsManager.LoadSettings().DataProvider == DataProviderType.MySql;
        }
        catch
        {
            return false;
        }
    }

    public static string SelectInsertedIntId(string alias = "Value")
        => IsMySql()
            ? $"SELECT CAST(LAST_INSERT_ID() AS SIGNED) AS {alias};"
            : $"SELECT CAST(SCOPE_IDENTITY() as int) AS {alias};";

    public static string SelectInsertedLongId(string alias = "Value")
        => IsMySql()
            ? $"SELECT CAST(LAST_INSERT_ID() AS SIGNED) AS {alias};"
            : $"SELECT CAST(SCOPE_IDENTITY() AS bigint) AS {alias};";

    public static string SelectTop(int count, string columns, string fromWhereOrderBy)
    {
        if (count <= 0)
            count = 1;

        if (IsMySql())
            return $"SELECT {columns} {fromWhereOrderBy} LIMIT {count}";

        return $"SELECT TOP ({count}) {columns} {fromWhereOrderBy}";
    }

    public static string QuoteIdentifier(string identifier)
        => IsMySql() ? $"`{identifier}`" : $"[{identifier}]";

    public static string UtcNow()
        => IsMySql() ? "UTC_TIMESTAMP()" : "SYSUTCDATETIME()";

    public static string DateAddDays(string dateExpression, int days)
        => IsMySql()
            ? $"DATE_ADD({dateExpression}, INTERVAL {days} DAY)"
            : $"DATEADD(day, {days}, {dateExpression})";

    public static string ScalarSubqueryLimitOne(string selectExpression, string fromWhereOrderBy)
        => IsMySql()
            ? $"(SELECT {selectExpression} {fromWhereOrderBy} LIMIT 1)"
            : $"(SELECT TOP 1 {selectExpression} {fromWhereOrderBy})";

    public static string AcquireSessionLock(string resourceParameter = "@resource")
        => IsMySql()
            ? $"SELECT GET_LOCK({resourceParameter}, 10);"
            : $"EXEC sp_getapplock @Resource={resourceParameter}, @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=10000;";

    public static string ReleaseSessionLock(string resourceParameter = "@resource")
        => IsMySql()
            ? $"SELECT RELEASE_LOCK({resourceParameter});"
            : $"EXEC sp_releaseapplock @Resource={resourceParameter}, @LockOwner='Session';";

    /// <summary>
    /// Opens a dedicated LinqToDB connection and runs <paramref name="work"/> inside a real
    /// database transaction. The nopCommerce data provider opens a new connection per call, so
    /// issuing START TRANSACTION as a standalone command cannot wrap later inserts.
    /// </summary>
    public static async Task ExecuteInTransactionAsync(
        Func<DataConnection, Task> work,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);

        await using var connection = CreateDataConnection();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await work(connection);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public static Task<int> ExecuteAsync(
        DataConnection connection,
        string sql,
        params DataParameter[] parameters)
        => new CommandInfo(connection, sql, parameters).ExecuteAsync(cancellationToken: default);

    public static async Task<T> QueryScalarAsync<T>(
        DataConnection connection,
        string sql,
        params DataParameter[] parameters)
    {
        var rows = await new CommandInfo(connection, sql, parameters).QueryToListAsync<T>();
        return rows.Count > 0 ? rows[0] : default!;
    }

    private static DataConnection CreateDataConnection()
    {
        var settings = DataSettingsManager.LoadSettings();
        var connection = new DataConnection(ResolveLinqProvider(settings), settings.ConnectionString)
        {
            CommandTimeout = DataSettingsManager.GetSqlCommandTimeout()
        };

        if (settings.DataProvider == DataProviderType.MySql)
        {
            connection.MappingSchema.SetDataType(typeof(Guid), new SqlDataType(DataType.NChar, typeof(Guid), 36));
            connection.MappingSchema.SetConvertExpression<string, Guid>(strGuid => new Guid(strGuid));
        }

        return connection;
    }

    private static IDataProvider ResolveLinqProvider(DataConfig settings)
        => settings.DataProvider switch
        {
            DataProviderType.MySql => MySqlTools.GetDataProvider(),
            DataProviderType.SqlServer => SqlServerTools.GetDataProvider(
                SqlServerVersion.v2012,
                SqlServerProvider.MicrosoftDataSqlClient),
            DataProviderType.PostgreSQL => new LinqToDBPostgreSQLDataProvider(),
            _ => throw new InvalidOperationException($"Unsupported data provider '{settings.DataProvider}'.")
        };
}
