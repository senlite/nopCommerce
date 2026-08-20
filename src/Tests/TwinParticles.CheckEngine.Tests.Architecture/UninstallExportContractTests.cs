using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class UninstallExportContractTests
{
    [Test]
    public void Export_Should_Contain_Vehicle_Oem_And_Provenanced_Fitment_Data()
    {
        var model = ReadPluginFile(
            "TwinParticles.CheckEngine.Application", "Observability", "CheckEngineUninstallExport.cs");
        var service = ReadPluginFile(
            "TwinParticles.CheckEngine.Application", "Observability", "CheckEngineUninstallExportService.cs");
        var fitment = ReadPluginFile(
            "TwinParticles.CheckEngine.Infrastructure", "Fitment", "SqlFitmentClaimRepository.cs");

        model.Should().Contain("VehicleConfiguration");
        model.Should().Contain("VehicleAlias");
        model.Should().Contain("Manufacturer");
        model.Should().Contain("OemNumber");
        model.Should().Contain("OemRelation");
        model.Should().Contain("FitmentClaim");
        service.Should().Contain("GetAllClaimsAsync");
        fitment.Should().Contain("public async Task<IReadOnlyList<FitmentClaim>> GetAllClaimsAsync");
        fitment.Should().Contain("q.OptionCodesCsv");
        fitment.Should().Contain("c.SourceReference");
        fitment.Should().Contain("c.CreatedBy");
    }

    [Test]
    public void Uninstall_Should_Be_Blocked_Until_A_Fresh_Export_Was_Downloaded()
    {
        var plugin = ReadPluginFile(
            "TwinParticles.CheckEngine", "CheckEnginePlugin.cs");
        var controller = ReadPluginFile(
            "TwinParticles.CheckEngine", "Controllers", "UninstallAdminController.cs");
        var route = ReadPluginFile(
            "TwinParticles.CheckEngine", "Infrastructure", "RouteProvider.cs");

        var guardIndex = plugin.IndexOf("UninstallExportPreparedUtc", System.StringComparison.Ordinal);
        var dropIndex = plugin.IndexOf("ApplyDownMigrations", System.StringComparison.Ordinal);
        guardIndex.Should().BeGreaterThan(0);
        guardIndex.Should().BeLessThan(dropIndex, "export guard must run before any destructive uninstall step");
        plugin.Should().Contain("TimeSpan.FromHours(24)");
        plugin.Should().Contain("uninstall blocked");
        controller.Should().Contain("CheckEngineUninstallExportService");
        controller.Should().Contain("File(bytes, \"application/json\", filename)");
        controller.Should().Contain("UninstallExportPreparedUtc = DateTime.UtcNow");
        route.Should().Contain("Admin/CheckEngine/UninstallAdmin/{action?}");
    }

    [Test]
    public void Uninstall_Status_Should_Explain_Data_Loss_And_Offer_Export()
    {
        var controller = ReadPluginFile(
            "TwinParticles.CheckEngine", "Controllers", "UninstallAdminController.cs");

        controller.Should().Contain("Uninstall permanently deletes Check Engine vehicle, OEM, fitment, garage, analytics, audit and integration data.");
        controller.Should().Contain("exportRequired = true");
        controller.Should().Contain("exportUrl");
        controller.Should().Contain("expiresUtc");
    }

    [Test]
    public void Uninstall_Should_Integrate_With_Plugin_List_And_Configure_Surfaces()
    {
        var plugin = ReadPluginFile("TwinParticles.CheckEngine", "CheckEnginePlugin.cs");
        var component = ReadPluginFile(
            "TwinParticles.CheckEngine", "Components", "UninstallPreparationViewComponent.cs");
        var view = ReadPluginFile(
            "TwinParticles.CheckEngine", "Views", "Shared", "Components", "UninstallPreparation", "Default.cshtml");
        var configure = ReadPluginFile("TwinParticles.CheckEngine", "Views", "Configure.cshtml");
        var dashboard = ReadPluginFile("TwinParticles.CheckEngine", "Views", "Admin", "Dashboard.cshtml");

        plugin.Should().Contain("AdminWidgetZones.PluginListButtons");
        plugin.Should().Contain("typeof(Components.UninstallPreparationViewComponent)");
        component.Should().Contain("CheckEnginePermissionProvider.ManageCheckEngine");
        component.Should().Contain("UninstallPreparationModel");
        view.Should().Contain("ce-uninstall-confirm-modal");
        view.Should().Contain("uninstall-plugin-link-");
        view.Should().Contain("TwinParticles.CheckEngine");
        configure.Should().Contain("UninstallPreparationViewComponent");
        dashboard.Should().Contain("UninstallPreparationViewComponent");
        plugin.Should().Contain("Plugins.TwinParticles.CheckEngine.Uninstall.ConfirmTitle");
    }

    private static string ReadPluginFile(string project, params string[] relativePath)
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             dir is not null;
             dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", project, .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {project}/{string.Join('/', relativePath)}");
    }
}
