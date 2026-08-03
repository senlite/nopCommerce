using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-02 01:50:00", "TwinParticles.CheckEngine vehicle market schema", MigrationProcessType.Installation)]
public sealed class VehicleMarketSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_VehicleMarket")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Code").AsString(32).NotNullable()
            .WithColumn("Name").AsString(128).NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable();
    }
}
