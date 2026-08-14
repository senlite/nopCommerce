using System.IO;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Seo;
using TwinParticles.CheckEngine.Infrastructure.Seo;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class CoreWebVitalsGateConventionsTests
{
    [Test]
    public void Performance_Budget_Service_Should_Expose_Documented_NFR054_Thresholds()
    {
        var service = new DefaultSeoPerformanceBudgetService();

        service.MaxLargestContentfulPaintMs.Should().Be(2500);
        service.MaxInteractionToNextPaintMs.Should().Be(200);
        service.MaxCumulativeLayoutShift.Should().Be(0.1m);
        service.MeetsBudget(2400, 180, 0.05m).Should().BeTrue();
        service.MeetsBudget(2600, 180, 0.05m).Should().BeFalse();
    }

    [Test]
    public void Repository_Should_Ship_CWV_Gate_Script_For_Operator_Rehearsal()
    {
        var script = LocateRepoFile("CheckEngine", "scripts", "run-cwv-gate.sh");
        File.Exists(script).Should().BeTrue("H1.28 requires an operator-runnable CWV rehearsal entry point");
        var contents = File.ReadAllText(script);
        contents.Should().Contain("lighthouse");
        contents.Should().Contain("NFR-054");
        contents.Should().Contain("form-factor");
        contents.Should().Contain("cpuSlowdownMultiplier");
    }

    [Test]
    public void Repository_Should_Ship_CWV_Gate_PowerShell_Script_With_Mobile_Throttling()
    {
        var script = LocateRepoFile("CheckEngine", "scripts", "run-cwv-gate.ps1");
        File.Exists(script).Should().BeTrue();
        var contents = File.ReadAllText(script);
        contents.Should().Contain("cpuSlowdownMultiplier");
        contents.Should().Contain("/ar/");
    }

    private static string LocateRepoFile(params string[] relativePath)
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             dir is not null;
             dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, .. relativePath]);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }
}
