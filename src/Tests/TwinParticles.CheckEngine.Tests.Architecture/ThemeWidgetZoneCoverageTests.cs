using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ThemeWidgetZoneCoverageTests
{
    [Test]
    public void CheckEnginePlugin_Should_Declare_EP11_Widget_Zones()
    {
        var pluginPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "CheckEnginePlugin.cs");
        pluginPath = System.IO.Path.GetFullPath(pluginPath);
        var content = System.IO.File.ReadAllText(pluginPath);

        content.Should().Contain("PublicWidgetZones.HeaderAfter");
        content.Should().Contain("PublicWidgetZones.HeaderMenuAfter");
        content.Should().Contain("PublicWidgetZones.BodyStartHtmlTagAfter");
        content.Should().Contain("PublicWidgetZones.ProductDetailsTop");
        content.Should().Contain("PublicWidgetZones.HomepageTop");
        content.Should().Contain("AdminWidgetZones.PluginListButtons");
        content.Should().Contain("UninstallPreparationViewComponent");
    }
}
