using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-16 12:00:00", "TwinParticles.CheckEngine AI usage estimated cost", MigrationProcessType.Installation)]
public sealed class AiUsageEstimatedCostMigration : Migration
{
    public override void Up()
    {
        Alter.Table("TP_CE_AiUsageDaily")
            .AddColumn("EstimatedCostUsd").AsDecimal(18, 4).NotNullable().WithDefaultValue(0);
    }

    public override void Down()
    {
        Delete.Column("EstimatedCostUsd").FromTable("TP_CE_AiUsageDaily");
    }
}
