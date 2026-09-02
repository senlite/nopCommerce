using System.IO;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class CheckEngineThemePackageTests
{
    [Test]
    public void CheckEngine_Theme_Should_Be_A_Rtl_Dark_Package()
    {
        var themeJson = ReadThemeFile("theme.json");
        using var document = JsonDocument.Parse(themeJson);
        var root = document.RootElement;

        root.GetProperty("SystemName").GetString().Should().Be("CheckEngine");
        root.GetProperty("FriendlyName").GetString().Should().Be("Check Engine");
        root.GetProperty("SupportRTL").GetBoolean().Should().BeTrue();
        root.GetProperty("PreviewImageUrl").GetString().Should().Contain("Themes/CheckEngine/preview.jpg");

        File.Exists(ThemePath("preview.jpg")).Should().BeTrue();
        File.Exists(ThemePath("Views", "_ViewImports.cshtml")).Should().BeTrue();
        File.Exists(ThemePath("Views", "Shared", "Head.cshtml")).Should().BeTrue();
        File.Exists(ThemePath("Content", "css", "styles.css")).Should().BeTrue();
    }

    [Test]
    public void Theme_Head_Should_Overlay_DefaultClean_With_CheckEngine_Shell()
    {
        var head = ReadThemeFile("Views", "Shared", "Head.cshtml");
        head.Should().Contain("~/Themes/DefaultClean/Content/css/styles");
        head.Should().Contain("Content/css/styles.css");
        head.Should().Contain("CheckEngine");
    }

    [Test]
    public void Theme_Home_Should_Drop_Host_Lorem_And_Keep_Hero_Categories()
    {
        var home = ReadThemeFile("Views", "Home", "Index.cshtml");
        home.Should().Contain("PublicWidgetZones.HomepageTop");
        home.Should().Contain("HomepageCategoriesViewComponent");
        home.Should().NotContain("TopicBlockViewComponent");
        home.Should().NotContain("HomepageNewsViewComponent");
        home.Should().NotContain("HomepagePollsViewComponent");
    }

    [Test]
    public void Theme_Logo_Should_Use_CheckEngine_Wordmark()
    {
        var logo = ReadThemeFile("Views", "Shared", "Components", "Logo", "Default.cshtml");
        logo.Should().Contain("ce-wordmark");
        logo.Should().Contain("Plugins.TwinParticles.CheckEngine.Theme.Wordmark");
        logo.Should().NotContain("LogoPath");
    }

    [Test]
    public void Theme_Css_And_Plugin_Should_Activate_The_Dark_Shell()
    {
        var css = ReadThemeFile("Content", "css", "styles.css");
        css.Should().Contain("--ce-surface-sunken");
        css.Should().Contain(".ce-wordmark");
        css.Should().Contain(".header-lower .search-box");
        css.Should().Contain(".ce-footer-disclaimer");

        var plugin = ReadPluginFile("CheckEnginePlugin.cs");
        plugin.Should().Contain("EnsureCheckEngineThemeAsync");
        plugin.Should().Contain("DefaultStoreTheme = \"CheckEngine\"");
        plugin.Should().Contain("PublicWidgetZones.Footer");
        plugin.Should().Contain("Plugins.TwinParticles.CheckEngine.Theme.Affiliation");
        plugin.Should().Contain("Plugins.TwinParticles.CheckEngine.Theme.Wordmark");

        var chrome = ReadPluginFile("Views", "Shared", "Components", "CheckEngineThemeChrome", "Default.cshtml");
        chrome.Should().Contain("zone == \"footer\"");
        chrome.Should().Contain("Theme.Affiliation");
    }

    private static string ReadThemeFile(params string[] segments) => File.ReadAllText(ThemePath(segments));

    private static string ThemePath(params string[] segments)
    {
        var path = Path.Combine([FindRepositoryRoot(), "src", "Presentation", "Nop.Web", "Themes", "CheckEngine", .. segments]);
        return path;
    }

    private static string ReadPluginFile(params string[] segments)
    {
        var path = Path.Combine([FindRepositoryRoot(), "src", "Plugins", "TwinParticles.CheckEngine", .. segments]);
        File.Exists(path).Should().BeTrue(path);
        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             dir is not null;
             dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine")))
                return dir.FullName;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root");
    }
}
