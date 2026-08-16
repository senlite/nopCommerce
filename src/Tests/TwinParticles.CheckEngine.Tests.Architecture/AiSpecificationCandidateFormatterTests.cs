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

    [Test]
    public void Validate_Should_Flag_Unknown_Keys_And_Compute_Quality_Score()
    {
        var catalog = new AllowedSpecificationKeyCatalog(["Material", "Thread"]);
        var result = AiSpecificationCandidateFormatter.Validate(
            """[{"key":"Material","value":"Steel"},{"key":"FooBar","value":"X"}]""",
            catalog);

        result.UnknownKeys.Should().ContainSingle().Which.Should().Be("FooBar");
        result.QualityScore.Should().Be(0.5m);
        result.Lines.Should().HaveCount(2);
    }

    [Test]
    public void Validate_Should_Return_Perfect_Score_When_All_Keys_Allowed()
    {
        var catalog = new AllowedSpecificationKeyCatalog(["Material", "Thread"]);
        var result = AiSpecificationCandidateFormatter.Validate(
            """[{"key":"Material","value":"Steel"},{"key":"Thread","value":"M14x1.5"}]""",
            catalog);

        result.UnknownKeys.Should().BeEmpty();
        result.QualityScore.Should().Be(1m);
    }
}
