using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VendorOnboardingConventionsTests
{
    [Test]
    public void Admin_And_Apply_Routes_Should_Gate_On_Marketplace_Entitlement()
    {
        var admin = ReadPluginFile("Controllers", "VendorAdminController.cs");
        var apply = ReadPluginFile("Controllers", "VendorController.cs");
        var routes = ReadPluginFile("Infrastructure", "RouteProvider.cs");

        admin.Should().Contain("MarketplaceLicenceGate");
        admin.Should().Contain("VendorErrorCodes.LicenceDenied");
        admin.Should().Contain("ReviewBoard");
        admin.Should().Contain("Views/Admin/VendorReview.cshtml");

        apply.Should().Contain("MarketplaceLicenceGate");
        apply.Should().Contain("ApplicationsOpen");
        apply.Should().Contain("AcceptAgreement");
        apply.Should().Contain("AuthorizeOnboardingAccessAsync");
        apply.Should().Contain("CanAccessApplicationAsync");
        apply.Should().Contain("accessToken");
        apply.Should().Contain("ApplicantAccessToken");
        apply.Should().NotContain("IgnoreAntiforgeryToken");
        apply.Should().NotContain("BankingSecretProtected");

        routes.Should().Contain("Admin/CheckEngine/VendorAdmin/{action}");
        routes.Should().Contain("area = AreaNames.ADMIN");
        routes.Should().Contain("check-engine/vendor/{action}");
    }

    [Test]
    public void Sql_Vendor_Repository_Should_Never_Select_Banking_Ciphertext()
    {
        var repository = ReadInfrastructureFile("Marketplace", "SqlVendorRepository.cs");

        repository.Should().Contain("CheckEngineSql.SelectInsertedIntId");
        repository.Should().Contain("CheckEngineSql.SelectTop");
        repository.Should().Contain("HasBankingDetails");
        repository.Should().NotContain("SELECT BankingSecretProtected");
        repository.Should().Contain("BankingSecretProtected = null");
        repository.Should().Contain("ApplicantAccessTokenHash");
        repository.Should().Contain("GetApplicantAccessTokenHashAsync");
    }

    [Test]
    public void Review_View_Should_Not_Render_Banking_Fields()
    {
        var view = ReadPluginFile("Views", "Admin", "VendorReview.cshtml");

        view.Should().Contain("HasBankingDetails");
        view.Should().NotContain("bankingDetails");
        view.Should().NotContain("BankingSecret");
        view.Should().Contain("/Admin/CheckEngine/VendorAdmin/Queue");
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
