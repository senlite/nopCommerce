using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AccessibilityGateConventionsTests
{
    [Test]
    public void Repository_Should_Ship_Axe_Gate_Script_For_Operator_Rehearsal()
    {
        var script = LocateRepoFile("CheckEngine", "scripts", "run-a11y-gate.sh");
        File.Exists(script).Should().BeTrue("G6 requires an operator-runnable axe rehearsal entry point");
        var contents = File.ReadAllText(script);
        contents.Should().Contain("NFR-046");
        contents.Should().Contain("axe-core");
        contents.Should().Contain("a11y-gate.mjs");
        contents.Should().Contain("/ar/");
    }

    [Test]
    public void Repository_Should_Ship_Axe_Gate_PowerShell_And_Node_Runner()
    {
        var ps1 = LocateRepoFile("CheckEngine", "scripts", "run-a11y-gate.ps1");
        var runner = LocateRepoFile("CheckEngine", "scripts", "a11y-gate.mjs");
        File.Exists(ps1).Should().BeTrue();
        File.Exists(runner).Should().BeTrue();

        File.ReadAllText(ps1).Should().Contain("NFR-046");
        var js = File.ReadAllText(runner);
        js.Should().Contain("axe.run");
        js.Should().Contain(".ce-root");
        js.Should().Contain("[data-ce-theme]");
        js.Should().Contain("serious");
        js.Should().Contain("critical");
    }

    [Test]
    public void E2E_Suite_Should_Include_Axe_Scans_For_Key_Templates()
    {
        var spec = LocateRepoFile(
            "src", "Tests", "TwinParticles.CheckEngine.Tests.E2E", "AccessibilityAxeSpecs.cs");
        File.Exists(spec).Should().BeTrue();
        var contents = File.ReadAllText(spec);
        contents.Should().Contain("/search?q=filter");
        contents.Should().Contain("/computers");
        contents.Should().Contain("/en/gmaster-bmw-parts-5");
        contents.Should().Contain("/build-your-own-computer");
        contents.Should().Contain("/en/gmaster-gm-11127548196-2");
        contents.Should().Contain("/ar/");
        contents.Should().Contain("NFR-046");
        contents.Should().Contain("serious");
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
