using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class FigmaUxReviewConventionsTests
{
    [Test]
    public void Storefront_Should_Open_A_Garage_Sheet_Instead_Of_Window_Prompt()
    {
        var script = ReadPluginFile("Content", "checkengine-storefront.js");
        var chrome = ReadPluginFile("Views", "Shared", "Components", "CheckEngineThemeChrome", "Default.cshtml");

        script.Should().NotContain("window.prompt");
        script.Should().NotContain("window.confirm");
        script.Should().Contain("function openGarageSheet");
        script.Should().Contain("function closeGarageSheet");
        script.Should().Contain("function bindGarageSheet");
        script.Should().Contain("VIN …");
        script.Should().Contain("ce-garage-remove-confirm");

        chrome.Should().Contain("id=\"ce-garage-sheet\"");
        chrome.Should().Contain("aria-controls=\"ce-garage-sheet\"");
        chrome.Should().Contain("aria-haspopup=\"dialog\"");
        chrome.Should().Contain("ce-visually-hidden");
        chrome.Should().Contain("id=\"ce-garage-add-form\"");
        chrome.Should().Contain("id=\"ce-garage-vin-preview\"");
        chrome.Should().Contain("ce-search__more");
        chrome.Should().Contain("Search.MoreFilters");
    }

    [Test]
    public void Theme_Tokens_Should_Cover_Pill_Radius_Mega_Shadow_And_Host_Search_Hide()
    {
        var tokens = ReadPluginFile("Content", "checkengine-tokens.css");
        var theme = ReadPluginFile("Content", "checkengine-theme.css");

        tokens.Should().Contain("--ce-radius-pill: 999px;");
        theme.Should().Contain("box-shadow: var(--ce-shadow-raised);");
        theme.Should().Contain(":has(.ce-rail)");
        theme.Should().Contain(".header-lower .search-box");
        theme.Should().Contain(".ce-search__more");
        theme.Should().Contain(".ce-garage-form");
    }

    [Test]
    public void Admin_Should_Deep_Link_Order_Inspector_And_Keep_Uninstall_Below_Portals()
    {
        var dashboard = ReadPluginFile("Views", "Admin", "Dashboard.cshtml");
        var scoreboard = ReadPluginFile("Views", "Admin", "VendorScoreboard.cshtml");

        dashboard.Should().Contain("/Admin/CheckEngine/VendorAdmin/Scoreboard#ce-order-inspector");
        scoreboard.Should().Contain("id=\"ce-order-inspector\"");

        var portals = dashboard.IndexOf("Dashboard.Group.Portals", System.StringComparison.Ordinal);
        var uninstall = dashboard.IndexOf("UninstallPreparationViewComponent", System.StringComparison.Ordinal);
        portals.Should().BeGreaterThan(0);
        uninstall.Should().BeGreaterThan(portals);
    }

    [Test]
    public void Garage_Sheet_Locales_Should_Exist_In_English_And_Arabic()
    {
        var plugin = ReadPluginFile("CheckEnginePlugin.cs");
        var keys = new[]
        {
            "Plugins.TwinParticles.CheckEngine.Garage.Sheet.Title",
            "Plugins.TwinParticles.CheckEngine.Garage.Sheet.Lead",
            "Plugins.TwinParticles.CheckEngine.Garage.Sheet.Close",
            "Plugins.TwinParticles.CheckEngine.Garage.VinLabel",
            "Plugins.TwinParticles.CheckEngine.Garage.VinPreview",
            "Plugins.TwinParticles.CheckEngine.Garage.AddSubmit",
            "Plugins.TwinParticles.CheckEngine.Garage.Remove",
            "Plugins.TwinParticles.CheckEngine.Garage.RemoveConfirm",
            "Plugins.TwinParticles.CheckEngine.Search.MoreFilters"
        };

        foreach (var key in keys)
        {
            plugin.Should().Contain($"[\"{key}\"]");
            plugin.Split($"[\"{key}\"]").Length.Should().Be(3, $"EN and AR should both define {key}");
        }
    }

    private static string ReadPluginFile(params string[] segments)
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "Plugins", "TwinParticles.CheckEngine",
            Path.Combine(segments));
        path = Path.GetFullPath(path);
        File.Exists(path).Should().BeTrue(path);
        return File.ReadAllText(path);
    }
}
