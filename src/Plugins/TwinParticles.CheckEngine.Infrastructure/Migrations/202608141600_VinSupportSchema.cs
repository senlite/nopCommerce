using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-14 16:00:00", "TwinParticles.CheckEngine VIN WMI and pattern schema", MigrationProcessType.Installation)]
public sealed class VinSupportSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_VinWmi")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Wmi").AsFixedLengthString(3).NotNullable()
            .WithColumn("MakeId").AsInt32().Nullable()
            .WithColumn("ManufacturerName").AsString(128).NotNullable()
            .WithColumn("RegionCode").AsString(16).Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable();

        Create.Index("IX_TP_CE_VinWmi_Wmi")
            .OnTable("TP_CE_VinWmi")
            .OnColumn("Wmi").Ascending()
            .WithOptions().Unique();

        Create.Table("TP_CE_VinPattern")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("MakeId").AsInt32().NotNullable()
            .WithColumn("Pattern").AsString(32).NotNullable()
            .WithColumn("Priority").AsInt32().NotNullable()
            .WithColumn("ModelCode").AsString(16).NotNullable()
            .WithColumn("GenerationCode").AsString(16).NotNullable()
            .WithColumn("EngineCode").AsString(32).Nullable()
            .WithColumn("TrimSlug").AsString(128).Nullable()
            .WithColumn("Confidence").AsDecimal(5, 4).NotNullable()
            .WithColumn("Provenance").AsString(512).NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable();

        Create.Index("IX_TP_CE_VinPattern_Pattern_Priority")
            .OnTable("TP_CE_VinPattern")
            .OnColumn("Pattern").Ascending()
            .OnColumn("Priority").Descending();
    }
}
