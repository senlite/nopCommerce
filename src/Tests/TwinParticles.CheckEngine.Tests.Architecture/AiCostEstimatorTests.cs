using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AiCostEstimatorTests
{
    [Test]
    public void EstimateUsd_Should_Scale_By_Tokens_And_Rate()
    {
        AiCostEstimator.EstimateUsd(2000, 0.5m).Should().Be(1.0m);
    }

    [Test]
    public void EstimateUsd_Should_Return_Zero_For_Non_Positive_Input()
    {
        AiCostEstimator.EstimateUsd(0, 0.5m).Should().Be(0m);
        AiCostEstimator.EstimateUsd(100, 0m).Should().Be(0m);
    }
}
