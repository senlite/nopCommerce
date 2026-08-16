using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-15 12:00:00", "TwinParticles.CheckEngine AI generation and usage schema", MigrationProcessType.Installation)]
public sealed class AiGenerationSchemaMigration : Migration
{
    public override void Up()
    {
        Create.Table("TP_CE_AiGeneration")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("EntityTypeId").AsInt32().NotNullable()
            .WithColumn("EntityId").AsInt32().NotNullable()
            .WithColumn("FeatureKey").AsString(128).NotNullable()
            .WithColumn("Locale").AsString(16).NotNullable().WithDefaultValue("en")
            .WithColumn("OutputText").AsString(int.MaxValue).NotNullable()
            .WithColumn("PromptKey").AsString(128).NotNullable()
            .WithColumn("PromptHash").AsString(128).NotNullable()
            .WithColumn("QualityScore").AsDecimal(5, 4).Nullable()
            .WithColumn("IsPublished").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("ReviewStatus").AsString(32).NotNullable().WithDefaultValue("pending")
            .WithColumn("Reviewer").AsString(256).Nullable()
            .WithColumn("CreatedUtc").AsDateTime2().NotNullable()
            .WithColumn("ReviewedUtc").AsDateTime2().Nullable();

        Create.Index("IX_TP_CE_AiGeneration_Entity")
            .OnTable("TP_CE_AiGeneration")
            .OnColumn("EntityTypeId").Ascending()
            .OnColumn("EntityId").Ascending()
            .OnColumn("ReviewStatus").Ascending();

        Create.Table("TP_CE_AiUsageDaily")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("FeatureKey").AsString(128).NotNullable()
            .WithColumn("UsageDay").AsDate().NotNullable()
            .WithColumn("TokenUsage").AsInt32().NotNullable().WithDefaultValue(0);

        Create.UniqueConstraint("UQ_TP_CE_AiUsageDaily_Feature_Day")
            .OnTable("TP_CE_AiUsageDaily")
            .Columns("FeatureKey", "UsageDay");
    }

    public override void Down()
    {
        Delete.Table("TP_CE_AiUsageDaily");
        Delete.Table("TP_CE_AiGeneration");
    }
}
