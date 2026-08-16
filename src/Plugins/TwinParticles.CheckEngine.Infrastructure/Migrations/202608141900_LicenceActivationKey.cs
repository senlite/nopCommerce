using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-14 19:00:00", "TwinParticles.CheckEngine persist licence activation key for heartbeat revalidation", MigrationProcessType.Installation)]
public sealed class LicenceActivationKeyMigration : AutoReversingMigration
{
    public override void Up()
    {
        if (Schema.Table("TP_CE_LicenceState").Column("ActivationKeyProtected").Exists())
            return;

        Alter.Table("TP_CE_LicenceState")
            .AddColumn("ActivationKeyProtected").AsString(2048).Nullable();
    }
}
