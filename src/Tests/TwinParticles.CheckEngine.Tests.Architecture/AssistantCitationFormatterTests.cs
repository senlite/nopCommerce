using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AssistantCitationFormatterTests
{
    [Test]
    public void FromHit_Should_Map_Structured_Fields()
    {
        var citation = AssistantCitationFormatter.FromHit(new SearchHit
        {
            ProductId = 42,
            Name = "Water Pump",
            Brand = "OEM",
            Price = 99.5m,
            SeName = "water-pump"
        });

        citation.ProductId.Should().Be(42);
        citation.Name.Should().Be("Water Pump");
        citation.Brand.Should().Be("OEM");
        citation.Price.Should().Be(99.5m);
        citation.SeName.Should().Be("water-pump");
        citation.DisplayText.Should().Contain("ProductId=42");
    }

    [Test]
    public void BuildContextBlock_Should_Number_Lines()
    {
        var block = AssistantCitationFormatter.BuildContextBlock([
            AssistantCitationFormatter.FromHit(new SearchHit { ProductId = 1, Name = "A" }),
            AssistantCitationFormatter.FromHit(new SearchHit { ProductId = 2, Name = "B" })
        ]);

        block.Should().Contain("1. ProductId=1");
        block.Should().Contain("2. ProductId=2");
    }
}
