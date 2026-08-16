using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class UnifiedSearchNaturalLanguageIntentTests
{
    [Test]
    public async Task SearchAsync_Should_Return_ParsedIntent_For_Natural_Language_Mode()
    {
        var service = await SearchTestSupport.BuildSearchServiceWithSemanticAsync();

        var result = await service.SearchAsync(new SearchQuery
        {
            RawText = "oil filter for my bmw",
            Mode = SearchMode.NaturalLanguage,
            Locale = "en",
            Page = 1,
            PageSize = 5
        }, CancellationToken.None);

        result.ModeUsed.Should().Be(SearchMode.NaturalLanguage);
        result.ParsedIntent.Should().NotBeNull();
        result.ParsedIntent!.PartTerms.Should().NotBeEmpty();
    }
}
