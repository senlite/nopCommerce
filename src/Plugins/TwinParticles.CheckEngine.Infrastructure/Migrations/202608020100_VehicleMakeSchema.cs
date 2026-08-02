using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-02 01:00:00", "TwinParticles.CheckEngine vehicle make schema", MigrationProcessType.Installation)]
public sealed class VehicleMakeSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_VehicleMake")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Code").AsString(32).NotNullable()
            .WithColumn("Name").AsString(128).NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable();
    }
}
