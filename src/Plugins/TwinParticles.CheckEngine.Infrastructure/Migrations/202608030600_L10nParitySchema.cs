using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-03 06:00:00", "TwinParticles.CheckEngine l10n parity schema", MigrationProcessType.Installation)]
public sealed class L10nParitySchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_ResourceParity")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("ResourceKey").AsString(256).NotNullable()
            .WithColumn("HasEnglish").AsBoolean().NotNullable()
            .WithColumn("HasArabic").AsBoolean().NotNullable()
            .WithColumn("CheckedUtc").AsDateTime2().NotNullable();

        Create.Index("IX_TP_CE_ResourceParity_ResourceKey")
            .OnTable("TP_CE_ResourceParity")
            .OnColumn("ResourceKey").Ascending()
            .WithOptions().Unique();
    }
}
