using System;
using FluentAssertions;
using FluentMigrator;
using Nop.Data.Migrations;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class FitmentAndImportInfrastructureConventionsTests
{
    [Test]
    public void FitmentSchemaMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.FitmentSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }

    [Test]
    public void ImportPipelineSchemaMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.ImportPipelineSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }

    [Test]
    public void ImportPipelineDurableStateMigration_Should_Be_Installation_Migration_With_Explicit_Down()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.ImportPipelineDurableStateMigration);
        migrationType.IsSubclassOf(typeof(Migration)).Should().BeTrue();
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeFalse();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
        migrationType.GetMethod(nameof(Migration.Down))!.DeclaringType.Should().Be(migrationType);
    }

    [Test]
    public void ProductOemMapSchemaMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.ProductOemMapSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }

    [Test]
    public void FitmentInfrastructure_Should_Expose_Sql_Repository()
    {
        typeof(TwinParticles.CheckEngine.Infrastructure.Fitment.SqlFitmentClaimRepository).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Infrastructure.Fitment.SqlFitmentClaimRepository)
            .Should().BeAssignableTo<TwinParticles.CheckEngine.Domain.Fitment.IFitmentClaimReadRepository>();
        typeof(TwinParticles.CheckEngine.Infrastructure.Fitment.SqlFitmentClaimRepository)
            .Should().BeAssignableTo<TwinParticles.CheckEngine.Domain.Fitment.IFitmentClaimWriteRepository>();
    }

    [Test]
    public void ImportInfrastructure_Should_Expose_Sql_Repository()
    {
        typeof(TwinParticles.CheckEngine.Infrastructure.ImportPipeline.SqlImportPipelineRepository).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Infrastructure.ImportPipeline.SqlImportPipelineRepository)
            .Should().BeAssignableTo<TwinParticles.CheckEngine.Domain.ImportPipeline.IImportPipelineRepository>();
    }

    [Test]
    public void SqlImportPipelineRepository_Should_Bracket_SqlServer_Reserved_RowCount_Column()
    {
        // SQL Server rejects unquoted RowCount (error 156) in SELECT/INSERT/UPDATE column lists.
        var path = System.IO.Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "Plugins", "TwinParticles.CheckEngine.Infrastructure", "ImportPipeline", "SqlImportPipelineRepository.cs");
        path = System.IO.Path.GetFullPath(path);
        System.IO.File.Exists(path).Should().BeTrue();

        var source = System.IO.File.ReadAllText(path);
        source.Should().Contain("CheckEngineSql.QuoteIdentifier(\"RowCount\")");
        source.Should().NotContain(", RowCount, ErrorSummary");
        source.Should().NotContain("    RowCount = @rowCount");
        source.Should().NotContain("`RowCount`");
        source.Should().NotContain("[RowCount]");
    }

    [Test]
    public void OemInfrastructure_Should_Expose_ProductOemMap_Sql_Repository()
    {
        typeof(TwinParticles.CheckEngine.Infrastructure.Oem.SqlProductOemMapRepository).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Infrastructure.Oem.SqlProductOemMapRepository)
            .Should().BeAssignableTo<TwinParticles.CheckEngine.Domain.Oem.IProductOemMapRepository>();
    }

    [Test]
    public void GarageInfrastructure_Should_Expose_Sql_Repository()
    {
        typeof(TwinParticles.CheckEngine.Infrastructure.Garage.SqlGarageRepository).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Infrastructure.Garage.SqlGarageRepository)
            .Should().BeAssignableTo<TwinParticles.CheckEngine.Domain.Garage.IGarageRepository>();
    }

    [Test]
    public void SeoLandingSchemaMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.SeoLandingSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }

    [Test]
    public void ErpSyncSchemaMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.ErpSyncSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }

    [Test]
    public void ProductImageMetaSchemaMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.ProductImageMetaSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }

    [Test]
    public void FitmentReviewQueueEventSchemaMigration_Should_Be_Installation_AutoReversing_Migration()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.FitmentReviewQueueEventSchemaMigration);
        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();

        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }

    [Test]
    public void SeoInfrastructure_Should_Expose_Sql_Repository()
    {
        typeof(TwinParticles.CheckEngine.Infrastructure.Seo.SqlSeoLandingRepository).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Infrastructure.Seo.SqlSeoLandingRepository)
            .Should().BeAssignableTo<TwinParticles.CheckEngine.Domain.Seo.ISeoLandingRepository>();
    }

    [Test]
    public void ErpInfrastructure_Should_Expose_Sql_Repository()
    {
        typeof(TwinParticles.CheckEngine.Infrastructure.Erp.SqlErpSyncQueueRepository).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Infrastructure.Erp.SqlErpSyncQueueRepository)
            .Should().BeAssignableTo<TwinParticles.CheckEngine.Domain.Erp.IErpSyncQueueRepository>();
    }

    [Test]
    public void ImageInfrastructure_Should_Expose_Sql_Repository()
    {
        typeof(TwinParticles.CheckEngine.Infrastructure.Images.SqlProductImageRepository).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Infrastructure.Images.SqlProductImageRepository)
            .Should().BeAssignableTo<TwinParticles.CheckEngine.Domain.Images.IProductImageRepository>();
    }

    [Test]
    public void FitmentInfrastructure_Should_Expose_ReviewQueue_Sql_Repository()
    {
        typeof(TwinParticles.CheckEngine.Infrastructure.Fitment.SqlFitmentReviewQueueRepository).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Infrastructure.Fitment.SqlFitmentReviewQueueRepository)
            .Should().BeAssignableTo<TwinParticles.CheckEngine.Domain.Fitment.IFitmentReviewQueueRepository>();
    }
}
