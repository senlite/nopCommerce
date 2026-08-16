using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-14 15:00:00", "TwinParticles.CheckEngine durable licence heartbeat state", MigrationProcessType.Installation)]
public sealed class LicenceStateMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_LicenceState")
            .WithColumn("Id").AsInt32().PrimaryKey()
            .WithColumn("LastHeartbeatUtc").AsDateTime2().Nullable()
            .WithColumn("ActivationKeyProtected").AsString(2048).Nullable()
            .WithColumn("UpdatedUtc").AsDateTime2().NotNullable();
    }
}
