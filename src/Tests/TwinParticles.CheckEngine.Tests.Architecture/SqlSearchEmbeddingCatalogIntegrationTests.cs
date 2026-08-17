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
public class SqlSearchEmbeddingCatalogIntegrationTests
{
    private SqliteConnection _connection = null!;
    private SqliteCheckEngineDataProvider _dataProvider = null!;
    private SqlSearchEmbeddingCatalogSource _catalogSource = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        await CreateSchemaAsync(_connection);

        _dataProvider = new SqliteCheckEngineDataProvider(_connection);
        _catalogSource = new SqlSearchEmbeddingCatalogSource(_dataProvider, new BilingualSearchSynonymService());
    }

    [TearDown]
    public async Task TearDown()
    {
        await _connection.DisposeAsync();
    }

    [Test]
    public async Task GetStaleCountAsync_Should_Count_Missing_Embeddings()
    {
        await SeedProductAsync(1001, published: true);
        await SeedSearchIndexAsync(1001, DateTime.UtcNow);

        var stale = await _catalogSource.GetStaleCountAsync("en", null, CancellationToken.None);

        stale.Should().Be(1);
    }

    [Test]
    public async Task GetStaleCountAsync_Should_Count_Outdated_Keyword_Projection()
    {
        await SeedProductAsync(1001, published: true);
        var older = DateTime.UtcNow.AddHours(-2);
        var newer = DateTime.UtcNow;
        await SeedSearchIndexAsync(1001, newer);
        await SeedEmbeddingAsync(1001, "en", older, "hash-v1");

        var stale = await _catalogSource.GetStaleCountAsync("en", null, CancellationToken.None);

        stale.Should().Be(1);
    }

    [Test]
    public async Task GetStaleCountAsync_Should_Count_Model_Hash_Mismatch()
    {
        await SeedProductAsync(1001, published: true);
        var timestamp = DateTime.UtcNow;
        await SeedSearchIndexAsync(1001, timestamp);
        await SeedEmbeddingAsync(1001, "en", timestamp, "hash-old");

        var stale = await _catalogSource.GetStaleCountAsync(
            "en",
            new SearchEmbeddingStaleOptions { ExpectedModelHash = "hash-new" },
            CancellationToken.None);

        stale.Should().Be(1);
    }

    [Test]
    public async Task GetStaleCountAsync_Should_Return_Zero_When_Embedding_Is_Current()
    {
        await SeedProductAsync(1001, published: true);
        var timestamp = DateTime.UtcNow;
        await SeedSearchIndexAsync(1001, timestamp);
        await SeedEmbeddingAsync(1001, "en", timestamp, "hash-current");

        var stale = await _catalogSource.GetStaleCountAsync(
            "en",
            new SearchEmbeddingStaleOptions { ExpectedModelHash = "hash-current" },
            CancellationToken.None);

        stale.Should().Be(0);
    }

    private static async Task CreateSchemaAsync(SqliteConnection connection)
    {
        const string sql = """
            CREATE TABLE Product (
                Id INTEGER PRIMARY KEY,
                Deleted INTEGER NOT NULL,
                Published INTEGER NOT NULL,
                ManufacturerId INTEGER NULL
            );
            CREATE TABLE TP_CE_SearchIndex (
                ProductId INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                NormalizedText TEXT NOT NULL,
                Sku TEXT NULL,
                Mpn TEXT NULL,
                Price REAL NOT NULL,
                UpdatedUtc TEXT NOT NULL,
                IndexedUtc TEXT NOT NULL
            );
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

    private async Task SeedProductAsync(int productId, bool published)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = "INSERT INTO Product (Id, Deleted, Published, ManufacturerId) VALUES (@id, 0, @published, NULL)";
        command.Parameters.AddWithValue("@id", productId);
        command.Parameters.AddWithValue("@published", published ? 1 : 0);
        await command.ExecuteNonQueryAsync();
    }

    private async Task SeedSearchIndexAsync(int productId, DateTime updatedUtc)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = """
            INSERT INTO TP_CE_SearchIndex (ProductId, Name, NormalizedText, Sku, Mpn, Price, UpdatedUtc, IndexedUtc)
            VALUES (@productId, @name, @normalized, NULL, NULL, 10.0, @updated, @updated)
            """;
        command.Parameters.AddWithValue("@productId", productId);
        command.Parameters.AddWithValue("@name", "Oil Filter");
        command.Parameters.AddWithValue("@normalized", "oil filter");
        command.Parameters.AddWithValue("@updated", updatedUtc.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    private async Task SeedEmbeddingAsync(int productId, string locale, DateTime updatedUtc, string modelHash)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = """
            INSERT INTO TP_CE_SearchEmbedding
                (ProductId, Locale, Name, CategoryName, Brand, Price, EmbeddingJson, ModelHash, UpdatedUtc)
            VALUES (@productId, @locale, @name, NULL, NULL, 10.0, @embedding, @modelHash, @updated)
            """;
        command.Parameters.AddWithValue("@productId", productId);
        command.Parameters.AddWithValue("@locale", locale);
        command.Parameters.AddWithValue("@name", "Oil Filter");
        command.Parameters.AddWithValue("@embedding", "[0.1,0.2]");
        command.Parameters.AddWithValue("@modelHash", modelHash);
        command.Parameters.AddWithValue("@updated", updatedUtc.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }
}
