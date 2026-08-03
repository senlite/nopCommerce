using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LinqToDB.Data;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SqlVehicleAliasRepositoryIntegrationTests
{
    private SqliteConnection _connection = null!;
    private SqlVehicleAliasRepository _repository = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        await CreateSchemaAsync(_connection);

        var executor = new SqliteVehicleAliasSqlExecutor(_connection);
        _repository = new SqlVehicleAliasRepository(executor);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _connection.DisposeAsync();
    }

    [Test]
    public async Task UpsertAsync_Should_Insert_Then_Update_Using_Real_Provider()
    {
        await _repository.UpsertAsync(new VehicleAlias
        {
            NodeType = "model",
            NodeId = 42,
            Locale = "en",
            AliasText = "Civic",
            NormalizedAlias = "civic"
        }, CancellationToken.None);

        await _repository.UpsertAsync(new VehicleAlias
        {
            NodeType = "model",
            NodeId = 42,
            Locale = "en",
            AliasText = "Civic Updated",
            NormalizedAlias = "civic updated"
        }, CancellationToken.None);

        var results = await _repository.SearchAsync(new VehicleAliasSearchCriteria
        {
            Term = "civic",
            Locale = "en",
            Take = 10
        }, CancellationToken.None);

        results.Count.Should().Be(1);
        results[0].AliasText.Should().Be("Civic Updated");
    }

    [Test]
    public async Task SearchAsync_Should_Filter_By_Locale_And_Apply_Take_Using_Real_Provider()
    {
        await _repository.UpsertAsync(new VehicleAlias { NodeType = "model", NodeId = 1, Locale = "en", AliasText = "A", NormalizedAlias = "a" }, CancellationToken.None);
        await _repository.UpsertAsync(new VehicleAlias { NodeType = "model", NodeId = 2, Locale = "en", AliasText = "B", NormalizedAlias = "b" }, CancellationToken.None);
        await _repository.UpsertAsync(new VehicleAlias { NodeType = "model", NodeId = 3, Locale = "en", AliasText = "C", NormalizedAlias = "c" }, CancellationToken.None);
        await _repository.UpsertAsync(new VehicleAlias { NodeType = "model", NodeId = 4, Locale = "tr", AliasText = "A_TR", NormalizedAlias = "a_tr" }, CancellationToken.None);

        var results = await _repository.SearchAsync(new VehicleAliasSearchCriteria
        {
            Term = string.Empty,
            Locale = "en",
            Take = 2
        }, CancellationToken.None);

        results.Count.Should().Be(2);
        results.All(x => x.Locale == "en").Should().BeTrue();
    }

    [Test]
    public async Task UpsertAsync_Should_Handle_Concurrent_Insert_Race_With_Single_Row_Result()
    {
        var databaseName = $"tpce_race_{Guid.NewGuid():N}";
        var connectionString = $"Data Source={databaseName};Mode=Memory;Cache=Shared";

        await using var keepAlive = new SqliteConnection(connectionString);
        await keepAlive.OpenAsync();
        await CreateSchemaAsync(keepAlive);

        await using var connection1 = new SqliteConnection(connectionString);
        await using var connection2 = new SqliteConnection(connectionString);
        await connection1.OpenAsync();
        await connection2.OpenAsync();

        var raceGate = new ConcurrentUpsertRaceGate(2);
        var repository1 = new SqlVehicleAliasRepository(new SqliteVehicleAliasSqlExecutor(connection1, raceGate));
        var repository2 = new SqlVehicleAliasRepository(new SqliteVehicleAliasSqlExecutor(connection2, raceGate));

        var task1 = repository1.UpsertAsync(new VehicleAlias
        {
            NodeType = "model",
            NodeId = 555,
            Locale = "en",
            AliasText = "Concurrent A",
            NormalizedAlias = "concurrent-race"
        }, CancellationToken.None);

        var task2 = repository2.UpsertAsync(new VehicleAlias
        {
            NodeType = "model",
            NodeId = 555,
            Locale = "en",
            AliasText = "Concurrent B",
            NormalizedAlias = "concurrent-race"
        }, CancellationToken.None);

        await Task.WhenAll(task1, task2);

        var count = await CountRowsAsync(keepAlive, "model", 555, "en");
        count.Should().Be(1);

        var aliasText = await GetAliasTextAsync(keepAlive, "model", 555, "en");
        aliasText.Should().BeOneOf("Concurrent A", "Concurrent B");
    }

    private static async Task CreateSchemaAsync(SqliteConnection connection)
    {
        const string sql = @"CREATE TABLE TP_CE_VehicleAlias (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    NodeType TEXT NOT NULL,
    NodeId INTEGER NOT NULL,
    Locale TEXT NOT NULL,
    AliasText TEXT NOT NULL,
    NormalizedAlias TEXT NOT NULL
);
CREATE UNIQUE INDEX IX_TP_CE_VehicleAlias_NodeType_NormalizedAlias ON TP_CE_VehicleAlias (NodeType, NormalizedAlias);";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> CountRowsAsync(SqliteConnection connection, string nodeType, int nodeId, string locale)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM TP_CE_VehicleAlias WHERE NodeType = @nodeType AND NodeId = @nodeId AND Locale = @locale";
        command.Parameters.AddWithValue("@nodeType", nodeType);
        command.Parameters.AddWithValue("@nodeId", nodeId);
        command.Parameters.AddWithValue("@locale", locale);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static async Task<string?> GetAliasTextAsync(SqliteConnection connection, string nodeType, int nodeId, string locale)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT AliasText FROM TP_CE_VehicleAlias WHERE NodeType = @nodeType AND NodeId = @nodeId AND Locale = @locale LIMIT 1";
        command.Parameters.AddWithValue("@nodeType", nodeType);
        command.Parameters.AddWithValue("@nodeId", nodeId);
        command.Parameters.AddWithValue("@locale", locale);

        var result = await command.ExecuteScalarAsync();
        return result?.ToString();
    }

    private sealed class ConcurrentUpsertRaceGate
    {
        private readonly int _participants;
        private readonly object _lock = new();
        private int _arrived;
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ConcurrentUpsertRaceGate(int participants)
        {
            _participants = participants;
        }

        public Task SignalAndWaitAsync()
        {
            lock (_lock)
            {
                _arrived++;
                if (_arrived >= _participants)
                    _release.TrySetResult();
            }

            return _release.Task;
        }
    }

    private sealed class SqliteVehicleAliasSqlExecutor : IVehicleAliasSqlExecutor
    {
        private readonly SqliteConnection _connection;
        private readonly ConcurrentUpsertRaceGate? _raceGate;

        public SqliteVehicleAliasSqlExecutor(SqliteConnection connection, ConcurrentUpsertRaceGate? raceGate = null)
        {
            _connection = connection;
            _raceGate = raceGate;
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, params DataParameter[] parameters)
        {
            await using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameters(command, parameters);

            var affectedRows = await command.ExecuteNonQueryAsync();

            if (_raceGate is not null
                && affectedRows == 0
                && sql.Contains("UPDATE TP_CE_VehicleAlias", StringComparison.OrdinalIgnoreCase))
            {
                await _raceGate.SignalAndWaitAsync();
            }

            return affectedRows;
        }

        public async Task<IList<T>> QueryAsync<T>(string sql, params DataParameter[] parameters)
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
                var item = Activator.CreateInstance<T>();
                foreach (var property in properties)
                {
                    var ordinal = GetOrdinal(reader, property.Name);
                    if (ordinal < 0 || await reader.IsDBNullAsync(ordinal))
                        continue;

                    var value = reader.GetValue(ordinal);
                    property.SetValue(item, Convert.ChangeType(value, property.PropertyType));
                }

                items.Add(item);
            }

            return items;
        }

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
    }
}
