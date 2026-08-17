using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-17 16:30:00", "TwinParticles.CheckEngine payout statements", MigrationProcessType.Installation)]
public sealed class PayoutStatementSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        if (!Schema.Table("TP_CE_PayoutStatement").Exists())
        {
            Create.Table("TP_CE_PayoutStatement")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("VendorId").AsInt32().NotNullable().ForeignKey("TP_CE_Vendor", "Id")
                .WithColumn("PeriodStartUtc").AsDateTime2().NotNullable()
                .WithColumn("PeriodEndUtc").AsDateTime2().NotNullable()
                .WithColumn("GrossSales").AsDecimal(18, 4).NotNullable()
                .WithColumn("TotalCommission").AsDecimal(18, 4).NotNullable()
                .WithColumn("TotalRefunds").AsDecimal(18, 4).NotNullable()
                .WithColumn("TotalAdjustments").AsDecimal(18, 4).NotNullable()
                .WithColumn("NetPayout").AsDecimal(18, 4).NotNullable()
                .WithColumn("StatusId").AsInt32().NotNullable()
                .WithColumn("ErpReferenceId").AsString(128).Nullable()
                .WithColumn("ErpReportedTotal").AsDecimal(18, 4).Nullable()
                .WithColumn("CreatedUtc").AsDateTime2().NotNullable()
                .WithColumn("FinalizedUtc").AsDateTime2().Nullable();

            Create.Index("IX_TP_CE_PayoutStatement_VendorId_CreatedUtc")
                .OnTable("TP_CE_PayoutStatement")
                .OnColumn("VendorId").Ascending()
                .OnColumn("CreatedUtc").Descending();
        }

        if (!Schema.Table("TP_CE_PayoutStatementLine").Exists())
        {
            Create.Table("TP_CE_PayoutStatementLine")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("StatementId").AsInt32().NotNullable().ForeignKey("TP_CE_PayoutStatement", "Id")
                .WithColumn("OrderId").AsInt32().NotNullable()
                .WithColumn("OrderItemId").AsInt32().NotNullable()
                .WithColumn("LineSubtotalExclTax").AsDecimal(18, 4).NotNullable()
                .WithColumn("CommissionAmount").AsDecimal(18, 4).NotNullable()
                .WithColumn("RefundAmount").AsDecimal(18, 4).NotNullable();

            Create.Index("IX_TP_CE_PayoutStatementLine_StatementId")
                .OnTable("TP_CE_PayoutStatementLine")
                .OnColumn("StatementId").Ascending();
        }

        if (!Schema.Table("TP_CE_PayoutAdjustment").Exists())
        {
            Create.Table("TP_CE_PayoutAdjustment")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("VendorId").AsInt32().NotNullable().ForeignKey("TP_CE_Vendor", "Id")
                .WithColumn("StatementId").AsInt32().Nullable().ForeignKey("TP_CE_PayoutStatement", "Id")
                .WithColumn("Amount").AsDecimal(18, 4).NotNullable()
                .WithColumn("ReasonCode").AsString(64).NotNullable()
                .WithColumn("Notes").AsString(512).Nullable()
                .WithColumn("CreatedBy").AsString(128).NotNullable()
                .WithColumn("CreatedUtc").AsDateTime2().NotNullable();

            Create.Index("IX_TP_CE_PayoutAdjustment_VendorId_StatementId")
                .OnTable("TP_CE_PayoutAdjustment")
                .OnColumn("VendorId").Ascending()
                .OnColumn("StatementId").Ascending();
        }
    }
}
