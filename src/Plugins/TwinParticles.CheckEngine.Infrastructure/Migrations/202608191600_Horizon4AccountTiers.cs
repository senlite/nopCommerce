using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-19 16:00:00", "TwinParticles.CheckEngine Horizon 4 workshop account pricing tiers", MigrationProcessType.Installation)]
public sealed class Horizon4AccountTiers : Migration
{
    public override void Up()
    {
        if (!Schema.Table("TP_CE_WorkshopAccountTier").Exists())
        {
            Create.Table("TP_CE_WorkshopAccountTier")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TierCode").AsString(32).NotNullable().Unique()
                .WithColumn("DiscountPercent").AsDecimal(5, 2).NotNullable()
                .WithColumn("Label").AsString(128).Nullable();
        }

        if (Schema.Table("TP_CE_WorkshopAccount").Exists()
            && !Schema.Table("TP_CE_WorkshopAccount").Column("AccountTierCode").Exists())
        {
            Alter.Table("TP_CE_WorkshopAccount")
                .AddColumn("AccountTierCode").AsString(32).Nullable();
        }
    }

    public override void Down()
    {
    }
}
