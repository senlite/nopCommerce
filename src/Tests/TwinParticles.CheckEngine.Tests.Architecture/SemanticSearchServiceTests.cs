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
public class SemanticSearchServiceTests
{
    [Test]
    public async Task SearchAsync_Should_Return_Similar_Products_When_Index_Is_Ready()
    {
        var index = new InMemorySearchEmbeddingIndex();
        var builder = new SearchEmbeddingIndexBuilderService(
            new InMemorySearchEmbeddingCatalogSource(),
            index,
            new DeterministicTextEmbeddingPort());

        await builder.RebuildAsync("en", CancellationToken.None);

        var service = new SemanticSearchService(index, new DeterministicTextEmbeddingPort(), new AlwaysOnToggle());
        var hits = await service.SearchAsync(new SearchQuery
        {
            RawText = "radiator cooling hose",
            Mode = SearchMode.Semantic,
            Locale = "en",
            Page = 1,
            PageSize = 5
        }, CancellationToken.None);

        hits.Should().NotBeEmpty();
        hits[0].ProductId.Should().Be(1002);
    }

    [Test]
    public async Task SearchAsync_Should_Return_Empty_When_Feature_Disabled()
    {
        var index = new InMemorySearchEmbeddingIndex();
        var service = new SemanticSearchService(index, new DeterministicTextEmbeddingPort(), new AlwaysOffToggle());

        var hits = await service.SearchAsync(new SearchQuery
        {
            RawText = "radiator hose",
            Mode = SearchMode.Semantic,
            Locale = "en"
        }, CancellationToken.None);

        hits.Should().BeEmpty();
    }

    private sealed class AlwaysOnToggle : IAiFeatureToggle
    {
        public bool IsEnabled(string featureKey) => true;
    }

    private sealed class AlwaysOffToggle : IAiFeatureToggle
    {
        public bool IsEnabled(string featureKey) => false;
    }
}
