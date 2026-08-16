using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class CustomerAssistantConventionsTests
{
    [Test]
    public void AssistantController_Ask_Should_Gate_Feature_And_Rate_Limit()
    {
        var controller = ReadPluginFile("Controllers", "AssistantController.cs");

        controller.Should().Contain("Task<IActionResult> Ask(");
        controller.Should().Contain("AiFeatureKeys.CustomerAssistant");
        controller.Should().Contain("_searchRateLimiter.TryAcquire");
        controller.Should().Contain("assistant:ip:");
        controller.Should().Contain("assistant:customer:");
        controller.Should().Contain("assistant.rate_limited");
        controller.Should().Contain("assistant.empty_question");
    }

    [Test]
    public void Storefront_Assistant_Widget_Should_Support_Loading_Rate_Limit_And_SeName_Citations()
    {
        var script = ReadPluginFile("Content", "checkengine-storefront.js");
        var theme = ReadPluginFile("Views", "Shared", "Components", "CheckEngineThemeChrome", "Default.cshtml");
        var routes = ReadPluginFile("Infrastructure", "RouteProvider.cs");

        script.Should().Contain("function bindAssistant()");
        script.Should().Contain("function updateAssistantContext(");
        script.Should().Contain("assistantThinking");
        script.Should().Contain("assistantRateLimited");
        script.Should().Contain("assistantVehicleScoped");
        script.Should().Contain("/check-engine/assistant/ask");
        script.Should().Contain("citation.seName || citation.SeName");
        script.Should().Contain("thinking: true");

        theme.Should().Contain("data-ce-theme=\"assistant\"");
        theme.Should().Contain("EnableCustomerAssistant");
        theme.Should().Contain("ce-assistant-context");

        routes.Should().Contain("check-engine/assistant/ask");
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
