using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using LinqToDB.Data;
using Microsoft.Data.Sqlite;
using Nop.Core;
using Nop.Data;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// Minimal SQLite-backed <see cref="INopDataProvider"/> for Check Engine SQL integration tests.
/// </summary>
internal sealed class SqliteCheckEngineDataProvider : INopDataProvider
{
    private readonly SqliteConnection _connection;

    public SqliteCheckEngineDataProvider(SqliteConnection connection)
    {
        _connection = connection;
    }

    public string ConfigurationName => "SQLite";

    public int SupportedLengthOfBinaryHash => 0;

    public bool BackupSupported => false;

    public Task<IList<T>> QueryAsync<T>(string sql, params DataParameter[] parameters)
    {
        sql = AdaptSqlForSqlite(sql);
        return QueryInternalAsync<T>(sql, parameters);
    }

    public Task<int> ExecuteNonQueryAsync(string sql, params DataParameter[] parameters)
    {
        sql = AdaptSqlForSqlite(sql);
        return ExecuteInternalAsync(sql, parameters);
    }

    private async Task<IList<T>> QueryInternalAsync<T>(string sql, DataParameter[] parameters)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);

        await using var reader = await command.ExecuteReaderAsync();
        var items = new List<T>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanWrite)
            .ToArray();

        while (await reader.ReadAsync())
        {
            if (typeof(T) == typeof(int) && reader.FieldCount == 1)
            {
                items.Add((T)Convert.ChangeType(reader.GetValue(0), typeof(T)));
                continue;
            }

            var item = Activator.CreateInstance<T>();
            foreach (var property in properties)
            {
                var ordinal = GetOrdinal(reader, property.Name);
                if (ordinal < 0 || await reader.IsDBNullAsync(ordinal))
                    continue;

                var value = reader.GetValue(ordinal);
                property.SetValue(item, Convert.ChangeType(value, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType));
            }

            items.Add(item);
        }

        return items;
    }

    private async Task<int> ExecuteInternalAsync(string sql, DataParameter[] parameters)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        return await command.ExecuteNonQueryAsync();
    }

    private static string AdaptSqlForSqlite(string sql)
        => sql.Replace(
            "OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY",
            "LIMIT @pageSize OFFSET @offset",
            StringComparison.OrdinalIgnoreCase);

    private static void AddParameters(DbCommand command, IEnumerable<DataParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = "@" + parameter.Name;
            dbParameter.Value = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(dbParameter);
        }
    }

    private static int GetOrdinal(DbDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    public void CreateDatabase(int triesToConnect = 10) => throw new NotImplementedException();

    public Task<ITempDataStorage<TItem>> CreateTempDataStorageAsync<TItem>(string storeKey, IQueryable<TItem> query) where TItem : class
        => throw new NotImplementedException();

    public void InitializeDatabase() => throw new NotImplementedException();

    public Task<TEntity> InsertEntityAsync<TEntity>(TEntity entity) where TEntity : BaseEntity => throw new NotImplementedException();

    public TEntity InsertEntity<TEntity>(TEntity entity) where TEntity : BaseEntity => throw new NotImplementedException();

    public Task UpdateEntityAsync<TEntity>(TEntity entity) where TEntity : BaseEntity => throw new NotImplementedException();

    public void UpdateEntity<TEntity>(TEntity entity) where TEntity : BaseEntity => throw new NotImplementedException();

    public Task UpdateEntitiesAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity => throw new NotImplementedException();

    public void UpdateEntities<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity => throw new NotImplementedException();

    public Task DeleteEntityAsync<TEntity>(TEntity entity) where TEntity : BaseEntity => throw new NotImplementedException();

    public void DeleteEntity<TEntity>(TEntity entity) where TEntity : BaseEntity => throw new NotImplementedException();

    public Task BulkDeleteEntitiesAsync<TEntity>(IList<TEntity> entities) where TEntity : BaseEntity => throw new NotImplementedException();

    public void BulkDeleteEntities<TEntity>(IList<TEntity> entities) where TEntity : BaseEntity => throw new NotImplementedException();

    public Task<int> BulkDeleteEntitiesAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : BaseEntity => throw new NotImplementedException();

    public int BulkDeleteEntities<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : BaseEntity => throw new NotImplementedException();

    public Task BulkInsertEntitiesAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity => throw new NotImplementedException();

    public void BulkInsertEntities<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity => throw new NotImplementedException();

    public string CreateForeignKeyName(string foreignTable, string foreignColumn, string primaryTable, string primaryColumn) => throw new NotImplementedException();

    public string GetIndexName(string targetTable, string targetColumn) => throw new NotImplementedException();

    public IQueryable<TEntity> GetTable<TEntity>() where TEntity : BaseEntity => throw new NotImplementedException();

    public Task<int?> GetTableIdentAsync<TEntity>() where TEntity : BaseEntity => throw new NotImplementedException();

    public Task<bool> DatabaseExistsAsync() => Task.FromResult(true);

    public bool DatabaseExists() => true;

    public Task BackupDatabaseAsync(string fileName) => throw new NotImplementedException();

    public Task RestoreDatabaseAsync(string backupFileName) => throw new NotImplementedException();

    public Task ReIndexTablesAsync() => throw new NotImplementedException();

    public Task ShrinkDatabaseAsync() => throw new NotImplementedException();

    public string BuildConnectionString(INopConnectionStringInfo nopConnectionString) => throw new NotImplementedException();

    public Task SetTableIdentAsync<TEntity>(int ident) where TEntity : BaseEntity => throw new NotImplementedException();

    public Task<IDictionary<int, string>> GetFieldHashesAsync<TEntity>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, int>> keySelector,
        Expression<Func<TEntity, object>> fieldSelector) where TEntity : BaseEntity
        => throw new NotImplementedException();

    public Task<IList<T>> QueryProcAsync<T>(string procedureName, params DataParameter[] parameters) => throw new NotImplementedException();

    public Task TruncateAsync<TEntity>(bool resetIdentity = false) where TEntity : BaseEntity => throw new NotImplementedException();

    public Task<string> GetDataBaseCollationAsync() => Task.FromResult("NOCASE");
}
