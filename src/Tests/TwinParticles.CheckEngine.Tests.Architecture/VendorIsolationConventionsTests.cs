using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VendorIsolationConventionsTests
{
    [Test]
    public void Vendor_Apis_Should_Authorize_Catalog_Orders_And_Customers()
    {
        var controller = ReadPluginFile("Controllers", "VendorController.cs");
        var admin = ReadPluginFile("Controllers", "VendorAdminController.cs");
        var routes = ReadPluginFile("Infrastructure", "RouteProvider.cs");

        controller.Should().Contain("EditProduct");
        controller.Should().Contain("AuthorizeProductAsync");
        controller.Should().Contain("AuthorizeOrderAsync");
        controller.Should().Contain("AuthorizeCustomerAsync");
        controller.Should().Contain("VendorErrorCodes.IsolationDenied");

        admin.Should().Contain("EnableMarketplace");
        admin.Should().Contain("AssignProduct");

        routes.Should().Contain("check-engine/vendor/{action}");
    }

    [Test]
    public void Order_Sql_Should_Quote_Reserved_Order_Identifier()
    {
        var store = ReadInfrastructureFile("Marketplace", "SqlVendorOrderReadStore.cs");

        store.Should().Contain("CheckEngineSql.QuoteIdentifier(\"Order\")");
        store.Should().NotContain("FROM [Order]");
        store.Should().Contain("OrderItem");
    }

    [Test]
    public void Configure_Should_Run_Upgrade_When_Marketplace_Is_Enabled()
    {
        var configure = ReadPluginFile("Controllers", "CheckEngineController.cs");
        configure.Should().Contain("MarketplaceUpgradeService");
        configure.Should().Contain("model.EnableMarketplace");
        configure.Should().Contain("EnableAsync");
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
