using System.IO;
using FluentAssertions;
using FluentMigrator;
using Nop.Data.Migrations;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class GaragePrivacyContractTests
{
    [Test]
    public void Sql_Repository_Should_Protect_Vin_At_Persistence_Boundary()
    {
        var repository = ReadPluginFile(
            "TwinParticles.CheckEngine.Infrastructure", "Garage", "SqlGarageRepository.cs");

        repository.Should().Contain("_vinProtector.Protect(vehicle.Vin)");
        repository.Should().Contain("_vinProtector.Unprotect(storedVin)");
        repository.Should().NotContain("new DataParameter(\"vin\", vehicle.Vin)");
    }

    [Test]
    public void Garage_Controller_Should_Expose_Protected_Export_And_Erasure()
    {
        var controller = ReadPluginFile(
            "TwinParticles.CheckEngine", "Controllers", "GarageController.cs");

        controller.Should().Contain("[AutoValidateAntiforgeryToken]");
        controller.Should().Contain("IActionResult> Export(");
        controller.Should().Contain("IActionResult> Erase(");
        controller.Should().Contain("garage.erase_not_confirmed");
    }

    [Test]
    public void Permanent_Customer_Deletion_Should_Erase_Garage_Data()
    {
        var consumer = ReadPluginFile(
            "TwinParticles.CheckEngine", "Consumers", "GarageCustomerDeletedConsumer.cs");

        consumer.Should().Contain("IConsumer<CustomerPermanentlyDeleted>");
        consumer.Should().Contain("_privacyService.EraseAsync");
        consumer.Should().NotContain("Vin");
    }

    [Test]
    public void Garage_Vin_Encryption_Migration_Should_Expand_Storage_Without_Destructive_Shrink()
    {
        var type = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.GarageVinEncryptionMigration);
        type.IsSubclassOf(typeof(Migration)).Should().BeTrue();
        type.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeFalse();

        var attribute = System.Attribute.GetCustomAttribute(type, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);

        var source = ReadPluginFile(
            "TwinParticles.CheckEngine.Infrastructure", "Migrations", "202608131205_GarageVinEncryption.cs");
        source.Should().Contain(".AsString(512)");
        source.Should().NotContain(".AsString(64)");
    }

    [Test]
    public void Ambiguous_Vin_Should_Never_Silently_Select_First_Garage_Candidate()
    {
        var service = ReadPluginFile(
            "TwinParticles.CheckEngine.Application", "Garage", "GarageService.cs");
        var controller = ReadPluginFile(
            "TwinParticles.CheckEngine", "Controllers", "GarageController.cs");

        service.Should().Contain("\"NeedsDisambiguation\"");
        service.Should().Contain("throw new GarageVinDisambiguationException(decode.Candidates)");
        service.Should().Contain("decode.Candidates.Count == 1");
        service.Should().NotContain("resolvedConfigurationId ??= decode.Candidates[0]");
        controller.Should().Contain("GarageVinDisambiguationException");
        controller.Should().Contain("return Conflict");
        controller.Should().Contain("candidates = exception.Candidates.Select");
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
}
