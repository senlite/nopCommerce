using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Infrastructure.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SqlSearchEmbeddingIndexIntegrationTests
{
    private SqliteConnection _connection = null!;
    private SqliteCheckEngineDataProvider _dataProvider = null!;
    private SqlSearchEmbeddingIndex _index = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        await CreateSchemaAsync(_connection);

        _dataProvider = new SqliteCheckEngineDataProvider(_connection);
        _index = new SqlSearchEmbeddingIndex(_dataProvider);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _connection.DisposeAsync();
    }

    [Test]
    public async Task UpsertAsync_Should_Insert_Then_Update_Model_Hash()
    {
        var document = new SearchEmbeddingDocument
        {
            ProductId = 1001,
            Locale = "en",
            Name = "Oil Filter",
            Text = "oil filter"
        };

        await _index.UpsertAsync(document, [0.1f, 0.9f], "hash-v1", CancellationToken.None);
        (await _index.GetCountAsync("en", CancellationToken.None)).Should().Be(1);

        await _index.UpsertAsync(document, [0.2f, 0.8f], "hash-v2", CancellationToken.None);
        (await _index.GetCountAsync("en", CancellationToken.None)).Should().Be(1);

        var hash = await ReadModelHashAsync(1001, "en");
        hash.Should().Be("hash-v2");
    }

    [Test]
    public async Task SearchSimilarAsync_Should_Return_Ranked_Hits()
    {
        await _index.UpsertAsync(new SearchEmbeddingDocument
        {
            ProductId = 1001,
            Locale = "en",
            Name = "Oil Filter"
        }, [1f, 0f], "hash", CancellationToken.None);

        await _index.UpsertAsync(new SearchEmbeddingDocument
        {
            ProductId = 1002,
            Locale = "en",
            Name = "Radiator Hose"
        }, [0f, 1f], "hash", CancellationToken.None);

        var hits = await _index.SearchSimilarAsync([0.95f, 0.05f], "en", 2, CancellationToken.None);

        hits.Should().HaveCount(2);
        hits[0].ProductId.Should().Be(1001);
    }

    private static async Task CreateSchemaAsync(SqliteConnection connection)
    {
        const string sql = """
            CREATE TABLE TP_CE_SearchEmbedding (
                ProductId INTEGER NOT NULL,
                Locale TEXT NOT NULL,
                Name TEXT NOT NULL,
                CategoryName TEXT NULL,
                Brand TEXT NULL,
                Price REAL NULL,
                EmbeddingJson TEXT NOT NULL,
                ModelHash TEXT NOT NULL,
                UpdatedUtc TEXT NOT NULL,
                PRIMARY KEY (ProductId, Locale)
            );
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string?> ReadModelHashAsync(int productId, string locale)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT ModelHash FROM TP_CE_SearchEmbedding WHERE ProductId = @productId AND Locale = @locale";
        command.Parameters.AddWithValue("@productId", productId);
        command.Parameters.AddWithValue("@locale", locale);
        return (await command.ExecuteScalarAsync())?.ToString();
    }
}
