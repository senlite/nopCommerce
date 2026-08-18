using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class CommissionConventionsTests
{
    [Test]
    public void Commission_Schema_Should_Define_Plans_Rules_And_Snapshots()
    {
        var migration = ReadInfrastructureFile("Migrations", "202608171500_CommissionSchema.cs");
        migration.Should().Contain("TP_CE_CommissionPlan");
        migration.Should().Contain("TP_CE_CommissionRule");
        migration.Should().Contain("TP_CE_CommissionTierBand");
        migration.Should().Contain("TP_CE_OrderLineCommissionSnapshot");
    }

    [Test]
    public void Order_Placed_Should_Snapshot_Commissions_And_Split_Vendors()
    {
        var consumer = ReadPluginFile("Consumers", "MarketplaceOrderPlacedConsumer.cs");
        consumer.Should().Contain("OrderPlacedEvent");
        consumer.Should().Contain("CommissionSnapshotService");
        consumer.Should().Contain("OrderVendorSplitService");
    }

    [Test]
    public void Commission_Admin_Should_Expose_Plan_Configuration()
    {
        var controller = ReadPluginFile("Controllers", "CommissionAdminController.cs");
        var routes = ReadPluginFile("Infrastructure", "RouteProvider.cs");
        controller.Should().Contain("SavePlan");
        controller.Should().Contain("GetPlan");
        routes.Should().Contain("CommissionAdmin");
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
}
