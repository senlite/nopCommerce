using System.IO;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class GarageVinDisambiguationContractTests
{
    [Test]
    public void Garage_Controller_Should_Enrich_409_Candidates_With_Configuration_Labels()
    {
        var controller = ReadPluginFile(
            "TwinParticles.CheckEngine", "Controllers", "GarageController.cs");

        controller.Should().Contain("VehicleAdminService");
        controller.Should().Contain("GetConfigurationDisplayLabelsAsync");
        controller.Should().Contain("vehicleConfigurationId = candidate.VehicleConfigurationId");
        controller.Should().Contain("label = labels.TryGetValue");
    }

    [Test]
    public void Vin_Controller_Should_Enrich_Disambiguation_Candidates_With_Configuration_Labels()
    {
        var controller = ReadPluginFile(
            "TwinParticles.CheckEngine", "Controllers", "VinController.cs");

        controller.Should().Contain("NeedsDisambiguation");
        controller.Should().Contain("GetConfigurationDisplayLabelsAsync");
        controller.Should().Contain("vehicleConfigurationId = candidate.VehicleConfigurationId");
    }

    [Test]
    public void Vehicle_Admin_Service_Should_Resolve_Configuration_Display_Labels()
    {
        var service = typeof(VehicleAdminService);

        service.GetMethod("GetConfigurationDisplayLabelAsync").Should().NotBeNull();
        service.GetMethod("GetConfigurationDisplayLabelsAsync").Should().NotBeNull();
    }

    [Test]
    public void Configuration_Display_Labels_Should_Qualify_With_Market_Name_And_Years()
    {
        var source = ReadApplicationFile("Vehicle", "Admin", "VehicleAdminService.cs");

        source.Should().Contain("configuration.MarketId");
        source.Should().Contain("GetMarketByIdAsync");
        source.Should().Contain("market.Name");
        source.Should().Contain("FormatProductionWindow");
        source.Should().Contain("ProductionFromYear");
        source.Should().Contain("ProductionToYear");
    }

    [Test]
    public void Storefront_Should_Handle_409_And_Guest_Vin_Decode_Disambiguation()
    {
        var storefront = ReadPluginFile(
            "TwinParticles.CheckEngine", "Content", "checkengine-storefront.js");
        var chrome = ReadPluginFile(
            "TwinParticles.CheckEngine", "Views", "Shared", "Components", "CheckEngineThemeChrome", "Default.cshtml");

        storefront.Should().Contain("response.status === 409");
        storefront.Should().Contain("showVinDisambiguationPicker");
        storefront.Should().Contain("/check-engine/vin/decode");
        storefront.Should().Contain("NeedsDisambiguation");
        storefront.Should().Contain("vehicleConfigurationId");
        storefront.Should().Contain("vinDisplayLabel");
        storefront.Should().Contain("needsDisambiguation");
        storefront.Should().NotContain("persistGuest(null");
        chrome.Should().Contain("ce-vin-disambiguation");
        chrome.Should().Contain("Garage.VinDisambiguation.Title");
    }

    private static string ReadPluginFile(string project, params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", project, .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {project}/{string.Join('/', relativePath)}");
    }

    private static string ReadApplicationFile(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine.Application", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate Application/{string.Join('/', relativePath)}");
    }
}
