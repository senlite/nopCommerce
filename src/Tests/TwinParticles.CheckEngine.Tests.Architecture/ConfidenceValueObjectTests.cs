using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ConfidenceValueObjectTests
{
    [Test]
    public void TryCreate_Should_Accept_Boundary_Values()
    {
        var lowCreated = Confidence.TryCreate(0m, out var low, out var lowErrorCode);
        var highCreated = Confidence.TryCreate(1m, out var high, out var highErrorCode);

        lowCreated.Should().BeTrue();
        highCreated.Should().BeTrue();
        lowErrorCode.Should().BeNull();
        highErrorCode.Should().BeNull();
        low!.Value.Should().Be(0m);
        high!.Value.Should().Be(1m);
    }

    [Test]
    public void TryCreate_Should_Reject_Value_Below_Zero()
    {
        var created = Confidence.TryCreate(-0.01m, out var confidence, out var errorCode);

        created.Should().BeFalse();
        confidence.Should().BeNull();
        errorCode.Should().Be("vin.confidence_out_of_range");
    }

    [Test]
    public void TryCreate_Should_Reject_Value_Above_One()
    {
        var created = Confidence.TryCreate(1.01m, out var confidence, out var errorCode);

        created.Should().BeFalse();
        confidence.Should().BeNull();
        errorCode.Should().Be("vin.confidence_out_of_range");
    }
}
