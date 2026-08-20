using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class HostImportAdminConventionsBySearchTests
{
    [Test]
    public void ImportAdminController_And_Routes_Should_Exist()
    {
        var controllerPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Controllers", "ImportAdminController.cs");
        controllerPath = System.IO.Path.GetFullPath(controllerPath);
        System.IO.File.Exists(controllerPath).Should().BeTrue();

        var controller = System.IO.File.ReadAllText(controllerPath);
        controller.Should().Contain("class ImportAdminController");
        controller.Should().Contain("Task<IActionResult> Run(");
        controller.Should().Contain("Task<IActionResult> Batch(");
        controller.Should().Contain("Task<IActionResult> Publish(");

        var routePath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Infrastructure", "RouteProvider.cs");
        routePath = System.IO.Path.GetFullPath(routePath);
        System.IO.File.Exists(routePath).Should().BeTrue();

        var routes = System.IO.File.ReadAllText(routePath);
        routes.Should().Contain("Plugin.TwinParticles.CheckEngine.ImportAdmin");
        routes.Should().Contain("Admin/CheckEngine/ImportAdmin/{action?}");
    }
}
