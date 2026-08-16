using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AiSpecificationCandidateFormatterTests
{
    [Test]
    public void TryFormat_Should_Parse_Json_Array()
    {
        var ok = AiSpecificationCandidateFormatter.TryFormat(
            """[{"key":"Material","value":"Steel"},{"key":"Thread","value":"M14x1.5"}]""",
            out var formatted);

        ok.Should().BeTrue();
        formatted.Should().Contain("Material: Steel");
        formatted.Should().Contain("Thread: M14x1.5");
    }

    [Test]
    public void TryFormat_Should_Accept_Plain_Text()
    {
        var ok = AiSpecificationCandidateFormatter.TryFormat("Weight: 1.2 kg", out var formatted);

        ok.Should().BeTrue();
        formatted.Should().Be("Weight: 1.2 kg");
    }

    [Test]
    public void ParseLines_Should_Read_Key_Value_Pairs()
    {
        var lines = AiSpecificationCandidateFormatter.ParseLines("Material: Steel\nThread: M14x1.5");

        lines.Should().HaveCount(2);
        lines[0].Key.Should().Be("Material");
        lines[0].Value.Should().Be("Steel");
    }
}
