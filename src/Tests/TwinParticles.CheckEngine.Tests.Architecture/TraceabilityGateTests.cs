using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class TraceabilityGateTests
{
    private static readonly string[] RequiredAcceptanceCriteria =
    [
        "AC-015",
        "AC-027",
        "AC-028",
        "AC-034",
        "AC-060",
        "AC-070",
        "AC-076",
        "AC-080",
        "AC-095",
        "AC-099"
    ];

    [Test]
    public void Acceptance_Criteria_Doc_Should_Contain_Traceability_Anchors()
    {
        var path = LocateAcceptanceCriteriaDoc();
        File.Exists(path).Should().BeTrue($"expected acceptance criteria at {path}");

        var content = File.ReadAllText(path);
        foreach (var ac in RequiredAcceptanceCriteria)
        {
            content.Should().Contain(ac, $"traceability gate requires '{ac}' in 40-acceptance-criteria.md (T6.5)");
        }
    }

    private static string LocateAcceptanceCriteriaDoc()
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "CheckEngine", "docs", "40-acceptance-criteria.md");
            if (File.Exists(candidate))
                return candidate;
        }

        return "/workspace/CheckEngine/docs/40-acceptance-criteria.md";
    }
}
