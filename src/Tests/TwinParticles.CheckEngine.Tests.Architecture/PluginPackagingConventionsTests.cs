using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class PluginPackagingConventionsTests
{
    [Test]
    public void Repository_Should_Ship_Plugin_Pack_Scripts()
    {
        var py = LocateRepoFile("CheckEngine", "scripts", "pack-checkengine.py");
        var sh = LocateRepoFile("CheckEngine", "scripts", "pack-checkengine.sh");
        var ps1 = LocateRepoFile("CheckEngine", "scripts", "pack-checkengine.ps1");
        File.Exists(py).Should().BeTrue("G11 requires a versioned pack script");
        File.Exists(sh).Should().BeTrue();
        File.Exists(ps1).Should().BeTrue();

        var python = File.ReadAllText(py);
        python.Should().Contain("TwinParticles.CheckEngine");
        python.Should().Contain(".zip");
        python.Should().Contain("sha256");
        python.Should().Contain("nop.web");
        python.Should().Contain("app_data");
        python.Should().Contain("plugin.json");
        python.Should().Contain("signed");
        python.Should().Contain("G11");

        File.ReadAllText(sh).Should().Contain("pack-checkengine.py");
        File.ReadAllText(sh).Should().Contain("G11");
    }

    [Test]
    public void Pack_Script_Should_Refuse_Host_And_Customer_Payloads()
    {
        var python = File.ReadAllText(LocateRepoFile("CheckEngine", "scripts", "pack-checkengine.py"));
        python.Should().Contain("nop.web");
        python.Should().Contain("app_data");
        python.Should().Contain("connectionstring");
        python.Should().Contain("runtimes");
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
