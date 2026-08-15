using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Infrastructure.Ai;
using TwinParticles.CheckEngine.Infrastructure.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SearchEmbeddingIndexBuilderServiceTests
{
    [Test]
    public async Task RebuildAsync_Should_Report_Skipped_And_Indexed_Counts()
    {
        var index = new InMemorySearchEmbeddingIndex();
        var builder = new SearchEmbeddingIndexBuilderService(
            new InMemorySearchEmbeddingCatalogSource(),
            index,
            new EmptyVectorPort());

        var result = await builder.RebuildAsync("en", CancellationToken.None);

        result.CatalogCount.Should().BeGreaterThan(0);
        result.Indexed.Should().Be(0);
        result.Skipped.Should().Be(result.CatalogCount);
        result.Failed.Should().Be(0);
    }

    [Test]
    public async Task RebuildAsync_Should_Index_Documents_When_Embeddings_Succeed()
    {
        var index = new InMemorySearchEmbeddingIndex();
        var builder = new SearchEmbeddingIndexBuilderService(
            new InMemorySearchEmbeddingCatalogSource(),
            index,
            new DeterministicTextEmbeddingPort());

        var result = await builder.RebuildAsync("en", CancellationToken.None);

        result.Indexed.Should().BeGreaterThan(0);
        (await index.GetCountAsync("en", CancellationToken.None)).Should().Be(result.Indexed);
    }

    [Test]
    public async Task RefreshIncrementalAsync_Should_Reindex_Stale_Documents_Without_Clearing()
    {
        var index = new InMemorySearchEmbeddingIndex();
        var builder = new SearchEmbeddingIndexBuilderService(
            new InMemorySearchEmbeddingCatalogSource(),
            index,
            new DeterministicTextEmbeddingPort());

        await builder.RebuildAsync("en", CancellationToken.None);
        var before = await index.GetCountAsync("en", CancellationToken.None);

        var result = await builder.RefreshIncrementalAsync("en", CancellationToken.None);

        result.Indexed.Should().BeGreaterThan(0);
        (await index.GetCountAsync("en", CancellationToken.None)).Should().Be(before);
    }

    private sealed class EmptyVectorPort : IAiEmbeddingPort
    {
        public Task<AiEmbeddingResult> EmbedAsync(AiEmbeddingRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AiEmbeddingResult
            {
                Success = true,
                Vector = [],
                ProviderName = "test",
                ModelHash = "empty"
            });
        }
    }
}
