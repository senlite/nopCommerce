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
        routes.Should().Contain("Plugin.TwinParticles.CheckEngine.SearchSuggest");
        routes.Should().Contain("check-engine/search/suggest");
        routes.Should().Contain("Plugin.TwinParticles.CheckEngine.SearchRecommend");
        routes.Should().Contain("check-engine/search/recommend");
        routes.Should().Contain("Plugin.TwinParticles.CheckEngine.SearchAdmin");
        routes.Should().Contain("Admin/CheckEngine/SearchAdmin/{action}");
        routes.Should().Contain("action = \"Index\"");
    }

    [Test]
    public void Configuration_Autocomplete_Selection_Should_Search_The_Vehicle_Tree_Not_Product_Text()
    {
        var scriptPath = System.IO.Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "Plugins", "TwinParticles.CheckEngine", "Content", "checkengine-storefront.js");
        scriptPath = System.IO.Path.GetFullPath(scriptPath);
        var script = System.IO.File.ReadAllText(scriptPath);

        script.Should().Contain("data-ce-suggest-vehicle");
        script.Should().Contain("selectedSuggestionVehicleId");
        script.Should().Contain("function resolveSearchMode()");
        script.Should().Contain("if (selectedSuggestionVehicleId)");
        script.Should().Contain("return 4");
        script.Should().Contain("mode: resolveSearchMode()");
        script.Should().Contain("vehicleConfigurationId: selectedSuggestionVehicleId || null");
    }
}
