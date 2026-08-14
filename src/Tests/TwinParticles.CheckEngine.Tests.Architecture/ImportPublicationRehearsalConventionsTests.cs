using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ImportPublicationRehearsalConventionsTests
{
    [Test]
    public void Repository_Should_Ship_Import_Publication_Rehearsal_Script()
    {
        var script = LocateRepoFile("CheckEngine", "scripts", "run-import-publication-rehearsal.sh");
        File.Exists(script).Should().BeTrue("H1.21 requires an operator rehearsal entry point");
        var contents = File.ReadAllText(script);
        contents.Should().Contain("10000");
        contents.Should().Contain("NopImportProductPublisher");
        contents.Should().Contain("ImportProductionPathContractTests");
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
