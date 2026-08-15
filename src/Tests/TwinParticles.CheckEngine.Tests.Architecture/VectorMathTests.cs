using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VectorMathTests
{
    [Test]
    public void CosineSimilarity_Should_Rank_Related_Text_Higher()
    {
        var radiator = VectorMath.EmbedText("radiator hose cooling");
        var oil = VectorMath.EmbedText("bmw oil filter");
        var query = VectorMath.EmbedText("cooling radiator hose");

        VectorMath.CosineSimilarity(query, radiator).Should().BeGreaterThan(
            VectorMath.CosineSimilarity(query, oil));
    }
}
