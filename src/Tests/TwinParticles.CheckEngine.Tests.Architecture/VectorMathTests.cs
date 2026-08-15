using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VectorMathTests
{
    [Test]
    public void CosineSimilarityTopK_Should_Return_Highest_Scoring_Items()
    {
        var query = VectorMath.EmbedText("cooling radiator hose");
        var candidates = new[]
        {
            VectorMath.EmbedText("radiator hose cooling"),
            VectorMath.EmbedText("bmw oil filter"),
            VectorMath.EmbedText("cabin air filter"),
            VectorMath.EmbedText("upper radiator coolant hose")
        };

        var topK = new CosineSimilarityTopK<int>(query, 2, id => id);
        for (var index = 0; index < candidates.Length; index++)
            topK.Consider(index, candidates[index]);

        var results = topK.Results();
        results.Should().HaveCount(2);
        results[0].Score.Should().BeGreaterThan(results[1].Score);
        results.Select(entry => entry.Item).Should().Contain(0);
        results.Select(entry => entry.Item).Should().Contain(3);
    }

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
