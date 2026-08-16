using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class NaturalLanguageIntentParserHeuristicTests
{
    [Test]
    public async Task ParseAsync_Should_Extract_Make_Model_And_Part_Terms()
    {
        var parser = new NaturalLanguageIntentParser();
        var intent = await parser.ParseAsync("2015 BMW 320i oil filter", "en", CancellationToken.None);

        intent.Make.Should().Be("BMW");
        intent.Model.Should().Be("320i");
        intent.ModelYear.Should().Be(2015);
        intent.PartTerms.Should().Contain("oil");
        intent.PartTerms.Should().Contain("filter");
    }
}
