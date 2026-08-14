using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class MegaMenuContractTests
{
    [Test]
    public void Mega_Menu_Should_Use_Live_Localized_Categories_And_Keyboard_Native_Disclosure()
    {
        var component = ReadPluginFile("Components", "CheckEngineThemeChromeViewComponent.cs");
        var view = ReadPluginFile("Views", "Shared", "Components", "CheckEngineThemeChrome", "Default.cshtml");

        component.Should().Contain("GetAllCategoriesAsync");
        component.Should().Contain("GetLocalizedAsync");
        component.Should().Contain("GetSeNameAsync");
        component.Should().Contain("PublicWidgetZones.HeaderAfter");
        view.Should().Contain("zone == \"header_after\"");
        view.Should().Contain("<details class=\"ce-mega\">");
        view.Should().Contain("<summary class=\"ce-mega__trigger\"");
        view.Should().Contain("aria-label=");
        view.Should().Contain("Url.RouteUrl(\"Category\"");
    }

    [Test]
    public void Mega_Menu_Should_Have_Desktop_Tablet_And_Mobile_Layouts()
    {
        var css = ReadPluginFile("Content", "checkengine-theme.css");

        css.Should().Contain("grid-template-columns: repeat(4");
        css.Should().Contain("grid-template-columns: repeat(2");
        css.Should().Contain(".ce-mega__panel");
        css.Should().Contain("position: fixed");
        css.Should().Contain("max-block-size: min(76vh");
    }

    [Test]
    public void Mega_Menu_Should_Close_On_Escape_And_Outside_Click()
    {
        var script = ReadPluginFile("Content", "checkengine-storefront.js");

        script.Should().Contain("function bindMegaMenu()");
        script.Should().Contain("event.key === 'Escape'");
        script.Should().Contain("!menu.contains(event.target)");
        script.Should().Contain("trigger.setAttribute('aria-expanded'");
        script.Should().Contain("trigger.focus()");
    }

    private static string ReadPluginFile(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }
}
