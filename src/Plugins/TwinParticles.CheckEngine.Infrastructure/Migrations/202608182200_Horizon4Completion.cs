using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-18 22:00:00", "TwinParticles.CheckEngine Horizon 4 completion (fleet spend, warranty OEM)", MigrationProcessType.Installation)]
public sealed class Horizon4Completion : Migration
{
    public override void Up()
    {
        if (!Schema.Table("TP_CE_FleetVehicleSpend").Exists())
        {
            Create.Table("TP_CE_FleetVehicleSpend")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("FleetAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_FleetAccount", "Id")
                .WithColumn("FleetVehicleId").AsInt32().NotNullable().ForeignKey("TP_CE_FleetVehicle", "Id")
                .WithColumn("ProductId").AsInt32().NotNullable()
                .WithColumn("OrderId").AsInt32().Nullable()
                .WithColumn("Amount").AsDecimal(18, 4).NotNullable()
                .WithColumn("RecordedUtc").AsDateTime2().NotNullable();

            Create.Index("IX_TP_CE_FleetVehicleSpend_Vehicle")
                .OnTable("TP_CE_FleetVehicleSpend")
                .OnColumn("FleetVehicleId").Ascending();
        }

        if (!Schema.Table("TP_CE_WarrantyClaim").Column("ResolvedOemNumberId").Exists())
        {
            Alter.Table("TP_CE_WarrantyClaim")
                .AddColumn("ResolvedOemNumberId").AsInt32().Nullable();
        }
    }

    public override void Down()
    {
    }
}
