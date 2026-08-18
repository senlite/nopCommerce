using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-18 12:00:00", "TwinParticles.CheckEngine vertical portal schema (workshop, fleet, dealer)", MigrationProcessType.Installation)]
public sealed class VerticalPortalSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        if (!Schema.Table("TP_CE_PriceList").Exists())
        {
            Create.Table("TP_CE_PriceList")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString(128).NotNullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);
        }

        if (!Schema.Table("TP_CE_PriceListItem").Exists())
        {
            Create.Table("TP_CE_PriceListItem")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("PriceListId").AsInt32().NotNullable().ForeignKey("TP_CE_PriceList", "Id")
                .WithColumn("ProductId").AsInt32().NotNullable()
                .WithColumn("UnitPrice").AsDecimal(18, 4).NotNullable();

            Create.Index("IX_TP_CE_PriceListItem_PriceListId_ProductId")
                .OnTable("TP_CE_PriceListItem")
                .OnColumn("PriceListId").Ascending()
                .OnColumn("ProductId").Ascending();
        }

        if (!Schema.Table("TP_CE_WorkshopAccount").Exists())
        {
            Create.Table("TP_CE_WorkshopAccount")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CustomerId").AsInt32().NotNullable()
                .WithColumn("DisplayName").AsString(256).NotNullable()
                .WithColumn("CreditLimit").AsDecimal(18, 4).NotNullable()
                .WithColumn("CreditUsed").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
                .WithColumn("DefaultPriceListId").AsInt32().Nullable().ForeignKey("TP_CE_PriceList", "Id")
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

            Create.Index("IX_TP_CE_WorkshopAccount_CustomerId")
                .OnTable("TP_CE_WorkshopAccount")
                .OnColumn("CustomerId").Ascending();
        }

        if (!Schema.Table("TP_CE_WorkshopJob").Exists())
        {
            Create.Table("TP_CE_WorkshopJob")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("WorkshopAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_WorkshopAccount", "Id")
                .WithColumn("WorkshopCustomerId").AsInt32().Nullable()
                .WithColumn("AssignedTechnicianCustomerId").AsInt32().Nullable()
                .WithColumn("StatusId").AsInt32().NotNullable()
                .WithColumn("LabourEstimate").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
                .WithColumn("OrderId").AsInt32().Nullable()
                .WithColumn("CreatedUtc").AsDateTime2().NotNullable()
                .WithColumn("UpdatedUtc").AsDateTime2().NotNullable();
        }

        if (!Schema.Table("TP_CE_WorkshopJobVehicle").Exists())
        {
            Create.Table("TP_CE_WorkshopJobVehicle")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("JobId").AsInt32().NotNullable().ForeignKey("TP_CE_WorkshopJob", "Id")
                .WithColumn("VehicleConfigurationId").AsInt32().NotNullable()
                .WithColumn("Vin").AsString(17).Nullable()
                .WithColumn("Label").AsString(128).Nullable();
        }

        if (!Schema.Table("TP_CE_WorkshopJobLine").Exists())
        {
            Create.Table("TP_CE_WorkshopJobLine")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("JobId").AsInt32().NotNullable().ForeignKey("TP_CE_WorkshopJob", "Id")
                .WithColumn("JobVehicleId").AsInt32().NotNullable().ForeignKey("TP_CE_WorkshopJobVehicle", "Id")
                .WithColumn("ProductId").AsInt32().NotNullable()
                .WithColumn("Quantity").AsInt32().NotNullable()
                .WithColumn("FitmentOutcome").AsString(64).NotNullable()
                .WithColumn("UnitPrice").AsDecimal(18, 4).NotNullable();
        }

        if (!Schema.Table("TP_CE_FleetAccount").Exists())
        {
            Create.Table("TP_CE_FleetAccount")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CustomerId").AsInt32().NotNullable()
                .WithColumn("DisplayName").AsString(256).NotNullable()
                .WithColumn("DefaultBudgetCentreId").AsInt32().Nullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

            Create.Index("IX_TP_CE_FleetAccount_CustomerId")
                .OnTable("TP_CE_FleetAccount")
                .OnColumn("CustomerId").Ascending();
        }

        if (!Schema.Table("TP_CE_FleetVehicle").Exists())
        {
            Create.Table("TP_CE_FleetVehicle")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("FleetAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_FleetAccount", "Id")
                .WithColumn("VehicleConfigurationId").AsInt32().Nullable()
                .WithColumn("Vin").AsString(17).Nullable()
                .WithColumn("AssetTag").AsString(64).Nullable();
        }

        if (!Schema.Table("TP_CE_FleetVinImportBatch").Exists())
        {
            Create.Table("TP_CE_FleetVinImportBatch")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("FleetAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_FleetAccount", "Id")
                .WithColumn("TotalRows").AsInt32().NotNullable()
                .WithColumn("SucceededRows").AsInt32().NotNullable()
                .WithColumn("CreatedUtc").AsDateTime2().NotNullable();
        }

        if (!Schema.Table("TP_CE_FleetVinImportRow").Exists())
        {
            Create.Table("TP_CE_FleetVinImportRow")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("BatchId").AsInt32().NotNullable().ForeignKey("TP_CE_FleetVinImportBatch", "Id")
                .WithColumn("Vin").AsString(17).NotNullable()
                .WithColumn("Outcome").AsString(32).NotNullable()
                .WithColumn("VehicleConfigurationId").AsInt32().Nullable()
                .WithColumn("ReasonCode").AsString(128).Nullable();
        }

        if (!Schema.Table("TP_CE_FleetBudgetCentre").Exists())
        {
            Create.Table("TP_CE_FleetBudgetCentre")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("FleetAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_FleetAccount", "Id")
                .WithColumn("Name").AsString(128).NotNullable()
                .WithColumn("SpendLimit").AsDecimal(18, 4).NotNullable()
                .WithColumn("SpendUsed").AsDecimal(18, 4).NotNullable().WithDefaultValue(0);
        }

        if (!Schema.Table("TP_CE_FleetApprovalRequest").Exists())
        {
            Create.Table("TP_CE_FleetApprovalRequest")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("FleetAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_FleetAccount", "Id")
                .WithColumn("FleetVehicleId").AsInt32().NotNullable().ForeignKey("TP_CE_FleetVehicle", "Id")
                .WithColumn("ProductId").AsInt32().NotNullable()
                .WithColumn("Quantity").AsInt32().NotNullable()
                .WithColumn("BudgetCentreId").AsInt32().NotNullable().ForeignKey("TP_CE_FleetBudgetCentre", "Id")
                .WithColumn("RequesterCustomerId").AsInt32().NotNullable()
                .WithColumn("StatusId").AsInt32().NotNullable()
                .WithColumn("RejectionReason").AsString(512).Nullable()
                .WithColumn("OrderId").AsInt32().Nullable()
                .WithColumn("CreatedUtc").AsDateTime2().NotNullable()
                .WithColumn("UpdatedUtc").AsDateTime2().NotNullable();
        }

        if (!Schema.Table("TP_CE_DealerAccount").Exists())
        {
            Create.Table("TP_CE_DealerAccount")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CustomerId").AsInt32().NotNullable()
                .WithColumn("DisplayName").AsString(256).NotNullable()
                .WithColumn("DefaultPriceListId").AsInt32().Nullable().ForeignKey("TP_CE_PriceList", "Id")
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

            Create.Index("IX_TP_CE_DealerAccount_CustomerId")
                .OnTable("TP_CE_DealerAccount")
                .OnColumn("CustomerId").Ascending();
        }

        if (!Schema.Table("TP_CE_DealerFranchise").Exists())
        {
            Create.Table("TP_CE_DealerFranchise")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("DealerAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_DealerAccount", "Id")
                .WithColumn("MakeId").AsInt32().NotNullable()
                .WithColumn("FranchiseLabel").AsString(128).NotNullable();
        }

        if (!Schema.Table("TP_CE_DealerAllocation").Exists())
        {
            Create.Table("TP_CE_DealerAllocation")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("DealerAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_DealerAccount", "Id")
                .WithColumn("ProductId").AsInt32().Nullable()
                .WithColumn("CategoryId").AsInt32().Nullable()
                .WithColumn("PeriodCeilingUnits").AsInt32().NotNullable()
                .WithColumn("PeriodUsedUnits").AsInt32().NotNullable().WithDefaultValue(0);
        }

        if (!Schema.Table("TP_CE_DealerQuota").Exists())
        {
            Create.Table("TP_CE_DealerQuota")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("DealerAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_DealerAccount", "Id")
                .WithColumn("SpendCeiling").AsDecimal(18, 4).NotNullable()
                .WithColumn("SpendUsed").AsDecimal(18, 4).NotNullable().WithDefaultValue(0);
        }

        if (!Schema.Table("TP_CE_WarrantyClaim").Exists())
        {
            Create.Table("TP_CE_WarrantyClaim")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("DealerAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_DealerAccount", "Id")
                .WithColumn("OrderId").AsInt32().Nullable()
                .WithColumn("OemNumber").AsString(64).NotNullable()
                .WithColumn("VehicleConfigurationId").AsInt32().NotNullable()
                .WithColumn("StatusId").AsInt32().NotNullable()
                .WithColumn("EvidenceJson").AsString(int.MaxValue).NotNullable()
                .WithColumn("CreatedUtc").AsDateTime2().NotNullable()
                .WithColumn("UpdatedUtc").AsDateTime2().NotNullable();
        }
    }
}
