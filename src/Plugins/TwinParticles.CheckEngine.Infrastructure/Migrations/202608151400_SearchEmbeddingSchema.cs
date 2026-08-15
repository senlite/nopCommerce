using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-15 14:00:00", "TwinParticles.CheckEngine semantic search embedding index", MigrationProcessType.Installation)]
public sealed class SearchEmbeddingSchemaMigration : Migration
{
    public override void Up()
    {
        Create.Table("TP_CE_SearchEmbedding")
            .WithColumn("ProductId").AsInt32().NotNullable()
            .WithColumn("Locale").AsString(16).NotNullable()
            .WithColumn("Name").AsString(1000).NotNullable()
            .WithColumn("CategoryName").AsString(256).Nullable()
            .WithColumn("Brand").AsString(256).Nullable()
            .WithColumn("Price").AsDecimal(18, 4).Nullable()
            .WithColumn("EmbeddingJson").AsString(int.MaxValue).NotNullable()
            .WithColumn("ModelHash").AsString(128).NotNullable()
            .WithColumn("UpdatedUtc").AsDateTime2().NotNullable();

        Create.PrimaryKey("PK_TP_CE_SearchEmbedding")
            .OnTable("TP_CE_SearchEmbedding")
            .Columns("ProductId", "Locale");
    }

    public override void Down()
    {
        Delete.Table("TP_CE_SearchEmbedding");
    }
}
