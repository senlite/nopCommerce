using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-02 01:20:00", "TwinParticles.CheckEngine vehicle generation schema", MigrationProcessType.Installation)]
public sealed class VehicleGenerationSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_VehicleGeneration")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("ModelId").AsInt32().NotNullable().ForeignKey("TP_CE_VehicleModel", "Id")
            .WithColumn("Code").AsString(32).NotNullable()
            .WithColumn("Name").AsString(128).NotNullable()
            .WithColumn("StartYear").AsInt32().NotNullable()
            .WithColumn("EndYear").AsInt32().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable();
    }
}
