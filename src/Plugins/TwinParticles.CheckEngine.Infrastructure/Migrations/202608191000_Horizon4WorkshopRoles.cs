using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-19 10:00:00", "TwinParticles.CheckEngine Horizon 4 workshop front desk role", MigrationProcessType.Installation)]
public sealed class Horizon4WorkshopRoles : Migration
{
    public override void Up()
    {
        if (Schema.Table("TP_CE_WorkshopTechnician").Exists()
            && !Schema.Table("TP_CE_WorkshopTechnician").Column("IsFrontDesk").Exists())
        {
            Alter.Table("TP_CE_WorkshopTechnician")
                .AddColumn("IsFrontDesk").AsBoolean().NotNullable().WithDefaultValue(false);
        }
    }

    public override void Down()
    {
    }
}
