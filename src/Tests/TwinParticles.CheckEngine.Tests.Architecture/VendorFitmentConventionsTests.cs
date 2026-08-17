using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VendorFitmentConventionsTests
{
    [Test]
    public void Fitment_Schema_Should_Add_VendorId_Column()
    {
        var migration = ReadInfrastructureFile("Migrations", "202608171730_FitmentClaimVendorId.cs");
        migration.Should().Contain("VendorId");
        migration.Should().Contain("TP_CE_FitmentClaim");
    }

    [Test]
    public void Fitment_Claim_Model_Should_Expose_VendorId()
    {
        var source = ReadDomainFile("Fitment", "FitmentClaim.cs");
        source.Should().Contain("VendorId");
    }

    [Test]
    public void Vendor_Should_Expose_Submit_And_Revoke_Fitment_Proposals()
    {
        var vendor = ReadPluginFile("Controllers", "VendorController.cs");
        vendor.Should().Contain("SubmitFitmentProposal");
        vendor.Should().Contain("RevokeFitmentProposal");
    }

    [Test]
    public void Fitment_Admin_Should_Expose_Submit_And_Revoke_For_Operator()
    {
        var admin = ReadPluginFile("Controllers", "FitmentAdminController.cs");
        admin.Should().Contain("Revoke");
        admin.Should().Contain("Submit");
        admin.Should().Contain("VendorFitmentContributionService");
        admin.Should().Contain("VendorId");
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
