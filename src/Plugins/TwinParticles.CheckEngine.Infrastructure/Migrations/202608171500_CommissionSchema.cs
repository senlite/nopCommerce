using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-17 15:00:00", "TwinParticles.CheckEngine commission plans and snapshots", MigrationProcessType.Installation)]
public sealed class CommissionSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        if (!Schema.Table("TP_CE_CommissionPlan").Exists())
        {
            Create.Table("TP_CE_CommissionPlan")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("VendorId").AsInt32().NotNullable().ForeignKey("TP_CE_Vendor", "Id")
                .WithColumn("Name").AsString(128).NotNullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedUtc").AsDateTime2().NotNullable();

            Create.Index("IX_TP_CE_CommissionPlan_VendorId")
                .OnTable("TP_CE_CommissionPlan")
                .OnColumn("VendorId").Ascending();
        }

        if (!Schema.Table("TP_CE_CommissionRule").Exists())
        {
            Create.Table("TP_CE_CommissionRule")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("PlanId").AsInt32().NotNullable().ForeignKey("TP_CE_CommissionPlan", "Id")
                .WithColumn("ModelKindId").AsInt32().NotNullable()
                .WithColumn("BasisId").AsInt32().NotNullable()
                .WithColumn("Priority").AsInt32().NotNullable()
                .WithColumn("FlatAmount").AsDecimal(18, 4).Nullable()
                .WithColumn("PercentageRate").AsDecimal(9, 6).Nullable()
                .WithColumn("CategoryId").AsInt32().Nullable()
                .WithColumn("EffectiveFromUtc").AsDateTime2().Nullable()
                .WithColumn("EffectiveToUtc").AsDateTime2().Nullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

            Create.Index("IX_TP_CE_CommissionRule_PlanId_Priority")
                .OnTable("TP_CE_CommissionRule")
                .OnColumn("PlanId").Ascending()
                .OnColumn("Priority").Ascending();
        }

        if (!Schema.Table("TP_CE_CommissionTierBand").Exists())
        {
            Create.Table("TP_CE_CommissionTierBand")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("RuleId").AsInt32().NotNullable().ForeignKey("TP_CE_CommissionRule", "Id")
                .WithColumn("MinVolume").AsDecimal(18, 4).NotNullable()
                .WithColumn("MaxVolume").AsDecimal(18, 4).Nullable()
                .WithColumn("PercentageRate").AsDecimal(9, 6).NotNullable();

            Create.Index("IX_TP_CE_CommissionTierBand_RuleId")
                .OnTable("TP_CE_CommissionTierBand")
                .OnColumn("RuleId").Ascending();
        }

        if (!Schema.Table("TP_CE_OrderLineCommissionSnapshot").Exists())
        {
            Create.Table("TP_CE_OrderLineCommissionSnapshot")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OrderId").AsInt32().NotNullable()
                .WithColumn("OrderItemId").AsInt32().NotNullable()
                .WithColumn("VendorId").AsInt32().NotNullable().ForeignKey("TP_CE_Vendor", "Id")
                .WithColumn("RuleId").AsInt32().NotNullable()
                .WithColumn("ModelKindId").AsInt32().NotNullable()
                .WithColumn("BasisId").AsInt32().NotNullable()
                .WithColumn("RateApplied").AsDecimal(18, 6).NotNullable()
                .WithColumn("CommissionAmount").AsDecimal(18, 4).NotNullable()
                .WithColumn("LineSubtotalExclTax").AsDecimal(18, 4).NotNullable()
                .WithColumn("Quantity").AsInt32().NotNullable()
                .WithColumn("SnapshottedUtc").AsDateTime2().NotNullable();

            Create.Index("IX_TP_CE_OrderLineCommissionSnapshot_OrderId")
                .OnTable("TP_CE_OrderLineCommissionSnapshot")
                .OnColumn("OrderId").Ascending();

            Create.Index("IX_TP_CE_OrderLineCommissionSnapshot_OrderItemId")
                .OnTable("TP_CE_OrderLineCommissionSnapshot")
                .OnColumn("OrderItemId").Ascending()
                .WithOptions().Unique();
        }
    }
}
