using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-03 10:00:00", "TwinParticles.CheckEngine fitment schema", MigrationProcessType.Installation)]
public sealed class FitmentSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_FitmentClaim")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("ProductId").AsInt32().NotNullable()
            .WithColumn("VehicleConfigurationId").AsInt32().NotNullable().ForeignKey("TP_CE_VehicleConfiguration", "Id")
            .WithColumn("OemNumberId").AsInt32().Nullable().ForeignKey("TP_CE_OemNumber", "Id")
            .WithColumn("FitmentStatusId").AsInt32().NotNullable()
            .WithColumn("Confidence").AsDecimal(5, 4).NotNullable()
            .WithColumn("SafetyClassId").AsInt32().NotNullable()
            .WithColumn("SourceKindId").AsInt32().NotNullable()
            .WithColumn("SourceReference").AsString(256).NotNullable()
            .WithColumn("CreatedBy").AsString(128).NotNullable()
            .WithColumn("ProvenanceCreatedUtc").AsDateTime2().NotNullable()
            .WithColumn("LastVerifiedUtc").AsDateTime2().Nullable()
            .WithColumn("ValidFromUtc").AsDateTime2().Nullable()
            .WithColumn("ValidToUtc").AsDateTime2().Nullable()
            .WithColumn("IsPublished").AsBoolean().NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable();

        Create.Table("TP_CE_FitmentQualifier")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("FitmentClaimId").AsInt32().NotNullable().ForeignKey("TP_CE_FitmentClaim", "Id")
            .WithColumn("ProductionFromYear").AsInt32().Nullable()
            .WithColumn("ProductionToYear").AsInt32().Nullable()
            .WithColumn("SteeringSide").AsString(64).Nullable()
            .WithColumn("MarketRegion").AsString(64).Nullable()
            .WithColumn("DriveType").AsString(64).Nullable()
            .WithColumn("TransmissionType").AsString(64).Nullable()
            .WithColumn("OptionCodesCsv").AsString(int.MaxValue).Nullable();

        Create.Index("IX_TP_CE_FitmentClaim_ProductId_VehicleConfigurationId")
            .OnTable("TP_CE_FitmentClaim")
            .OnColumn("ProductId").Ascending()
            .OnColumn("VehicleConfigurationId").Ascending()
            .WithOptions().Unique();

        Create.Index("IX_TP_CE_FitmentClaim_VehicleConfigurationId_IsPublished")
            .OnTable("TP_CE_FitmentClaim")
            .OnColumn("VehicleConfigurationId").Ascending()
            .OnColumn("IsPublished").Ascending();

        Create.Index("IX_TP_CE_FitmentClaim_ProductId_IsPublished")
            .OnTable("TP_CE_FitmentClaim")
            .OnColumn("ProductId").Ascending()
            .OnColumn("IsPublished").Ascending();

        Create.Index("IX_TP_CE_FitmentQualifier_FitmentClaimId")
            .OnTable("TP_CE_FitmentQualifier")
            .OnColumn("FitmentClaimId").Ascending()
            .WithOptions().Unique();
    }
}
