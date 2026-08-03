using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-02 01:30:00", "TwinParticles.CheckEngine vehicle body schema", MigrationProcessType.Installation)]
public sealed class VehicleBodySchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_VehicleBody")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("GenerationId").AsInt32().NotNullable().ForeignKey("TP_CE_VehicleGeneration", "Id")
            .WithColumn("Code").AsString(32).NotNullable()
            .WithColumn("Name").AsString(128).NotNullable()
            .WithColumn("Doors").AsInt32().NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable();
    }
}
