using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SearchFacetAggregatorTests
{
    private static IReadOnlyList<SearchHit> SampleHits() =>
    [
        new SearchHit { ProductId = 1, Name = "A", CategoryId = 10, CategoryName = "Engine", Brand = "BMW", Price = 24.90m, FitsActiveContext = true },
        new SearchHit { ProductId = 2, Name = "B", CategoryId = 10, CategoryName = "Engine", Brand = "Mann", Price = 18.50m, FitsActiveContext = true },
        new SearchHit { ProductId = 3, Name = "C", CategoryId = 20, CategoryName = "Cooling", Brand = "BMW", Price = 79.00m, FitsActiveContext = false },
        new SearchHit { ProductId = 4, Name = "D", CategoryId = 20, CategoryName = "Cooling", Brand = "Conti", Price = 320.00m, FitsActiveContext = false }
    ];

    [Test]
    public void Aggregate_Should_Count_Categories_Brands_And_Price_Bands()
    {
        var facets = SearchFacetAggregator.Aggregate(SampleHits(), vehicleContextApplied: false);

        var category = facets.Where(f => f.Key == "category").ToList();
        category.Should().Contain(f => f.Value == "10" && f.Count == 2 && f.Label == "Engine");
        category.Should().Contain(f => f.Value == "20" && f.Count == 2 && f.Label == "Cooling");

        var brand = facets.Where(f => f.Key == "brand").ToList();
        brand.Should().Contain(f => f.Value == "BMW" && f.Count == 2);
        brand.Should().Contain(f => f.Value == "Mann" && f.Count == 1);
        brand.Should().Contain(f => f.Value == "Conti" && f.Count == 1);

        var price = facets.Where(f => f.Key == "price").ToList();
        price.Should().Contain(f => f.Value == "0-50" && f.Count == 2);
        price.Should().Contain(f => f.Value == "50-100" && f.Count == 1);
        price.Should().Contain(f => f.Value == "250-500" && f.Count == 1);
    }

    [Test]
    public void Aggregate_Should_Include_Fitment_Facet_Only_When_Vehicle_Context_Applied()
    {
        var withoutContext = SearchFacetAggregator.Aggregate(SampleHits(), vehicleContextApplied: false);
        withoutContext.Should().NotContain(f => f.Key == "fitment");

        var withContext = SearchFacetAggregator.Aggregate(SampleHits(), vehicleContextApplied: true);
        var fitment = withContext.Where(f => f.Key == "fitment").ToList();
        fitment.Should().Contain(f => f.Value == "fits" && f.Count == 2);
        fitment.Should().Contain(f => f.Value == "unverified" && f.Count == 2);
    }

    [Test]
    public void Aggregate_Should_Return_Empty_For_No_Hits()
    {
        SearchFacetAggregator.Aggregate([], vehicleContextApplied: true).Should().BeEmpty();
    }
}
