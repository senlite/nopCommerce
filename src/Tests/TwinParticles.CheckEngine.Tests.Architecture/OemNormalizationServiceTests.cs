using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Infrastructure.Oem;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class OemNormalizationServiceTests
{
    [Test]
    public void Normalize_Should_Remove_Separators_And_Uppercase()
    {
        var service = new DefaultOemNormalizationService();

        var result = service.Normalize("11-51-7-586-925");

        result.Should().Be("11517586925");
    }

    [Test]
    public void Normalize_Should_Remove_Spaces_Dots_And_Decorative_Punctuation()
    {
        var service = new DefaultOemNormalizationService();

        var result = service.Normalize(" 11 51.7·586 925 ");

        result.Should().Be("11517586925");
    }

    [Test]
    public void Normalize_Should_Return_Empty_For_Null_Input()
    {
        var service = new DefaultOemNormalizationService();

        var result = service.Normalize(null!);

        result.Should().BeEmpty();
    }
}
