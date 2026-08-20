using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class HostErpConventionsBySearchTests
{
    [Test]
    public void ErpAdmin_Controller_And_Route_Should_Exist()
    {
        var controllerPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Controllers", "ErpAdminController.cs");
        controllerPath = System.IO.Path.GetFullPath(controllerPath);
        System.IO.File.Exists(controllerPath).Should().BeTrue();

        var routePath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Infrastructure", "RouteProvider.cs");
        routePath = System.IO.Path.GetFullPath(routePath);
        var content = System.IO.File.ReadAllText(routePath);

        content.Should().Contain("Plugin.TwinParticles.CheckEngine.ErpAdmin");
        content.Should().Contain("Admin/CheckEngine/ErpAdmin/{action?}");
    }
}
