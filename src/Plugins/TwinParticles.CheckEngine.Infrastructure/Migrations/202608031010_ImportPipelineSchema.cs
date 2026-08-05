using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-03 10:10:00", "TwinParticles.CheckEngine import pipeline schema", MigrationProcessType.Installation)]
public sealed class ImportPipelineSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_ImportBatch")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("FileName").AsString(260).NotNullable()
            .WithColumn("SourceFormatId").AsInt32().NotNullable()
            .WithColumn("Status").AsString(64).NotNullable()
            .WithColumn("UploadedByCustomerId").AsInt32().Nullable()
            .WithColumn("RowCount").AsInt32().NotNullable()
            .WithColumn("ErrorSummary").AsString(int.MaxValue).Nullable()
            .WithColumn("CreatedUtc").AsDateTime2().NotNullable()
            .WithColumn("UpdatedUtc").AsDateTime2().NotNullable();

        Create.Table("TP_CE_ImportRow")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("BatchId").AsInt32().NotNullable().ForeignKey("TP_CE_ImportBatch", "Id")
            .WithColumn("RowNumber").AsInt32().NotNullable()
            .WithColumn("RawPayload").AsString(int.MaxValue).NotNullable()
            .WithColumn("NormalizedOem").AsString(128).Nullable()
            .WithColumn("MatchedOemNumberId").AsInt32().Nullable().ForeignKey("TP_CE_OemNumber", "Id")
            .WithColumn("ProposedProductId").AsInt32().Nullable()
            .WithColumn("ProposedFitmentJson").AsString(int.MaxValue).Nullable()
            .WithColumn("Confidence").AsDecimal(5, 4).Nullable()
            .WithColumn("ReviewStatus").AsString(64).NotNullable()
            .WithColumn("ReviewNote").AsString(512).Nullable();

        Create.Index("IX_TP_CE_ImportRow_BatchId_ReviewStatus")
            .OnTable("TP_CE_ImportRow")
            .OnColumn("BatchId").Ascending()
            .OnColumn("ReviewStatus").Ascending();

        Create.Index("IX_TP_CE_ImportRow_BatchId_RowNumber")
            .OnTable("TP_CE_ImportRow")
            .OnColumn("BatchId").Ascending()
            .OnColumn("RowNumber").Ascending()
            .WithOptions().Unique();
    }
}
