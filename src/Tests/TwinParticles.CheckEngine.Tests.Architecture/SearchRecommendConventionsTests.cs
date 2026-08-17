using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SearchRecommendConventionsTests
{
    [Test]
    public void SearchController_Recommend_Should_Gate_Feature_Rate_Limit_And_Garage_Context()
    {
        var controller = ReadPluginFile("Controllers", "SearchController.cs");

        controller.Should().Contain("Task<IActionResult> Recommend(");
        controller.Should().Contain("AiFeatureKeys.Recommendations");
        controller.Should().Contain("_searchRateLimiter.TryAcquire");
        controller.Should().Contain("recommend:ip:");
        controller.Should().Contain("recommend:customer:");
        controller.Should().Contain("search.rate_limited");
        controller.Should().Contain("seedProductId");
        controller.Should().Contain("_garageService.GetAsync");
    }

    [Test]
    public void Storefront_Recommend_Rail_Should_Use_SeName_And_Feature_Flag()
    {
        var script = ReadPluginFile("Content", "checkengine-storefront.js");
        var theme = ReadPluginFile("Views", "Shared", "Components", "CheckEngineThemeChrome", "Default.cshtml");
        var mapper = ReadPluginFile("Configuration", "CheckEngineConfigurationMapper.cs");

        script.Should().Contain("function loadRecommendations()");
        script.Should().Contain("/check-engine/search/recommend");
        script.Should().Contain("seedProductId");
        script.Should().Contain("hit.seName || hit.SeName");
        script.Should().Contain("data-ce-enable-recommendations");
        script.Should().Contain("recommendFitmentBadge");
        script.Should().Contain("recommendUnscopedTitle");

        theme.Should().Contain("data-ce-theme=\"recommendations\"");
        theme.Should().Contain("data-ce-enable-recommendations");
        theme.Should().Contain("EnableRecommendations");

        mapper.Should().Contain("EnableRecommendations = enabled.Contains(AiFeatureKeys.Recommendations)");
        mapper.Should().Contain("if (model.EnableRecommendations) enabledFeatures.Add(AiFeatureKeys.Recommendations)");
    }

    private static string ReadPluginFile(params string[] segments)
    {
        var path = System.IO.Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "Plugins", "TwinParticles.CheckEngine",
            System.IO.Path.Combine(segments));
        path = System.IO.Path.GetFullPath(path);
        System.IO.File.Exists(path).Should().BeTrue(path);
        return System.IO.File.ReadAllText(path);
    }
}
