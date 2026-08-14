using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SearchFacetsAndRecoveryTests
{
    [Test]
    public async Task SearchAsync_Should_Compute_Facets_And_Total_Over_Full_Result_Not_Just_Page()
    {
        var service = SearchTestSupport.BuildSearchService();

        var result = await service.SearchAsync(new SearchQuery
        {
            RawText = string.Empty,
            Mode = SearchMode.Keyword,
            Locale = "en",
            Page = 1,
            PageSize = 1
        }, CancellationToken.None);

        result.Hits.Should().HaveCount(1, "the page size is one");
        result.Total.Should().Be(3, "total reflects the whole result set, not the page");

        result.Facets.Where(f => f.Key == "category").Sum(f => f.Count).Should().Be(3);
        result.Facets.Should().Contain(f => f.Key == "category" && f.Value == "10" && f.Count == 2);
        result.Facets.Should().Contain(f => f.Key == "brand");
        result.Facets.Should().Contain(f => f.Key == "price");
    }

    [Test]
    public async Task SearchAsync_Should_Filter_By_Selected_Brand_Facet()
    {
        var service = SearchTestSupport.BuildSearchService();

        var result = await service.SearchAsync(new SearchQuery
        {
            RawText = string.Empty,
            Mode = SearchMode.Keyword,
            Locale = "en",
            Filters = new SearchFilters { Brand = "BMW" },
            PageSize = 24
        }, CancellationToken.None);

        result.Total.Should().Be(1);
        result.Hits.Should().OnlyContain(hit => hit.Brand == "BMW");
    }

    [Test]
    public async Task SearchAsync_Should_Offer_At_Least_Two_Recovery_Actions_On_Zero_Results()
    {
        var service = SearchTestSupport.BuildSearchService();

        var result = await service.SearchAsync(new SearchQuery
        {
            RawText = "zzzzz-no-match",
            Mode = SearchMode.Keyword,
            Locale = "en"
        }, CancellationToken.None);

        result.Total.Should().Be(0);
        result.Recovery.Count.Should().BeGreaterThanOrEqualTo(2, "AC-23.2 requires at least two recovery actions");
        result.Recovery.Should().Contain(action => action.Kind == "select_vehicle");
        result.Recovery.Should().Contain(action => action.Kind == "broaden_keyword");
        result.Suggestions.Should().BeEquivalentTo(result.Recovery.Select(action => action.Label));
    }

    [Test]
    public async Task SearchAsync_Should_Offer_Widen_Fitment_Recovery_When_Vehicle_Context_Is_Strict()
    {
        var service = SearchTestSupport.BuildSearchService(fitmentMap: new()
        {
            [1001] = FitmentStatus.Unknown,
            [1002] = FitmentStatus.Unknown,
            [1003] = FitmentStatus.Unknown
        });

        var result = await service.SearchAsync(new SearchQuery
        {
            RawText = string.Empty,
            Mode = SearchMode.Keyword,
            Locale = "en",
            VehicleConfigurationId = 900,
            WidenFitment = false
        }, CancellationToken.None);

        result.Total.Should().Be(0, "strict fitment excludes unverified parts");
        result.Recovery.Should().Contain(action => action.Kind == "widen_fitment");
    }
}
