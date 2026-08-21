using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Application.Garage;
using TwinParticles.CheckEngine.Application.ImportPipeline.Stages;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class CoverageGateConventionsTests
{
    [Test]
    public void Coverage_Runsettings_Should_Include_Domain_And_Application_Only()
    {
        var runsettings = ReadRepoFile("src", "Tests", "TwinParticles.CheckEngine.Tests.Architecture", "coverage.runsettings");
        runsettings.Should().Contain("[TwinParticles.CheckEngine.Domain]*");
        runsettings.Should().Contain("[TwinParticles.CheckEngine.Application]*");
        runsettings.Should().Contain("cobertura");
        runsettings.Should().NotContain("[TwinParticles.CheckEngine.Infrastructure]*");
    }

    [Test]
    public void Coverage_Assert_Script_Should_Enforce_Nfr_Line_Floors()
    {
        var script = ReadRepoFile("CheckEngine", "scripts", "assert-checkengine-coverage.py");
        script.Should().Contain("DOMAIN_MIN = 80.0");
        script.Should().Contain("APPLICATION_MIN = 70.0");
        script.Should().Contain("TwinParticles.CheckEngine.Domain");
        script.Should().Contain("TwinParticles.CheckEngine.Application");
        script.Should().Contain("line-rate");
    }

    [Test]
    public void Domain_And_Application_Assemblies_Should_Expose_Core_Coverage_Types()
    {
        var domainTypes = typeof(Vin).Assembly.GetTypes();
        var applicationTypes = typeof(FitmentPublicationPolicyService).Assembly.GetTypes();
        var all = domainTypes.Concat(applicationTypes).ToArray();

        AssertTypeExists(all, nameof(FitmentPublicationPolicyService));
        AssertTypeExists(all, nameof(Vin));
        AssertTypeExists(all, nameof(GarageService));
        AssertTypeExists(all, "OemNormalization");
        AssertTypeExists(all, nameof(ImportReviewService));
    }

    private static void AssertTypeExists(Type[] types, string nameFragment)
    {
        types.Any(t => t.Name.Contains(nameFragment, StringComparison.Ordinal))
            .Should().BeTrue($"coverage gate expects a type containing '{nameFragment}' in Domain/Application assemblies");
    }

    private static string ReadRepoFile(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }
}
