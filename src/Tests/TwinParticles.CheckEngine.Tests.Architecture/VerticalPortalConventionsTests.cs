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
}
