using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-02 01:40:00", "TwinParticles.CheckEngine vehicle engine schema", MigrationProcessType.Installation)]
public sealed class VehicleEngineSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_VehicleEngine")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("BodyId").AsInt32().NotNullable().ForeignKey("TP_CE_VehicleBody", "Id")
            .WithColumn("Code").AsString(32).NotNullable()
            .WithColumn("Name").AsString(128).NotNullable()
            .WithColumn("FuelType").AsString(32).NotNullable()
            .WithColumn("DisplacementCc").AsInt32().NotNullable()
            .WithColumn("PowerHp").AsInt32().NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable();
    }
}
