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
        chrome.Should().Contain("checkengine-fonts.css");
        chrome.Should().NotContain("fonts.googleapis.com");
    }

    [Test]
    public void Plugin_Should_Self_Host_Ibm_Plex_And_Outfit()
    {
        var fontsCss = ReadPluginFile("Content", "checkengine-fonts.css");
        fontsCss.Should().Contain("@font-face");
        fontsCss.Should().Contain("IBM Plex Sans");
        fontsCss.Should().Contain("IBM Plex Sans Arabic");
        fontsCss.Should().Contain("IBM Plex Mono");
        fontsCss.Should().Contain("Outfit");
        fontsCss.Should().NotContain("fonts.googleapis.com");

        var required = new[]
        {
            "outfit-latin-600.woff2",
            "outfit-latin-700.woff2",
            "ibm-plex-sans-latin-400.woff2",
            "ibm-plex-sans-latin-500.woff2",
            "ibm-plex-sans-latin-600.woff2",
            "ibm-plex-sans-arabic-400.woff2",
            "ibm-plex-sans-arabic-600.woff2",
            "ibm-plex-mono-latin-400.woff2",
            "ibm-plex-mono-latin-500.woff2"
        };
        foreach (var file in required)
        {
            File.Exists(Path.Combine(FindRepositoryRoot(), "src", "Plugins", "TwinParticles.CheckEngine", "Content", "fonts", file))
                .Should().BeTrue(file);
        }

        ReadPluginFile("Views", "Shared", "_PortalAssets.cshtml").Should().NotContain("fonts.googleapis.com");
        ReadPluginFile("Views", "Shared", "_MarketplaceAssets.cshtml").Should().NotContain("fonts.googleapis.com");
        ReadPluginFile("Views", "Shared", "_PortalAssets.cshtml").Should().Contain("checkengine-fonts.css");
        ReadThemeFile("Views", "Shared", "Head.cshtml").Should().Contain("checkengine-fonts.css");
        ReadPluginFile("TwinParticles.CheckEngine.csproj").Should().Contain("Content\\fonts\\**\\*");
    }

    [Test]
    public void Theme_Should_Own_Category_Product_And_Cart_Templates()
    {
        var category = ReadThemeFile("Views", "Catalog", "CategoryTemplate.ProductsInGridOrLines.cshtml");
        var search = ReadThemeFile("Views", "Catalog", "Search.cshtml");
        var simple = ReadThemeFile("Views", "Product", "ProductTemplate.Simple.cshtml");
        var grouped = ReadThemeFile("Views", "Product", "ProductTemplate.Grouped.cshtml");
        var cart = ReadThemeFile("Views", "ShoppingCart", "Cart.cshtml");
        var css = ReadThemeFile("Content", "css", "styles.css");

        category.Should().Contain("ce-catalog");
        category.Should().Contain("Theme.CatalogHint");
        search.Should().Contain("ce-catalog-search");
        cart.Should().Contain("ce-cart");

        foreach (var pdp in new[] { simple, grouped })
        {
            pdp.Should().Contain("ce-pdp");
            pdp.Should().Contain("Theme.Aftermarket");
            var overview = pdp.IndexOf("class=\"overview\"", System.StringComparison.Ordinal);
            var fitment = pdp.IndexOf("PublicWidgetZones.ProductDetailsTop", System.StringComparison.Ordinal);
            overview.Should().BeGreaterThan(0);
            fitment.Should().BeGreaterThan(overview);
        }

        css.Should().Contain(".ce-catalog");
        css.Should().Contain(".ce-pdp");
        css.Should().Contain(".ce-cart");
        css.Should().Contain(".ce-pdp__aftermarket");
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
