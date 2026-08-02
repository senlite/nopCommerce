using System;
using FluentMigrator;
using FluentAssertions;
using Nop.Data.Migrations;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VehicleFoundationConventionsTests
{
    [Test]
    public void VehicleMake_Should_Define_Core_Properties()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.Vehicle.VehicleMake);

        type.GetProperty("Id").Should().NotBeNull();
        type.GetProperty("Code").Should().NotBeNull();
        type.GetProperty("Name").Should().NotBeNull();
        type.GetProperty("IsActive").Should().NotBeNull();
    }

    [Test]
    public void VehicleMakeMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.VehicleMakeSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }

    [Test]
    public void VehicleModel_Should_Define_Core_Properties()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.Vehicle.VehicleModel);

        type.GetProperty("Id").Should().NotBeNull();
        type.GetProperty("MakeId").Should().NotBeNull();
        type.GetProperty("Code").Should().NotBeNull();
        type.GetProperty("Name").Should().NotBeNull();
        type.GetProperty("IsActive").Should().NotBeNull();
    }

    [Test]
    public void VehicleModelMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.VehicleModelSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }
}
