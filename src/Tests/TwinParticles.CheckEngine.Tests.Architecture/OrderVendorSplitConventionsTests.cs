using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class OrderVendorSplitConventionsTests
{
    [Test]
    public void Split_Schema_Should_Define_Splits_Lines_And_Shipment_Map()
    {
        var migration = ReadInfrastructureFile("Migrations", "202608171700_OrderVendorSplitSchema.cs");
        migration.Should().Contain("TP_CE_OrderVendorSplit");
        migration.Should().Contain("TP_CE_OrderVendorSplitLine");
        migration.Should().Contain("TP_CE_ShipmentVendorMap");
    }

    [Test]
    public void Order_Placed_Should_Create_Vendor_Splits()
    {
        var consumer = ReadPluginFile("Consumers", "MarketplaceOrderPlacedConsumer.cs");
        consumer.Should().Contain("OrderPlacedEvent");
        consumer.Should().Contain("OrderVendorSplitService");
    }

    [Test]
    public void Shipment_Created_Should_Map_Vendors()
    {
        var consumer = ReadPluginFile("Consumers", "MarketplaceShipmentCreatedConsumer.cs");
        consumer.Should().Contain("ShipmentCreatedEvent");
        consumer.Should().Contain("MapShipmentAsync");
    }

    [Test]
    public void Vendor_And_Admin_Should_Expose_Order_Splits()
    {
        var vendor = ReadPluginFile("Controllers", "VendorController.cs");
        var admin = ReadPluginFile("Controllers", "VendorAdminController.cs");
        vendor.Should().Contain("OrderSplits");
        admin.Should().Contain("OrderSplits");
    }

    [Test]
    public void Split_Persist_Should_Use_A_Real_Database_Transaction()
    {
        var store = ReadInfrastructureFile("Marketplace", "SqlOrderVendorSplitStore.cs");
        var sql = ReadInfrastructureFile("Data", "CheckEngineSql.cs");
        store.Should().Contain("ExecuteInTransactionAsync");
        store.Should().Contain("SaveCheckoutGroupAsync");
        store.Should().Contain("SaveShipmentVendorMapsAsync");
        sql.Should().Contain("BeginTransactionAsync");
        sql.Should().Contain("CreateDataConnection");
        sql.Should().NotContain("INopDataProvider");
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
