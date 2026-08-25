using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SearchLoadGateConventionsTests
{
    [Test]
    public void Repository_Should_Ship_K6_Nfr017_Script()
    {
        var script = LocateRepoFile("CheckEngine", "tests", "perf", "search-nfr017.js");
        File.Exists(script).Should().BeTrue("NFR-017 requires a versioned k6 load script");
        var contents = File.ReadAllText(script);
        contents.Should().Contain("NFR-017");
        contents.Should().Contain("2000");
        contents.Should().Contain("/check-engine/search/query");
        contents.Should().Contain("p(95)<");
        contents.Should().Contain("p(99)<600");
        contents.Should().Contain("Mozilla/5.0");
    }

    [Test]
    public void Repository_Should_Ship_Search_Load_Gate_Scripts()
    {
        var sh = LocateRepoFile("CheckEngine", "scripts", "run-search-load-gate.sh");
        var ps1 = LocateRepoFile("CheckEngine", "scripts", "run-search-load-gate.ps1");
        var sample = LocateRepoFile("CheckEngine", "scripts", "search-nfr017-sample.mjs");
        File.Exists(sh).Should().BeTrue();
        File.Exists(ps1).Should().BeTrue();
        File.Exists(sample).Should().BeTrue();
        File.ReadAllText(sh).Should().Contain("NFR-017");
        File.ReadAllText(sh).Should().Contain("search-nfr017.js");
        File.ReadAllText(sh).Should().Contain("CHECKENGINE_LOAD_CONCURRENCY");
        File.ReadAllText(sample).Should().Contain("2000");
        File.ReadAllText(sample).Should().Contain("/check-engine/search/query");
        File.ReadAllText(sample).Should().Contain("Mozilla/5.0");
        File.ReadAllText(sample).Should().Contain("CHECKENGINE_LOAD_CONCURRENCY");
    }

    [Test]
    public void SearchController_Should_Rate_Limit_Per_Shopper_Not_Shared_Nat_Ip()
    {
        var controller = LocateRepoFile("src", "Plugins", "TwinParticles.CheckEngine", "Controllers", "SearchController.cs");
        var contents = File.ReadAllText(controller);
        contents.Should().Contain("search:customer:");
        contents.Should().Contain("suggest:customer:");
        contents.Should().Contain("recommend:customer:");
        contents.Should().NotContain("search:ip:");
        contents.Should().NotContain("suggest:ip:");
        contents.Should().Contain("NFR-017");
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
