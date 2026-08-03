using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-03 05:20:00", "TwinParticles.CheckEngine product image metadata schema", MigrationProcessType.Installation)]
public sealed class ProductImageMetaSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_ProductImageMeta")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("ProductId").AsInt32().NotNullable()
            .WithColumn("PictureId").AsInt32().NotNullable()
            .WithColumn("SourceUrl").AsString(2048).Nullable()
            .WithColumn("IsPlaceholder").AsBoolean().NotNullable()
            .WithColumn("QuarantineStatus").AsInt32().NotNullable()
            .WithColumn("CreatedUtc").AsDateTime2().NotNullable();

        Create.Index("IX_TP_CE_ProductImageMeta_ProductId")
            .OnTable("TP_CE_ProductImageMeta")
            .OnColumn("ProductId").Ascending()
            .WithOptions().Unique();
    }
}
