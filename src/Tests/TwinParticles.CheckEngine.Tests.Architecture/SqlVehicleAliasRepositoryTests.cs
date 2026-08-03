using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LinqToDB.Data;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SqlVehicleAliasRepositoryTests
{
    [Test]
    public async Task UpsertAsync_Should_Only_Run_Update_When_Row_Exists()
    {
        var executor = new FakeVehicleAliasSqlExecutor
        {
            NextExecuteResults = new Queue<int>(new[] { 1 })
        };

        var repository = new SqlVehicleAliasRepository(executor);

        await repository.UpsertAsync(new VehicleAlias
        {
            NodeType = "model",
            NodeId = 5,
            Locale = "en",
            AliasText = "Civic",
            NormalizedAlias = "civic"
        }, CancellationToken.None);

        executor.ExecuteCommands.Count.Should().Be(1);
        executor.ExecuteCommands[0].Should().Contain("UPDATE TP_CE_VehicleAlias");
    }

    [Test]
    public async Task UpsertAsync_Should_Run_Insert_When_Update_Affects_No_Rows()
    {
        var executor = new FakeVehicleAliasSqlExecutor
        {
            NextExecuteResults = new Queue<int>(new[] { 0, 1 })
        };

        var repository = new SqlVehicleAliasRepository(executor);

        await repository.UpsertAsync(new VehicleAlias
        {
            NodeType = "model",
            NodeId = 7,
            Locale = "en",
            AliasText = "Corolla",
            NormalizedAlias = "corolla"
        }, CancellationToken.None);

        executor.ExecuteCommands.Count.Should().Be(2);
        executor.ExecuteCommands[0].Should().Contain("UPDATE TP_CE_VehicleAlias");
        executor.ExecuteCommands[1].Should().Contain("INSERT INTO TP_CE_VehicleAlias");
    }

    [Test]
    public async Task UpsertAsync_Should_Retry_Update_When_Insert_Hits_Unique_Constraint()
    {
        var executor = new FakeVehicleAliasSqlExecutor
        {
            NextExecuteResults = new Queue<int>(new[] { 0 }),
            ThrowOnInsert = new InvalidOperationException("duplicate key value violates unique constraint")
        };

        var repository = new SqlVehicleAliasRepository(executor);

        await repository.UpsertAsync(new VehicleAlias
        {
            NodeType = "model",
            NodeId = 9,
            Locale = "en",
            AliasText = "Focus",
            NormalizedAlias = "focus"
        }, CancellationToken.None);

        executor.ExecuteCommands.Count.Should().Be(3);
        executor.ExecuteCommands[0].Should().Contain("UPDATE TP_CE_VehicleAlias");
        executor.ExecuteCommands[1].Should().Contain("INSERT INTO TP_CE_VehicleAlias");
        executor.ExecuteCommands[2].Should().Contain("UPDATE TP_CE_VehicleAlias");
    }

    [Test]
    public async Task SearchAsync_Should_Apply_Take_And_Map_Results()
    {
        var executor = new FakeVehicleAliasSqlExecutor();
        executor.QueryRows = new List<SqlVehicleAliasRepositoryTestsRow>
        {
            new() { NodeType = "model", NodeId = 1, Locale = "en", AliasText = "A" },
            new() { NodeType = "model", NodeId = 2, Locale = "en", AliasText = "B" },
            new() { NodeType = "model", NodeId = 3, Locale = "en", AliasText = "C" }
        };

        var repository = new SqlVehicleAliasRepository(executor);

        var result = await repository.SearchAsync(new VehicleAliasSearchCriteria
        {
            Term = "a",
            Locale = "en",
            Take = 2
        }, CancellationToken.None);

        result.Count.Should().Be(2);
        result.Select(x => x.NodeId).Should().Equal(1, 2);
        executor.QueryCommands.Count.Should().Be(1);
        executor.QueryCommands[0].Should().Contain("SELECT NodeType, NodeId, Locale, AliasText");
    }

    private sealed class FakeVehicleAliasSqlExecutor : IVehicleAliasSqlExecutor
    {
        public Queue<int> NextExecuteResults { get; set; } = new();

        public List<string> ExecuteCommands { get; } = new();

        public List<string> QueryCommands { get; } = new();

        public List<SqlVehicleAliasRepositoryTestsRow> QueryRows { get; set; } = new();

        public Exception? ThrowOnInsert { get; set; }

        public Task<int> ExecuteNonQueryAsync(string sql, params DataParameter[] parameters)
        {
            ExecuteCommands.Add(sql);

            if (ThrowOnInsert is not null && sql.Contains("INSERT INTO TP_CE_VehicleAlias", StringComparison.Ordinal))
            {
                var exception = ThrowOnInsert;
                ThrowOnInsert = null;
                throw exception;
            }

            var value = NextExecuteResults.Count > 0 ? NextExecuteResults.Dequeue() : 1;
            return Task.FromResult(value);
        }

        public Task<IList<T>> QueryAsync<T>(string sql, params DataParameter[] parameters)
        {
            QueryCommands.Add(sql);

            if (typeof(T).Name.Contains("VehicleAliasSearchRow", StringComparison.Ordinal))
            {
                var projected = QueryRows
                    .Select(x =>
                    {
                        var item = Activator.CreateInstance(typeof(T))!;
                        typeof(T).GetProperty("NodeType")!.SetValue(item, x.NodeType);
                        typeof(T).GetProperty("NodeId")!.SetValue(item, x.NodeId);
                        typeof(T).GetProperty("Locale")!.SetValue(item, x.Locale);
                        typeof(T).GetProperty("AliasText")!.SetValue(item, x.AliasText);
                        return (T)item;
                    })
                    .ToList();

                return Task.FromResult<IList<T>>(projected);
            }

            return Task.FromResult<IList<T>>(new List<T>());
        }
    }

    private sealed class SqlVehicleAliasRepositoryTestsRow
    {
        public string NodeType { get; set; } = string.Empty;

        public int NodeId { get; set; }

        public string Locale { get; set; } = string.Empty;

        public string AliasText { get; set; } = string.Empty;
    }
}
