using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class HostImageConventionsBySearchTests
{
    [Test]
    public void ImageAdminController_And_Route_Should_Exist()
    {
        var controllerPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Controllers", "ImageAdminController.cs");
        controllerPath = System.IO.Path.GetFullPath(controllerPath);
        System.IO.File.Exists(controllerPath).Should().BeTrue();

        var controller = System.IO.File.ReadAllText(controllerPath);
        controller.Should().Contain("class ImageAdminController");
        controller.Should().Contain("Task<IActionResult> Replace(");

        var routePath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Infrastructure", "RouteProvider.cs");
        routePath = System.IO.Path.GetFullPath(routePath);
        var routes = System.IO.File.ReadAllText(routePath);

        routes.Should().Contain("Plugin.TwinParticles.CheckEngine.ImageAdmin");
        routes.Should().Contain("Admin/CheckEngine/ImageAdmin/{action}");
    }
}
