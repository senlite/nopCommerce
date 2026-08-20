using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class HostFitmentConventionsBySearchTests
{
    [Test]
    public void FitmentControllers_And_Routes_Should_Exist()
    {
        var publicControllerPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Controllers", "FitmentController.cs");
        publicControllerPath = System.IO.Path.GetFullPath(publicControllerPath);
        System.IO.File.Exists(publicControllerPath).Should().BeTrue();

        var adminControllerPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Controllers", "FitmentAdminController.cs");
        adminControllerPath = System.IO.Path.GetFullPath(adminControllerPath);
        System.IO.File.Exists(adminControllerPath).Should().BeTrue();

        var routePath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Infrastructure", "RouteProvider.cs");
        routePath = System.IO.Path.GetFullPath(routePath);
        var routes = System.IO.File.ReadAllText(routePath);

        routes.Should().Contain("Plugin.TwinParticles.CheckEngine.FitmentEvaluate");
        routes.Should().Contain("check-engine/fitment/evaluate");
        routes.Should().Contain("Plugin.TwinParticles.CheckEngine.FitmentAdmin");
        routes.Should().Contain("Admin/CheckEngine/FitmentAdmin/{action?}");
    }
}
