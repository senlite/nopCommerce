using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-13 15:00:00", "TwinParticles.CheckEngine durable import pipeline state", MigrationProcessType.Installation)]
public sealed class ImportPipelineDurableStateMigration : Migration
{
    public override void Up()
    {
        Alter.Table("TP_CE_ImportBatch")
            .AddColumn("SourceContent").AsBinary(int.MaxValue).Nullable()
            .AddColumn("RunOptionsJson").AsString(int.MaxValue).Nullable()
            .AddColumn("CurrentStage").AsString(64).Nullable()
            .AddColumn("CompletedStagesCsv").AsString(512).Nullable();

        Alter.Table("TP_CE_ImportRow")
            .AddColumn("PipelineStateJson").AsString(int.MaxValue).Nullable()
            .AddColumn("CompletedStagesCsv").AsString(512).Nullable()
            .AddColumn("LastStageError").AsString(1024).Nullable();
    }

    public override void Down()
    {
        Delete.Column("LastStageError").FromTable("TP_CE_ImportRow");
        Delete.Column("CompletedStagesCsv").FromTable("TP_CE_ImportRow");
        Delete.Column("PipelineStateJson").FromTable("TP_CE_ImportRow");

        Delete.Column("CompletedStagesCsv").FromTable("TP_CE_ImportBatch");
        Delete.Column("CurrentStage").FromTable("TP_CE_ImportBatch");
        Delete.Column("RunOptionsJson").FromTable("TP_CE_ImportBatch");
        Delete.Column("SourceContent").FromTable("TP_CE_ImportBatch");
    }
}
