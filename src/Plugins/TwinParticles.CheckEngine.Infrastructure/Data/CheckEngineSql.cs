using Nop.Data;

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

    public static string UtcNow()
        => IsMySql() ? "UTC_TIMESTAMP()" : "SYSUTCDATETIME()";

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
}
