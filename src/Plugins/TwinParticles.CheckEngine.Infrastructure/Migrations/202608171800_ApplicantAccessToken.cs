using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-17 18:00:00", "TwinParticles.CheckEngine vendor applicant access token hash", MigrationProcessType.Installation)]
public sealed class ApplicantAccessTokenMigration : AutoReversingMigration
{
    public override void Up()
    {
        if (Schema.Table("TP_CE_Vendor").Exists()
            && !Schema.Table("TP_CE_Vendor").Column("ApplicantAccessTokenHash").Exists())
        {
            Alter.Table("TP_CE_Vendor")
                .AddColumn("ApplicantAccessTokenHash").AsString(64).Nullable();
        }
    }
}
