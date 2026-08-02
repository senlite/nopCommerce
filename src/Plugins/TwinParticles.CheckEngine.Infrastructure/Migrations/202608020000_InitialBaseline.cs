using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-02 00:00:00", "TwinParticles.CheckEngine initial baseline", MigrationProcessType.Installation)]
public sealed class InitialBaselineMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CheckEngine_State")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("InstalledOnUtc").AsDateTime2().NotNullable()
            .WithColumn("PluginVersion").AsString(32).NotNullable();
    }
}
