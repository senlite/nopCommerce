using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-17 17:00:00", "TwinParticles.CheckEngine order vendor splits", MigrationProcessType.Installation)]
public sealed class OrderVendorSplitSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        if (!Schema.Table("TP_CE_OrderVendorSplit").Exists())
        {
            Create.Table("TP_CE_OrderVendorSplit")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CheckoutGroupId").AsGuid().NotNullable()
                .WithColumn("ParentOrderId").AsInt32().NotNullable()
                .WithColumn("VendorId").AsInt32().NotNullable().ForeignKey("TP_CE_Vendor", "Id")
                .WithColumn("LineSubtotalExclTax").AsDecimal(18, 4).NotNullable()
                .WithColumn("CreatedUtc").AsDateTime2().NotNullable();

            Create.Index("IX_TP_CE_OrderVendorSplit_ParentOrderId")
                .OnTable("TP_CE_OrderVendorSplit")
                .OnColumn("ParentOrderId").Ascending();

            Create.Index("IX_TP_CE_OrderVendorSplit_CheckoutGroupId")
                .OnTable("TP_CE_OrderVendorSplit")
                .OnColumn("CheckoutGroupId").Ascending();
        }

        if (!Schema.Table("TP_CE_OrderVendorSplitLine").Exists())
        {
            Create.Table("TP_CE_OrderVendorSplitLine")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("SplitId").AsInt32().NotNullable().ForeignKey("TP_CE_OrderVendorSplit", "Id")
                .WithColumn("OrderItemId").AsInt32().NotNullable()
                .WithColumn("ProductId").AsInt32().NotNullable()
                .WithColumn("Quantity").AsInt32().NotNullable()
                .WithColumn("LineSubtotalExclTax").AsDecimal(18, 4).NotNullable();

            Create.Index("IX_TP_CE_OrderVendorSplitLine_SplitId")
                .OnTable("TP_CE_OrderVendorSplitLine")
                .OnColumn("SplitId").Ascending();
        }

        if (!Schema.Table("TP_CE_ShipmentVendorMap").Exists())
        {
            Create.Table("TP_CE_ShipmentVendorMap")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ShipmentId").AsInt32().NotNullable()
                .WithColumn("OrderId").AsInt32().NotNullable()
                .WithColumn("VendorId").AsInt32().NotNullable().ForeignKey("TP_CE_Vendor", "Id")
                .WithColumn("CreatedUtc").AsDateTime2().NotNullable();

            Create.Index("IX_TP_CE_ShipmentVendorMap_ShipmentId")
                .OnTable("TP_CE_ShipmentVendorMap")
                .OnColumn("ShipmentId").Ascending();

            Create.Index("IX_TP_CE_ShipmentVendorMap_OrderId")
                .OnTable("TP_CE_ShipmentVendorMap")
                .OnColumn("OrderId").Ascending();
        }
    }
}
