using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-02 01:10:00", "TwinParticles.CheckEngine vehicle model schema", MigrationProcessType.Installation)]
public sealed class VehicleModelSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_VehicleModel")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("MakeId").AsInt32().NotNullable().ForeignKey("TP_CE_VehicleMake", "Id")
            .WithColumn("Code").AsString(32).NotNullable()
            .WithColumn("Name").AsString(128).NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable();
    }
}
