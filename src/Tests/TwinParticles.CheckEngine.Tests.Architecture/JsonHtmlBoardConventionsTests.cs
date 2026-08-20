using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class JsonHtmlBoardConventionsTests
{
    [Test]
    public void Remaining_Admin_Json_Gets_Should_Redirect_Browsers_To_Html_Boards()
    {
        AiAdmin().Should().Contain("WantsJsonResponse");
        AiAdmin().Should().Contain("PageOrJsonApi(nameof(Dashboard))");
        AiAdmin().Should().Contain("PageOrJsonApi(nameof(ReviewBoard))");

        SearchAdmin().Should().Contain("WantsJsonResponse");
        SearchAdmin().Should().Contain("RedirectAdmin(\"SearchAdmin\", \"Index\")");

        Diagnostics().Should().Contain("Views/Admin/DiagnosticsAdmin.cshtml");
        Diagnostics().Should().Contain("WantsJsonResponse");

        ReferenceData().Should().Contain("Views/Admin/ReferenceDataAdmin.cshtml");
        ReferenceData().Should().Contain("WantsJsonResponse");

        ImageAdmin().Should().Contain("Views/Admin/ImageAdmin.cshtml");

        Uninstall().Should().Contain("WantsJsonResponse");
        Uninstall().Should().Contain("RedirectAdmin(\"CheckEngine\", \"Dashboard\")");
    }

    [Test]
    public void Admin_Js_Should_Boot_New_Html_Boards()
    {
        var js = ReadPluginFile("Content", "checkengine-admin.js");
        js.Should().Contain("diagnostics-admin");
        js.Should().Contain("reference-admin");
        js.Should().Contain("image-admin");
        js.Should().Contain("DiagnosticsAdmin/Package");
        js.Should().Contain("ReferenceDataAdmin/Status");
        js.Should().Contain("ImageAdmin/Replace");
        js.Should().Contain("data-ce-garage-vehicles");
        js.Should().Contain("data-ce-erp-variances");
        js.Should().Contain("data-ce-import-rows");
        js.Should().Contain("data-ce-image-manifest-rows");
        js.Should().NotContain("[data-ce-garage-result]");
        js.Should().NotContain("[data-ce-erp-result]");
        js.Should().NotContain("[data-ce-import-result]");
        js.Should().Contain("SeoAdmin/GenerateVehicle");
        js.Should().Contain("ErpAdmin/InventorySnapshot");
        js.Should().Contain("ImageAdmin/SourceFromTemplate");
        js.Should().Contain("urlTemplate");
        js.Should().Contain("data-ce-image-template-url");
        js.Should().Contain("ImportAdmin/Publish");
        js.Should().Contain("ImportAdmin/Run");
        js.Should().Contain("data-ce-import-run");
        js.Should().Contain("SetDuplicateDecision");
        js.Should().Contain("data-ce-erp-snapshot-stats");
    }

    [Test]
    public void Remaining_Operator_Apis_Should_Have_Html_Boards_And_Browser_Redirects()
    {
        var marketplaceJs = ReadPluginFile("Content", "checkengine-marketplace.js");
        marketplaceJs.Should().Contain("data-ce-order-splits");
        marketplaceJs.Should().Contain("VendorAdmin/OrderSplits");
        marketplaceJs.Should().Contain("CommissionAdmin/OrderSnapshots");

        ReadPluginFile("Controllers", "VendorAdminController.cs")
            .Should().Contain("RedirectAdmin(\"VendorAdmin\", \"Scoreboard\"");
        ReadPluginFile("Controllers", "CommissionAdminController.cs")
            .Should().Contain("RedirectAdmin(\"VendorAdmin\", \"Scoreboard\"");
        ReadPluginFile("Infrastructure", "CheckEnginePaths.cs")
            .Should().Contain("/Admin/CheckEngine/");
        ReadPluginFile("Controllers", "VehicleAdminController.cs")
            .Should().Contain("RedirectAdmin(\"VehicleAdmin\"");
        ReadPluginFile("Controllers", "VendorController.cs")
            .Should().Contain("RedirectPortal(\"vendor\"");
        ReadPluginFile("Controllers", "SeoAdminController.cs")
            .Should().Contain("seo.generate_failed");
        ReadPluginFile("Controllers", "ImageAdminController.cs")
            .Should().Contain("RedirectAdmin(\"ImageAdmin\", \"Index\")");
        ReadPluginFile("Views", "Admin", "ImageAdmin.cshtml")
            .Should().Contain("data-ce-image-template-url");
        ReadPluginFile("Views", "Admin", "Dashboard.cshtml")
            .Should().Contain("href=\"/Admin/CheckEngine/VehicleAdmin/Index\"");
        ReadPluginFile("Views", "Admin", "Dashboard.cshtml")
            .Should().NotContain("href=\"/Admin/CheckEngine/VehicleAdmin\">");
        ReadPluginFile("Views", "Admin", "Dashboard.cshtml")
            .Should().Contain("Dashboard.Group.Catalog");
        ReadPluginFile("Infrastructure", "RouteProvider.cs")
            .Should().Contain("VehicleAdmin/{action}");
        ReadPluginFile("Infrastructure", "RouteProvider.cs")
            .Should().NotContain("{action?}");
        ReadPluginFile("Content", "checkengine-admin.js")
            .Should().Contain("imageStatusLabel");
        ReadPluginFile("Views", "Admin", "Dashboard.cshtml")
            .Should().Contain("<option value=\"1\">");
        ReadPluginFile("Views", "Admin", "Dashboard.cshtml")
            .Should().Contain("ImportUpload.Format.Csv");
        ReadPluginFile("Views", "Admin", "Dashboard.cshtml")
            .Should().NotContain("<option value=\"0\">");
        ReadPluginFile("Views", "Admin", "Dashboard.cshtml")
            .Should().NotContain("CSV / Excel / PDF file");
        ReadPluginFile("Views", "Admin", "VehicleAdmin.cshtml")
            .Should().Contain("data-ce-vehicle-seed");
        ReadPluginFile("Content", "checkengine-admin.js")
            .Should().Contain("VehicleAdmin/Seed");
        ReadPluginFile("Content", "checkengine-admin.js")
            .Should().Contain("licence.read_only");
        ReadPluginFile("Content", "checkengine-admin.js")
            .Should().Contain("if (!res.ok)");
        ReadPluginFile("Consumers", "AdminMenuCreatedEventConsumer.cs")
            .Should().Contain("BaseAdminMenuCreatedEventConsumer");
        ReadPluginFile("Consumers", "AdminMenuCreatedEventConsumer.cs")
            .Should().Contain("/Admin/CheckEngine/VehicleAdmin/Index");
        ReadPluginFile("Consumers", "AdminMenuCreatedEventConsumer.cs")
            .Should().Contain("AfterMenuSystemName => \"Configuration\"");
        ReadPluginFile("Consumers", "AdminMenuCreatedEventConsumer.cs")
            .Should().Contain("Dashboard.Group.Catalog");
        ReadPluginFile("Consumers", "AdminMenuCreatedEventConsumer.cs")
            .Should().Contain("/Admin/CheckEngine/OemAdmin/Index");
        ReadPluginFile("Consumers", "AdminMenuCreatedEventConsumer.cs")
            .Should().Contain("/Admin/CheckEngine/SeoAdmin/Index");
        ReadPluginFile("Consumers", "AdminMenuCreatedEventConsumer.cs")
            .Should().Contain("/Admin/CheckEngine/GarageAdmin/Index");
        ReadPluginFile("Consumers", "AdminMenuCreatedEventConsumer.cs")
            .Should().Contain("private async Task<AdminMenuItem> Group");
        ReadPluginFile("Views", "Admin", "VehicleAdmin.cshtml")
            .Should().Contain("TwinParticles.CheckEngine.VehicleAdmin");
        ReadPluginFile("Views", "Admin", "OemAdmin.cshtml")
            .Should().Contain("TwinParticles.CheckEngine.OemAdmin");
        ReadPluginFile("Views", "Admin", "PortalAccounts.cshtml")
            .Should().NotContain("SetActiveMenuItemSystemName(\"CheckEngine\")");
        ReadPluginFile("Content", "checkengine-marketplace.css")
            .Should().Contain(".ce-marketplace-admin");
        ReadPluginFile("Content", "checkengine-marketplace.css")
            .Should().Contain("--ce-text-primary: var(--ce-raw-graphite-900)");
        ReadPluginFile("Configuration", "CheckEnginePluginSettings.cs")
            .Should().Contain("SupplierImageUrlTemplate");
        ReadPluginFile("Views", "Shared", "_CheckEngineAdminAssets.cshtml")
            .Should().Contain("checkengine-admin.js?v=");
        ReadPluginFile("Views", "Shared", "_CheckEngineAdminAssets.cshtml")
            .Should().Contain("checkengine-marketplace.css?v=");
        ReadPluginFile("Views", "Shared", "_CheckEngineAdminAssets.cshtml")
            .Should().Contain("AddHeadCustomParts");
        ReadPluginFile("Views", "Shared", "_CheckEngineAdminAssets.cshtml")
            .Should().Contain("LicenceReadOnlyBannerViewComponent");
        ReadPluginFile("Views", "Shared", "_MarketplaceAdminAssets.cshtml")
            .Should().Contain("LicenceReadOnlyBannerViewComponent");
        ReadPluginFile("Views", "Admin", "PortalAccounts.cshtml")
            .Should().Contain("LicenceReadOnlyBannerViewComponent");
        ReadPluginFile("Views", "Shared", "_PortalAssets.cshtml")
            .Should().NotContain("LicenceReadOnlyBannerViewComponent");
        ReadPluginFile("Views", "Configure.cshtml")
            .Should().Contain("LicenceReadOnlyBannerViewComponent");
        ReadPluginFile("Components", "LicenceReadOnlyBannerViewComponent.cs")
            .Should().Contain("AllowsAdminWriteAsync");
        ReadPluginFile("Views", "Shared", "Components", "LicenceReadOnlyBanner", "Default.cshtml")
            .Should().Contain("data-ce-licence-readonly-banner");
        ReadPluginFile("Content", "checkengine-admin.js")
            .Should().Contain("errorMessage(res.body, 'Template failed.')");
        ReadPluginFile("Content", "checkengine-admin.js")
            .Should().Contain("errorMessage(res.body, 'Publish failed.')");
        ReadPluginFile("Views", "Workshop", "Index.cshtml")
            .Should().NotContain("LicenceReadOnlyBannerViewComponent");
        ReadPluginFile("Views", "Admin", "Dashboard.cshtml")
            .Should().Contain("importError");
        ReadPluginFile("Content", "checkengine-marketplace.js")
            .Should().Contain("licence.read_only");
        ReadPluginFile("Content", "checkengine-portals.js")
            .Should().Contain("function adminError");
        ReadPluginFile("Views", "Admin", "SearchAdmin.cshtml")
            .Should().Contain("_CheckEngineAdminAssets");
        ReadPluginFile("Views", "Admin", "SearchAdmin.cshtml")
            .Should().Contain("CheckEngineAdmin.errorMessage");
        ReadPluginFile("Views", "Admin", "SearchAdmin.cshtml")
            .Should().Contain("Keyword rebuild failed.");
        ReadPluginFile("Views", "Admin", "GlossaryAdmin.cshtml")
            .Should().Contain("_CheckEngineAdminAssets");
        ReadPluginFile("Views", "Admin", "GlossaryAdmin.cshtml")
            .Should().Contain("CheckEngineAdmin.errorMessage");
        ReadPluginFile("Views", "Admin", "SpecKeysAdmin.cshtml")
            .Should().Contain("_CheckEngineAdminAssets");
        ReadPluginFile("Views", "Admin", "AiDashboard.cshtml")
            .Should().Contain("_CheckEngineAdminAssets");
        ReadPluginFile("Views", "Admin", "AiReview.cshtml")
            .Should().Contain("CheckEngineAdmin.errorMessage");
        ReadPluginFile("Views", "Admin", "FitmentAiReview.cshtml")
            .Should().Contain("CheckEngineAdmin.errorMessage");
        ReadPluginFile("Views", "Shared", "_CheckEngineAdminAssets.cshtml")
            .Should().Contain("CheckEngineAdmin.errorMessage");
        ReadPluginFile("Views", "Shared", "_LicencePanel.cshtml")
            .Should().Contain("Licence.Panel.LastHeartbeat");
        ReadPluginFile("Controllers", "GarageController.cs")
            .Should().Contain("WantsJsonResponse");
        ReadPluginFile("Controllers", "GarageController.cs")
            .Should().Contain("Redirect(\"/\")");
    }

    [Test]
    public void Admin_Boards_Should_Not_Dump_Json_Into_Pre_Tags()
    {
        ReadPluginFile("Content", "checkengine-admin.js").Should().NotContain("panel.textContent = JSON.stringify");
        ReadPluginFile("Content", "checkengine-licence-admin.js").Should().Contain("data-ce-licence-state");
        ReadPluginFile("Content", "checkengine-licence-admin.js").Should().NotContain("statusEl.textContent = JSON.stringify");
        ReadPluginFile("Content", "checkengine-marketplace.js").Should().Contain("data-ce-payout-recon-succeeded");
        ReadPluginFile("Content", "checkengine-marketplace.js").Should().NotContain("recon.textContent = JSON.stringify");
        ReadPluginFile("Views", "Admin", "Dashboard.cshtml").Should().NotContain("<pre");
        ReadPluginFile("Views", "Admin", "FitmentAiReview.cshtml").Should().NotContain("<pre");
        ReadPluginFile("Views", "Admin", "PayoutAdmin.cshtml").Should().NotContain("<pre");
        ReadPluginFile("Views", "Shared", "_LicencePanel.cshtml").Should().Contain("data-ce-licence-state");
        ReadPluginFile("Views", "Shared", "_LicencePanel.cshtml").Should().NotContain("<pre");
        ReadPluginFile("Views", "Shared", "_LicencePanel.cshtml").Should().Contain("checkengine-licence-admin.js?v=");
    }

    private static string AiAdmin() => ReadPluginFile("Controllers", "AiAdminController.cs");
    private static string SearchAdmin() => ReadPluginFile("Controllers", "SearchAdminController.cs");
    private static string Diagnostics() => ReadPluginFile("Controllers", "DiagnosticsAdminController.cs");
    private static string ReferenceData() => ReadPluginFile("Controllers", "ReferenceDataAdminController.cs");
    private static string ImageAdmin() => ReadPluginFile("Controllers", "ImageAdminController.cs");
    private static string Uninstall() => ReadPluginFile("Controllers", "UninstallAdminController.cs");

    private static string ReadPluginFile(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate TwinParticles.CheckEngine/{string.Join('/', relativePath)}");
    }
}
