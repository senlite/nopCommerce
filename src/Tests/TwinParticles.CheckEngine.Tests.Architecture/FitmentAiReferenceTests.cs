using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class FitmentAiReferenceTests
{
    [Test]
    public void Format_And_Parse_Should_Roundtrip_Rationale()
    {
        var encoded = FitmentAiReference.Format("hash123", "Catalog context insufficient");
        var (hash, rationale) = FitmentAiReference.Parse(encoded);

        hash.Should().Be("hash123");
        rationale.Should().Be("Catalog context insufficient");
    }
}
