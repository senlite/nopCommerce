using System;
using FluentAssertions;
using FluentMigrator;
using Nop.Data.Migrations;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class OemFoundationConventionsTests
{
    [Test]
    public void Manufacturer_Should_Define_Core_Properties()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.Oem.Manufacturer);

        type.GetProperty("Id").Should().NotBeNull();
        type.GetProperty("Code").Should().NotBeNull();
        type.GetProperty("Name").Should().NotBeNull();
        type.GetProperty("IsOeBrand").Should().NotBeNull();
        type.GetProperty("IsActive").Should().NotBeNull();
    }

    [Test]
    public void OemNumber_Should_Define_Core_Properties()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.Oem.OemNumber);

        type.GetProperty("Id").Should().NotBeNull();
        type.GetProperty("ManufacturerId").Should().NotBeNull();
        type.GetProperty("DisplayNumber").Should().NotBeNull();
        type.GetProperty("NormalizedNumber").Should().NotBeNull();
        type.GetProperty("IsObsolete").Should().NotBeNull();
        type.GetProperty("IsActive").Should().NotBeNull();
    }

    [Test]
    public void OemRelation_Should_Define_Core_Properties()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.Oem.OemRelation);

        type.GetProperty("Id").Should().NotBeNull();
        type.GetProperty("FromOemNumberId").Should().NotBeNull();
        type.GetProperty("ToOemNumberId").Should().NotBeNull();
        type.GetProperty("RelationType").Should().NotBeNull();
        type.GetProperty("ValidFromUtc").Should().NotBeNull();
        type.GetProperty("ValidToUtc").Should().NotBeNull();
        type.GetProperty("IsActive").Should().NotBeNull();
    }

    [Test]
    public void OemRelationType_Should_Define_Expected_Values()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.Oem.OemRelationType);

        Enum.IsDefined(type, "CrossReference").Should().BeTrue();
        Enum.IsDefined(type, "Equivalent").Should().BeTrue();
        Enum.IsDefined(type, "Alternate").Should().BeTrue();
        Enum.IsDefined(type, "Supersession").Should().BeTrue();
        Enum.IsDefined(type, "KitMember").Should().BeTrue();
    }

    [Test]
    public void OemRegistryMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.OemRegistrySchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }

    [Test]
    public void OemRelationMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.OemRelationSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }
}
