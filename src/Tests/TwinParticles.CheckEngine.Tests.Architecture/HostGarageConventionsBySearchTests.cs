using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class HostGarageConventionsBySearchTests
{
    [Test]
    public void GarageControllers_And_Routes_Should_Exist()
    {
        var publicControllerPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Controllers", "GarageController.cs");
        publicControllerPath = System.IO.Path.GetFullPath(publicControllerPath);
        System.IO.File.Exists(publicControllerPath).Should().BeTrue();

        var adminControllerPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Controllers", "GarageAdminController.cs");
        adminControllerPath = System.IO.Path.GetFullPath(adminControllerPath);
        System.IO.File.Exists(adminControllerPath).Should().BeTrue();

        var routePath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Infrastructure", "RouteProvider.cs");
        routePath = System.IO.Path.GetFullPath(routePath);
        var routes = System.IO.File.ReadAllText(routePath);

        routes.Should().Contain("Plugin.TwinParticles.CheckEngine.Garage");
        routes.Should().Contain("check-engine/garage/{action}");
        routes.Should().Contain("Plugin.TwinParticles.CheckEngine.GarageAdmin");
        routes.Should().Contain("Admin/CheckEngine/GarageAdmin/{action}");
    }
}
