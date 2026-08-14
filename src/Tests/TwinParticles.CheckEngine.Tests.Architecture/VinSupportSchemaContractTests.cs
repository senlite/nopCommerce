using System;
using System.IO;
using FluentAssertions;
using FluentMigrator;
using Nop.Data.Migrations;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VinSupportSchemaContractTests
{
    [Test]
    public void Migration_Should_Create_Vin_Wmi_And_Pattern_Tables()
    {
        var type = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.VinSupportSchemaMigration);
        type.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(type, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);

        var source = ReadInfrastructureFile("Migrations", "202608141600_VinSupportSchema.cs");
        source.Should().Contain("TP_CE_VinWmi");
        source.Should().Contain("TP_CE_VinPattern");
        source.Should().Contain("IX_TP_CE_VinWmi_Wmi");
    }

    [Test]
    public void Bmw_Decoder_Should_Load_Patterns_From_Repository_Not_Hardcoded_Map()
    {
        var decoder = ReadInfrastructureFile("Vehicle", "Vin", "BmwVinDecoder.cs");
        decoder.Should().Contain("IVinSupportRepository");
        decoder.Should().Contain("BmwVinConfigurationResolver");
        decoder.Should().NotContain("VehicleConfigurationId = 10041");
        decoder.Should().NotContain("VdsPatternCandidates");
    }

    private static string ReadInfrastructureFile(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine.Infrastructure", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate Infrastructure/{string.Join('/', relativePath)}");
    }
}
