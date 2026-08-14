using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// The fitment band can only evaluate a part if it knows which product it is on. The band
/// previously shipped with an empty product id, which made it inert on every product page.
/// </summary>
[TestFixture]
public class FitmentBandProductBindingTests
{
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

    [Test]
    public void Theme_Chrome_Should_Expose_The_Product_Id_To_The_Band()
    {
        var view = ReadPluginFile("Views", "Shared", "Components", "CheckEngineThemeChrome", "Default.cshtml");

        view.Should().Contain("data-ce-product-id=\"@(Model.ProductId",
            "the band must be bound to the product being viewed");
        view.Should().NotContain("data-ce-product-id=\"\"",
            "an empty product id leaves the band permanently in the select-vehicle state");
    }

    [Test]
    public void View_Component_Should_Read_The_Product_From_The_Widget_Context()
    {
        var component = ReadPluginFile("Components", "CheckEngineThemeChromeViewComponent.cs");

        component.Should().Contain("ProductDetailsModel",
            "the product detail widget zones supply the page model");
        component.Should().Contain("ProductId",
            "the resolved product id must reach the view model");
    }

    [Test]
    public void Client_Should_Retain_A_Fallback_For_Reading_The_Product_Id()
    {
        var script = ReadPluginFile("Content", "checkengine-storefront.js");

        script.Should().Contain("data-ce-product-id", "the server-rendered id is the primary source");
        script.Should().Contain("addtocart_",
            "a theme-level fallback keeps the band working if the zone renders without an id");
        script.Should().Contain("driveType");
        script.Should().Contain("unmatched");
        script.Should().Contain("populateVehicleSelector().then");
    }
}
