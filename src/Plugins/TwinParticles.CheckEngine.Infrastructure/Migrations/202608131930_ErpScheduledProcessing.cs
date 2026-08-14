using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-13 19:30:00", "TwinParticles.CheckEngine durable ERP scheduled processing", MigrationProcessType.Installation)]
public sealed class ErpScheduledProcessingMigration : Migration
{
    public override void Up()
    {
        Alter.Table("TP_CE_ErpSyncJob")
            .AddColumn("LastAttemptUtc").AsDateTime2().Nullable()
            .AddColumn("NextAttemptUtc").AsDateTime2().Nullable();

        Create.Index("IX_TP_CE_ErpSyncJob_Status_NextAttemptUtc")
            .OnTable("TP_CE_ErpSyncJob")
            .OnColumn("Status").Ascending()
            .OnColumn("NextAttemptUtc").Ascending()
            .OnColumn("CreatedUtc").Ascending()
            .WithOptions().NonClustered();
    }

    public override void Down()
    {
        Delete.Index("IX_TP_CE_ErpSyncJob_Status_NextAttemptUtc")
            .OnTable("TP_CE_ErpSyncJob");
        Delete.Column("NextAttemptUtc").FromTable("TP_CE_ErpSyncJob");
        Delete.Column("LastAttemptUtc").FromTable("TP_CE_ErpSyncJob");
    }
}
