using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SearchEmbeddingCatalogTextBuilderTests
{
    [Test]
    public void Build_Should_Join_NonEmpty_Parts()
    {
        var text = SearchEmbeddingCatalogTextBuilder.Build(
            "Water Pump",
            "Cooling",
            "BMW",
            null,
            "11517586925",
            "water pump bmw");

        text.Should().Be("Water Pump Cooling BMW 11517586925 water pump bmw");
    }

    [Test]
    public void Build_Should_Return_Empty_When_All_Parts_Are_Blank()
    {
        SearchEmbeddingCatalogTextBuilder.Build(null, " ", string.Empty).Should().BeEmpty();
    }

    [Test]
    public void BuildForEmbedding_Should_Expand_Arabic_Synonyms_For_Index_Text()
    {
        var text = SearchEmbeddingCatalogTextBuilder.BuildForEmbedding(
            "ar",
            "فلتر زيت BMW",
            "محرك");

        text.Should().Contain("oil filter");
    }
}
