using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ThemeWidgetPluginTests
{
    [Test]
    public void Theme_Component_Artifacts_Should_Exist()
    {
        var pluginRoot = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine");
        pluginRoot = System.IO.Path.GetFullPath(pluginRoot);

        System.IO.File.Exists(System.IO.Path.Combine(pluginRoot, "Components", "CheckEngineThemeChromeViewComponent.cs")).Should().BeTrue();
        System.IO.File.Exists(System.IO.Path.Combine(pluginRoot, "Views", "Shared", "Components", "CheckEngineThemeChrome", "Default.cshtml")).Should().BeTrue();
        System.IO.File.Exists(System.IO.Path.Combine(pluginRoot, "Content", "checkengine-theme.css")).Should().BeTrue();
    }
}
