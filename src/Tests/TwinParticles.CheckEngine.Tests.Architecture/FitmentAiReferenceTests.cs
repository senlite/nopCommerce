using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class FitmentAiReferenceTests
{
    [Test]
    public void Format_Should_Encode_Hash_And_Rationale()
    {
        FitmentAiReference.Format("abc123", "OEM cross-reference missing")
            .Should().Be("abc123|OEM cross-reference missing");
    }

    [Test]
    public void Format_Should_Return_Hash_Only_When_Rationale_Empty()
    {
        FitmentAiReference.Format("abc123", null).Should().Be("abc123");
        FitmentAiReference.Format("abc123", "   ").Should().Be("abc123");
    }

    [Test]
    public void Parse_Should_Round_Trip_Rationale_With_Pipe_In_Text()
    {
        var encoded = FitmentAiReference.Format("hash", "note|detail");
        var parsed = FitmentAiReference.Parse(encoded);

        parsed.PromptHash.Should().Be("hash");
        parsed.Rationale.Should().Be("note|detail");
    }

    [Test]
    public void Parse_Should_Treat_Reference_Without_Separator_As_Hash()
    {
        FitmentAiReference.Parse("legacy-hash").PromptHash.Should().Be("legacy-hash");
        FitmentAiReference.Parse("legacy-hash").Rationale.Should().BeNull();
    }
}
