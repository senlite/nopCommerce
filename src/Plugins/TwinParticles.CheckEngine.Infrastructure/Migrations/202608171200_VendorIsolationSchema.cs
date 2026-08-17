using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-17 12:00:00", "TwinParticles.CheckEngine vendor catalog isolation and operator upgrade", MigrationProcessType.Installation)]
public sealed class VendorIsolationSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        if (!Schema.Table("TP_CE_Vendor").Column("IsOperator").Exists())
        {
            Alter.Table("TP_CE_Vendor")
                .AddColumn("IsOperator").AsBoolean().NotNullable().WithDefaultValue(false);
        }

        if (Schema.Table("TP_CE_VendorProductMap").Exists())
            return;

        Create.Table("TP_CE_VendorProductMap")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("VendorId").AsInt32().NotNullable().ForeignKey("TP_CE_Vendor", "Id")
            .WithColumn("ProductId").AsInt32().NotNullable();

        Create.Index("IX_TP_CE_VendorProductMap_ProductId")
            .OnTable("TP_CE_VendorProductMap")
            .OnColumn("ProductId").Ascending()
            .WithOptions().Unique();

        Create.Index("IX_TP_CE_VendorProductMap_VendorId")
            .OnTable("TP_CE_VendorProductMap")
            .OnColumn("VendorId").Ascending();
    }
}
