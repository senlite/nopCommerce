using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-03 10:20:00", "TwinParticles.CheckEngine product OEM map schema", MigrationProcessType.Installation)]
public sealed class ProductOemMapSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_ProductOemMap")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("ProductId").AsInt32().NotNullable()
            .WithColumn("OemNumberId").AsInt32().NotNullable().ForeignKey("TP_CE_OemNumber", "Id")
            .WithColumn("IsPrimary").AsBoolean().NotNullable()
            .WithColumn("CreatedUtc").AsDateTime2().NotNullable();

        Create.Index("IX_TP_CE_ProductOemMap_ProductId_OemNumberId")
            .OnTable("TP_CE_ProductOemMap")
            .OnColumn("ProductId").Ascending()
            .OnColumn("OemNumberId").Ascending()
            .WithOptions().Unique();

        Create.Index("IX_TP_CE_ProductOemMap_OemNumberId")
            .OnTable("TP_CE_ProductOemMap")
            .OnColumn("OemNumberId").Ascending();
    }
}
