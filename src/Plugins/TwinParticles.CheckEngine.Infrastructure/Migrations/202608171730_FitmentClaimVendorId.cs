using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-17 17:30:00", "TwinParticles.CheckEngine fitment claim vendor attribution", MigrationProcessType.Installation)]
public sealed class FitmentClaimVendorIdMigration : AutoReversingMigration
{
    public override void Up()
    {
        if (Schema.Table("TP_CE_FitmentClaim").Exists()
            && !Schema.Table("TP_CE_FitmentClaim").Column("VendorId").Exists())
        {
            Alter.Table("TP_CE_FitmentClaim")
                .AddColumn("VendorId").AsInt32().Nullable().ForeignKey("TP_CE_Vendor", "Id");

            Create.Index("IX_TP_CE_FitmentClaim_VendorId")
                .OnTable("TP_CE_FitmentClaim")
                .OnColumn("VendorId").Ascending();
        }
    }
}
