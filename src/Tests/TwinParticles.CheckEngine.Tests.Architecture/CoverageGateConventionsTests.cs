using System;
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
}
