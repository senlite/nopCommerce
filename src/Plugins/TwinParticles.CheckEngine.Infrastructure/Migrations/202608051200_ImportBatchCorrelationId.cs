using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-05 12:00:00", "TwinParticles.CheckEngine import batch correlation id", MigrationProcessType.Installation)]
public sealed class ImportBatchCorrelationIdMigration : Migration
{
    public override void Up()
    {
        Alter.Table("TP_CE_ImportBatch")
            .AddColumn("CorrelationId").AsGuid().Nullable();

        Create.Index("IX_TP_CE_ImportBatch_CorrelationId")
            .OnTable("TP_CE_ImportBatch")
            .OnColumn("CorrelationId").Ascending();
    }

    public override void Down()
    {
        Delete.Index("IX_TP_CE_ImportBatch_CorrelationId")
            .OnTable("TP_CE_ImportBatch");

        Delete.Column("CorrelationId")
            .FromTable("TP_CE_ImportBatch");
    }
}
