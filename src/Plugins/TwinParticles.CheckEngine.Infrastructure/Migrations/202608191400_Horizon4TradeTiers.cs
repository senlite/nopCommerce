using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-19 14:00:00", "TwinParticles.CheckEngine Horizon 4 trade price quantity tiers", MigrationProcessType.Installation)]
public sealed class Horizon4TradeTiers : Migration
{
    public override void Up()
    {
        if (!Schema.Table("TP_CE_WorkshopPriceTier").Exists())
        {
            Create.Table("TP_CE_WorkshopPriceTier")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("PriceListId").AsInt32().NotNullable().ForeignKey("TP_CE_PriceList", "Id")
                .WithColumn("MinQuantity").AsInt32().NotNullable()
                .WithColumn("DiscountPercent").AsDecimal(5, 2).NotNullable();

            Create.Index("IX_TP_CE_WorkshopPriceTier_PriceList")
                .OnTable("TP_CE_WorkshopPriceTier")
                .OnColumn("PriceListId").Ascending()
                .OnColumn("MinQuantity").Ascending();
        }
    }

    public override void Down()
    {
    }
}
