using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VerticalPortalConventionsTests
{
    [Test]
    public void Portal_Routes_Should_Register_Workshop_Fleet_And_Dealer()
    {
        var routes = ReadPluginFile("Infrastructure", "RouteProvider.cs");
        routes.Should().Contain("check-engine/workshop/{action}");
        routes.Should().Contain("check-engine/fleet/{action}");
        routes.Should().Contain("check-engine/dealer/{action}");
    }

    [Test]
    public void Portal_Controllers_Should_Gate_On_Licence_Entitlements()
    {
        var workshop = ReadPluginFile("Controllers", "WorkshopController.cs");
        var fleet = ReadPluginFile("Controllers", "FleetController.cs");
        var dealer = ReadPluginFile("Controllers", "DealerController.cs");

        workshop.Should().Contain("WorkshopPortalLicenceGate");
        fleet.Should().Contain("FleetPortalLicenceGate");
        dealer.Should().Contain("DealerPortalLicenceGate");
    }

    [Test]
    public void Portal_Controllers_Should_Use_Account_Based_Access()
    {
        var workshop = ReadPluginFile("Controllers", "WorkshopController.cs");
        var fleet = ReadPluginFile("Controllers", "FleetController.cs");
        var dealer = ReadPluginFile("Controllers", "DealerController.cs");

        workshop.Should().Contain("VerticalPortalAccessService");
        fleet.Should().Contain("VerticalPortalAccessService");
        dealer.Should().Contain("VerticalPortalAccessService");
        workshop.Should().Contain("PortalErrorCodes.AccessUnauthenticated");
    }

    [Test]
    public void Portal_Services_Should_Call_Fitment_Evaluation_Not_Storage()
    {
        var workshop = ReadApplicationFile("Workshop", "WorkshopJobService.cs");
        var fleet = ReadApplicationFile("Fleet", "FleetPortalService.cs");

        workshop.Should().Contain("FitmentEvaluationService");
        workshop.Should().NotContain("IFitmentClaimReadRepository");
        fleet.Should().Contain("FitmentEvaluationService");
        fleet.Should().NotContain("IFitmentClaimReadRepository");
        fleet.Should().Contain("VinDecodeApplicationService");
    }

    [Test]
    public void Portal_Permissions_Should_Exist_For_Each_Vertical()
    {
        var permissions = ReadInfrastructureFile("Security", "CheckEnginePermissionProvider.cs");
        permissions.Should().Contain("ManageCheckEngineWorkshop");
        permissions.Should().Contain("ManageCheckEngineFleet");
        permissions.Should().Contain("ManageCheckEngineDealer");
    }

    [Test]
    public void Vertical_Portal_Schema_Should_Create_Core_Tables()
    {
        var migration = ReadInfrastructureFile("Migrations", "202608181200_VerticalPortalSchema.cs");
        migration.Should().Contain("TP_CE_WorkshopJob");
        migration.Should().Contain("TP_CE_FleetVehicle");
        migration.Should().Contain("TP_CE_DealerAllocation");
        migration.Should().Contain("TP_CE_WarrantyClaim");
    }

    [Test]
    public void Licence_Status_Should_Expose_Portal_Entitlements()
    {
        var status = ReadDomainFile("Licensing", "LicenceStatus.cs");
        status.Should().Contain("WorkshopPortalEntitlement");
        status.Should().Contain("FleetPortalEntitlement");
        status.Should().Contain("DealerPortalEntitlement");
    }

    private static string ReadPluginFile(params string[] segments)
    {
        var path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "Plugins", "TwinParticles.CheckEngine",
            Path.Combine(segments)));
        File.Exists(path).Should().BeTrue(path);
        return File.ReadAllText(path);
    }

    private static string ReadApplicationFile(params string[] segments)
    {
        var path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "Plugins", "TwinParticles.CheckEngine.Application",
            Path.Combine(segments)));
        File.Exists(path).Should().BeTrue(path);
        return File.ReadAllText(path);
    }

    private static string ReadInfrastructureFile(params string[] segments)
    {
        var path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "Plugins", "TwinParticles.CheckEngine.Infrastructure",
            Path.Combine(segments)));
        File.Exists(path).Should().BeTrue(path);
        return File.ReadAllText(path);
    }

    private static string ReadDomainFile(params string[] segments)
    {
        var path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "Plugins", "TwinParticles.CheckEngine.Domain",
            Path.Combine(segments)));
        File.Exists(path).Should().BeTrue(path);
        return File.ReadAllText(path);
    }

    [Test]
    public void Portal_Pages_Should_Use_Design_System_Assets()
    {
        var workshop = ReadPluginFile("Views", "Workshop", "Index.cshtml");
        var fleet = ReadPluginFile("Views", "Fleet", "Index.cshtml");
        var dealer = ReadPluginFile("Views", "Dealer", "Index.cshtml");
        var admin = ReadPluginFile("Views", "Admin", "PortalAccounts.cshtml");
        var portalsJs = ReadPluginFile("Content", "checkengine-portals.js");

        workshop.Should().Contain("_PortalAssets.cshtml");
        workshop.Should().Contain("data-ce-page=\"workshop\"");
        fleet.Should().Contain("data-ce-page=\"fleet\"");
        dealer.Should().Contain("data-ce-page=\"dealer\"");
        admin.Should().Contain("data-ce-page=\"portal-admin\"");

        portalsJs.Should().Contain("initWorkshop");
        portalsJs.Should().Contain("initFleet");
        portalsJs.Should().Contain("initDealer");
        portalsJs.Should().Contain("/check-engine/workshop/DashboardData");
    }

    [Test]
    public void Configure_Page_Should_Include_Licence_Panel()
    {
        var configure = ReadPluginFile("Views", "Configure.cshtml");
        configure.Should().Contain("_LicencePanel.cshtml");
        configure.Should().Contain("AntiForgeryToken");
    }

    [Test]
    public void Licence_Admin_Assets_Should_Exist()
    {
        var js = ReadPluginFile("Content", "checkengine-licence-admin.js");
        js.Should().Contain("DiagnosticsAdmin/Activate");
        js.Should().Contain("data-ce-licence-panel");
    }
    [Test]
    public void Portal_Admin_Should_Provision_All_Three_Account_Types()
    {
        var controller = ReadPluginFile("Controllers", "PortalAdminController.cs");
        controller.Should().Contain("ProvisionWorkshop");
        controller.Should().Contain("ProvisionFleet");
        controller.Should().Contain("ProvisionDealer");
    }

    [Test]
    public void Horizon4_v067_Should_Expose_Job_Detail_Allocation_And_Fleet_Forecast()
    {
        var workshop = ReadPluginFile("Controllers", "WorkshopController.cs");
        workshop.Should().Contain("JobDetail");
        workshop.Should().Contain("AllocateJobLine");

        var fleet = ReadPluginFile("Controllers", "FleetController.cs");
        fleet.Should().Contain("MaintenanceForecast");

        var fleetService = ReadApplicationFile("Fleet", "FleetPortalService.cs");
        fleetService.Should().Contain("RefreshMaintenanceForecastsForVehicleAsync");
        fleetService.Should().Contain("SeedDefaultMaintenanceSchedulesAsync");

        var migration = ReadInfrastructureFile("Migrations", "202608182100_FleetMaintenanceForecast.cs");
        migration.Should().Contain("TP_CE_FleetMaintenanceSchedule");
        migration.Should().Contain("TP_CE_FleetMaintenanceForecast");

        var portalsJs = ReadPluginFile("Content", "checkengine-portals.js");
        portalsJs.Should().Contain("/check-engine/workshop/JobDetail");
        portalsJs.Should().Contain("AllocateJobLine");
        portalsJs.Should().Contain("maintenanceForecasts");
        portalsJs.Should().Contain("SeedDealerAllocation");
    }

    [Test]
    public void Portal_Admin_Should_Seed_Dealer_Allocation_And_Quota()
    {
        var controller = ReadPluginFile("Controllers", "PortalAdminController.cs");
        controller.Should().Contain("SeedDealerAllocation");
        controller.Should().Contain("SeedDealerQuota");

        var admin = ReadPluginFile("Views", "Admin", "PortalAccounts.cshtml");
        admin.Should().Contain("data-ce-admin-seed-allocation");
        admin.Should().Contain("data-ce-admin-seed-quota");
    }

    [Test]
    public void Horizon4_v068_Should_Close_Remaining_Portal_Gaps()
    {
        var bridge = ReadInfrastructureFile("Portals", "NopPortalOrderBridge.cs");
        bridge.Should().Contain("OrderPlacedEvent");

        var fleet = ReadApplicationFile("Fleet", "FleetPortalService.cs");
        fleet.Should().Contain("InsertVehicleSpendAsync");
        fleet.Should().Contain("PortalTradePricingService");

        var dealer = ReadApplicationFile("Dealer", "DealerPortalService.cs");
        dealer.Should().Contain("OemResolveService");
        dealer.Should().Contain("GetCatalogViewAsync");

        var workshop = ReadApplicationFile("Workshop", "WorkshopJobService.cs");
        workshop.Should().Contain("UpdateAccountAsync");
        workshop.Should().Contain("supplementaryLabourFee");

        var migration = ReadInfrastructureFile("Migrations", "202608182200_Horizon4Completion.cs");
        migration.Should().Contain("TP_CE_FleetVehicleSpend");

        var portalsJs = ReadPluginFile("Content", "checkengine-portals.js");
        portalsJs.Should().Contain("SubmitApprovalRequest");
        portalsJs.Should().Contain("vehicleCostSummaries");
        portalsJs.Should().Contain("SeedDealerFranchise");
    }

    [Test]
    public void Horizon4_Final_Should_Add_Customers_Roles_Territory_And_Split_Invoice()
    {
        var migration = ReadInfrastructureFile("Migrations", "202608182300_Horizon4Final.cs");
        migration.Should().Contain("TP_CE_WorkshopCustomer");
        migration.Should().Contain("TP_CE_FleetMember");
        migration.Should().Contain("TP_CE_DealerTerritory");

        var dealer = ReadApplicationFile("Dealer", "DealerPortalService.cs");
        dealer.Should().Contain("FitmentBlocked");
        dealer.Should().Contain("TerritoryDenied");

        var access = ReadApplicationFile("Portals", "VerticalPortalAccessService.cs");
        access.Should().Contain("CanApproveFleetAsync");
        access.Should().Contain("CanRaiseWorkshopInvoiceAsync");

        var workshop = ReadApplicationFile("Workshop", "WorkshopJobService.cs");
        workshop.Should().Contain("CreateWorkshopCustomerAsync");
        workshop.Should().Contain("InvoicedOrderId");
    }

    [Test]
    public void Horizon4_v069_Should_Expose_Portal_Ui_And_Permissions()
    {
        var workshopController = ReadPluginFile("Controllers", "WorkshopController.cs");
        workshopController.Should().Contain("ManageCheckEngineWorkshop");

        var fleetController = ReadPluginFile("Controllers", "FleetController.cs");
        fleetController.Should().Contain("ManageCheckEngineFleet");

        var dealerController = ReadPluginFile("Controllers", "DealerController.cs");
        dealerController.Should().Contain("ManageCheckEngineDealer");

        var admin = ReadPluginFile("Views", "Admin", "PortalAccounts.cshtml");
        admin.Should().Contain("data-ce-admin-seed-technician");
        admin.Should().Contain("data-ce-admin-seed-fleet-approver");
        admin.Should().Contain("data-ce-admin-seed-territory");

        var workshopView = ReadPluginFile("Views", "Workshop", "Index.cshtml");
        workshopView.Should().Contain("data-ce-workshop-customers");

        var portalsJs = ReadPluginFile("Content", "checkengine-portals.js");
        portalsJs.Should().Contain("CreateWorkshopCustomer");
        portalsJs.Should().Contain("SeedFleetApprover");
        portalsJs.Should().Contain("vehicleConfigurationId");
        portalsJs.Should().Contain("data-ce-workshop-invoice-vehicle");
    }

    [Test]
    public void Horizon4_v070_Should_Add_Service_History_And_Front_Desk()
    {
        var migration = ReadInfrastructureFile("Migrations", "202608191000_Horizon4WorkshopRoles.cs");
        migration.Should().Contain("IsFrontDesk");

        var access = ReadApplicationFile("Portals", "VerticalPortalAccessService.cs");
        access.Should().Contain("ResolveWorkshopCapabilitiesAsync");
        access.Should().Contain("CanAssignWorkshopTechnicianAsync");

        var workshop = ReadApplicationFile("Workshop", "WorkshopJobService.cs");
        workshop.Should().Contain("GetServiceHistoryAsync");

        var portalsJs = ReadPluginFile("Content", "checkengine-portals.js");
        portalsJs.Should().Contain("ServiceHistory");
        portalsJs.Should().Contain("AssignTechnician");
        portalsJs.Should().Contain("isFrontDesk");
    }

    [Test]
    public void Horizon4_v071_Should_Add_Export_Credit_Statements_And_Technician_Scoping()
    {
        var migration = ReadInfrastructureFile("Migrations", "202608191200_Horizon4WorkshopCompletion.cs");
        migration.Should().Contain("TP_CE_WorkshopLabourRate");
        migration.Should().Contain("TP_CE_WorkshopCreditStatement");

        var access = ReadApplicationFile("Portals", "VerticalPortalAccessService.cs");
        access.Should().Contain("CanModifyWorkshopJobAsync");
        access.Should().Contain("ResolveTechnicianJobFilterAsync");

        var repository = ReadInfrastructureFile("Workshop", "SqlWorkshopJobRepository.cs");
        repository.Should().Contain("ResolveAccountForPortalUserAsync");

        var workshop = ReadApplicationFile("Workshop", "WorkshopJobService.cs");
        workshop.Should().Contain("ExportCustomerAsync");

        var credit = ReadApplicationFile("Workshop", "WorkshopCreditStatementService.cs");
        credit.Should().Contain("GenerateStatementAsync");

        var portalsJs = ReadPluginFile("Content", "checkengine-portals.js");
        portalsJs.Should().Contain("ExportCustomer");
        portalsJs.Should().Contain("GenerateCreditStatement");
        portalsJs.Should().Contain("operationCode");
    }

    [Test]
    public void Horizon4_v072_Should_Add_Price_Tiers_Fleet_Member_Access_And_Credit_Override()
    {
        var migration = ReadInfrastructureFile("Migrations", "202608191400_Horizon4TradeTiers.cs");
        migration.Should().Contain("TP_CE_WorkshopPriceTier");

        var pricing = ReadApplicationFile("Portals", "PortalTradePricingService.cs");
        pricing.Should().Contain("ListPriceTiersAsync");

        var fleetRepo = ReadInfrastructureFile("Fleet", "SqlFleetPortalRepository.cs");
        fleetRepo.Should().Contain("ResolveAccountForPortalUserAsync");

        var workshop = ReadApplicationFile("Workshop", "WorkshopJobService.cs");
        workshop.Should().Contain("allowCreditOverride");

        var admin = ReadApplicationFile("Portals", "PortalAdminService.cs");
        admin.Should().Contain("SeedWorkshopPriceTierAsync");
    }

    [Test]
    public void Horizon4_v073_Should_Add_Account_Tier_Pricing_Bands()
    {
        var migration = ReadInfrastructureFile("Migrations", "202608191600_Horizon4AccountTiers.cs");
        migration.Should().Contain("TP_CE_WorkshopAccountTier");
        migration.Should().Contain("AccountTierCode");

        var pricing = ReadApplicationFile("Portals", "PortalTradePricingService.cs");
        pricing.Should().Contain("GetAccountTierAsync");

        var admin = ReadApplicationFile("Portals", "PortalAdminService.cs");
        admin.Should().Contain("SeedWorkshopAccountTierAsync");

        var portalsJs = ReadPluginFile("Content", "checkengine-portals.js");
        portalsJs.Should().Contain("SeedWorkshopAccountTier");
    }
}
