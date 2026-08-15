using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-15 16:00:00", "TwinParticles.CheckEngine AI usage outcome columns", MigrationProcessType.Installation)]
public sealed class AiUsageOutcomeColumnsMigration : Migration
{
    public override void Up()
    {
        Alter.Table("TP_CE_AiUsageDaily")
            .AddColumn("AttemptCount").AsInt32().NotNullable().WithDefaultValue(0)
            .AddColumn("FailureCount").AsInt32().NotNullable().WithDefaultValue(0);
    }

    public override void Down()
    {
        Delete.Column("FailureCount").FromTable("TP_CE_AiUsageDaily");
        Delete.Column("AttemptCount").FromTable("TP_CE_AiUsageDaily");
    }
}
