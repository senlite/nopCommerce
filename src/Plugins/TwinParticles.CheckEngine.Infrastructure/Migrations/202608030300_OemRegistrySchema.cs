using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-03 03:00:00", "TwinParticles.CheckEngine OEM registry schema", MigrationProcessType.Installation)]
public sealed class OemRegistrySchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_Manufacturer")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Code").AsString(64).NotNullable()
            .WithColumn("Name").AsString(128).NotNullable()
            .WithColumn("IsOeBrand").AsBoolean().NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable();

        Create.Index("IX_TP_CE_Manufacturer_Code")
            .OnTable("TP_CE_Manufacturer")
            .OnColumn("Code").Ascending()
            .WithOptions().Unique();

        Create.Table("TP_CE_OemNumber")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("ManufacturerId").AsInt32().NotNullable().ForeignKey("TP_CE_Manufacturer", "Id")
            .WithColumn("DisplayNumber").AsString(128).NotNullable()
            .WithColumn("NormalizedNumber").AsString(128).NotNullable()
            .WithColumn("IsObsolete").AsBoolean().NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable();

        Create.Index("IX_TP_CE_OemNumber_ManufacturerId_NormalizedNumber")
            .OnTable("TP_CE_OemNumber")
            .OnColumn("ManufacturerId").Ascending()
            .OnColumn("NormalizedNumber").Ascending()
            .WithOptions().Unique();
    }
}
