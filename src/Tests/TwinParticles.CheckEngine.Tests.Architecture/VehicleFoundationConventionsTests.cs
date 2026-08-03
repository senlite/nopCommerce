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

    [Test]
    public void VehicleGeneration_Should_Define_Core_Properties()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.Vehicle.VehicleGeneration);

        type.GetProperty("Id").Should().NotBeNull();
        type.GetProperty("ModelId").Should().NotBeNull();
        type.GetProperty("Code").Should().NotBeNull();
        type.GetProperty("Name").Should().NotBeNull();
        type.GetProperty("StartYear").Should().NotBeNull();
        type.GetProperty("EndYear").Should().NotBeNull();
        type.GetProperty("IsActive").Should().NotBeNull();
    }

    [Test]
    public void VehicleGenerationMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.VehicleGenerationSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }

    [Test]
    public void VehicleBody_Should_Define_Core_Properties()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.Vehicle.VehicleBody);

        type.GetProperty("Id").Should().NotBeNull();
        type.GetProperty("GenerationId").Should().NotBeNull();
        type.GetProperty("Code").Should().NotBeNull();
        type.GetProperty("Name").Should().NotBeNull();
        type.GetProperty("Doors").Should().NotBeNull();
        type.GetProperty("IsActive").Should().NotBeNull();
    }

    [Test]
    public void VehicleBodyMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.VehicleBodySchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }

    [Test]
    public void VehicleEngine_Should_Define_Core_Properties()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.Vehicle.VehicleEngine);

        type.GetProperty("Id").Should().NotBeNull();
        type.GetProperty("BodyId").Should().NotBeNull();
        type.GetProperty("Code").Should().NotBeNull();
        type.GetProperty("Name").Should().NotBeNull();
        type.GetProperty("FuelType").Should().NotBeNull();
        type.GetProperty("DisplacementCc").Should().NotBeNull();
        type.GetProperty("PowerHp").Should().NotBeNull();
        type.GetProperty("IsActive").Should().NotBeNull();
    }

    [Test]
    public void VehicleEngineMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.VehicleEngineSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }

    [Test]
    public void VehicleMarket_Should_Define_Core_Properties()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.Vehicle.VehicleMarket);

        type.GetProperty("Id").Should().NotBeNull();
        type.GetProperty("Code").Should().NotBeNull();
        type.GetProperty("Name").Should().NotBeNull();
        type.GetProperty("IsActive").Should().NotBeNull();
    }

    [Test]
    public void VehicleMarketMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.VehicleMarketSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }

    [Test]
    public void VehicleConfiguration_Should_Define_Core_Properties()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.Vehicle.VehicleConfiguration);

        type.GetProperty("Id").Should().NotBeNull();
        type.GetProperty("GenerationId").Should().NotBeNull();
        type.GetProperty("BodyId").Should().NotBeNull();
        type.GetProperty("EngineId").Should().NotBeNull();
        type.GetProperty("MarketId").Should().NotBeNull();
        type.GetProperty("TrimName").Should().NotBeNull();
        type.GetProperty("ProductionFromYear").Should().NotBeNull();
        type.GetProperty("ProductionToYear").Should().NotBeNull();
        type.GetProperty("Fingerprint").Should().NotBeNull();
        type.GetProperty("IsActive").Should().NotBeNull();
    }

    [Test]
    public void VehicleConfigurationMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.VehicleConfigurationSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }

    [Test]
    public void VehicleAlias_Should_Define_Core_Properties()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.Vehicle.VehicleAlias);

        type.GetProperty("Id").Should().NotBeNull();
        type.GetProperty("NodeType").Should().NotBeNull();
        type.GetProperty("NodeId").Should().NotBeNull();
        type.GetProperty("Locale").Should().NotBeNull();
        type.GetProperty("AliasText").Should().NotBeNull();
        type.GetProperty("NormalizedAlias").Should().NotBeNull();
    }

    [Test]
    public void VehicleAliasMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.VehicleAliasSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }
}
