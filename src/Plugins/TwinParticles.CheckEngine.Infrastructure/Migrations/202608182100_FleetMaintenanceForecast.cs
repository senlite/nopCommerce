using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-18 21:00:00", "TwinParticles.CheckEngine fleet maintenance forecast", MigrationProcessType.Installation)]
public sealed class FleetMaintenanceForecast : Migration
{
    public override void Up()
    {
        if (!Schema.Table("TP_CE_FleetVehicle").Column("RegisteredUtc").Exists())
        {
            Alter.Table("TP_CE_FleetVehicle")
                .AddColumn("RegisteredUtc").AsDateTime2().Nullable();
        }

        if (!Schema.Table("TP_CE_FleetMaintenanceSchedule").Exists())
        {
            Create.Table("TP_CE_FleetMaintenanceSchedule")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("FleetAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_FleetAccount", "Id")
                .WithColumn("ServiceLabel").AsString(128).NotNullable()
                .WithColumn("IntervalDays").AsInt32().NotNullable();
        }

        if (!Schema.Table("TP_CE_FleetMaintenanceForecast").Exists())
        {
            Create.Table("TP_CE_FleetMaintenanceForecast")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("FleetVehicleId").AsInt32().NotNullable().ForeignKey("TP_CE_FleetVehicle", "Id")
                .WithColumn("ScheduleId").AsInt32().NotNullable().ForeignKey("TP_CE_FleetMaintenanceSchedule", "Id")
                .WithColumn("ServiceLabel").AsString(128).NotNullable()
                .WithColumn("DueUtc").AsDateTime2().NotNullable()
                .WithColumn("ComputedUtc").AsDateTime2().NotNullable();

            Create.Index("IX_TP_CE_FleetMaintenanceForecast_Vehicle")
                .OnTable("TP_CE_FleetMaintenanceForecast")
                .OnColumn("FleetVehicleId").Ascending();
        }
    }

    public override void Down()
    {
    }
}
