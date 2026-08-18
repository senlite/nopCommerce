using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-18 23:00:00", "TwinParticles.CheckEngine Horizon 4 final (customers, roles, territory, split invoice)", MigrationProcessType.Installation)]
public sealed class Horizon4Final : Migration
{
    public override void Up()
    {
        if (!Schema.Table("TP_CE_WorkshopCustomer").Exists())
        {
            Create.Table("TP_CE_WorkshopCustomer")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("WorkshopAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_WorkshopAccount", "Id")
                .WithColumn("DisplayName").AsString(256).NotNullable()
                .WithColumn("ContactEmail").AsString(256).Nullable();

            Create.Index("IX_TP_CE_WorkshopCustomer_Account")
                .OnTable("TP_CE_WorkshopCustomer")
                .OnColumn("WorkshopAccountId").Ascending();
        }

        if (!Schema.Table("TP_CE_WorkshopCustomerVehicle").Exists())
        {
            Create.Table("TP_CE_WorkshopCustomerVehicle")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("WorkshopCustomerId").AsInt32().NotNullable().ForeignKey("TP_CE_WorkshopCustomer", "Id")
                .WithColumn("VehicleConfigurationId").AsInt32().NotNullable()
                .WithColumn("Vin").AsString(17).Nullable();

            Create.Index("IX_TP_CE_WorkshopCustomerVehicle_Customer")
                .OnTable("TP_CE_WorkshopCustomerVehicle")
                .OnColumn("WorkshopCustomerId").Ascending();
        }

        if (!Schema.Table("TP_CE_WorkshopTechnician").Exists())
        {
            Create.Table("TP_CE_WorkshopTechnician")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("WorkshopAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_WorkshopAccount", "Id")
                .WithColumn("CustomerId").AsInt32().NotNullable()
                .WithColumn("CanRaiseInvoice").AsBoolean().NotNullable().WithDefaultValue(false);

            Create.Index("IX_TP_CE_WorkshopTechnician_Account_Customer")
                .OnTable("TP_CE_WorkshopTechnician")
                .OnColumn("WorkshopAccountId").Ascending()
                .OnColumn("CustomerId").Ascending();
        }

        if (!Schema.Table("TP_CE_FleetMember").Exists())
        {
            Create.Table("TP_CE_FleetMember")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("FleetAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_FleetAccount", "Id")
                .WithColumn("CustomerId").AsInt32().NotNullable()
                .WithColumn("CanApprove").AsBoolean().NotNullable().WithDefaultValue(false);

            Create.Index("IX_TP_CE_FleetMember_Account_Customer")
                .OnTable("TP_CE_FleetMember")
                .OnColumn("FleetAccountId").Ascending()
                .OnColumn("CustomerId").Ascending();
        }

        if (!Schema.Table("TP_CE_DealerTerritory").Exists())
        {
            Create.Table("TP_CE_DealerTerritory")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("DealerAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_DealerAccount", "Id")
                .WithColumn("MarketId").AsInt32().Nullable()
                .WithColumn("RegionCode").AsString(16).Nullable();
        }

        if (Schema.Table("TP_CE_WorkshopJobLine").Exists() && !Schema.Table("TP_CE_WorkshopJobLine").Column("InvoicedOrderId").Exists())
        {
            Alter.Table("TP_CE_WorkshopJobLine")
                .AddColumn("InvoicedOrderId").AsInt32().Nullable();
        }
    }

    public override void Down()
    {
    }
}
