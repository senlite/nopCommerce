using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class HostSearchConventionsBySearchTests
{
    [Test]
    public void SearchControllers_And_Routes_Should_Exist()
    {
        var publicControllerPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Controllers", "SearchController.cs");
        publicControllerPath = System.IO.Path.GetFullPath(publicControllerPath);
        System.IO.File.Exists(publicControllerPath).Should().BeTrue();

        var adminControllerPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Controllers", "SearchAdminController.cs");
        adminControllerPath = System.IO.Path.GetFullPath(adminControllerPath);
        System.IO.File.Exists(adminControllerPath).Should().BeTrue();

        var routePath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Infrastructure", "RouteProvider.cs");
        routePath = System.IO.Path.GetFullPath(routePath);
        var routes = System.IO.File.ReadAllText(routePath);

        routes.Should().Contain("Plugin.TwinParticles.CheckEngine.SearchQuery");
        routes.Should().Contain("check-engine/search/query");
        routes.Should().Contain("Plugin.TwinParticles.CheckEngine.SearchRecommend");
        routes.Should().Contain("check-engine/search/recommend");
        routes.Should().Contain("Plugin.TwinParticles.CheckEngine.SearchAdmin");
        routes.Should().Contain("Admin/CheckEngine/SearchAdmin/{action}");
    }
}
