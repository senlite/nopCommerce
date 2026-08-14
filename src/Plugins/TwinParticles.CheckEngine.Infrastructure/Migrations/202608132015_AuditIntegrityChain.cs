using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-13 20:15:00", "TwinParticles.CheckEngine tamper-evident audit chain", MigrationProcessType.Installation)]
public sealed class AuditIntegrityChainMigration : Migration
{
    public override void Up()
    {
        Alter.Table("TP_CE_AuditEvent")
            .AddColumn("PreviousHash").AsString(64).Nullable()
            .AddColumn("EntryHash").AsString(64).Nullable();

        Create.Table("TP_CE_AuditChainAnchor")
            .WithColumn("Id").AsInt32().PrimaryKey()
            .WithColumn("LastPrunedAuditEventId").AsInt32().NotNullable()
            .WithColumn("LastPrunedHash").AsString(64).NotNullable()
            .WithColumn("PrunedUtc").AsDateTime2().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("TP_CE_AuditChainAnchor");
        Delete.Column("EntryHash").FromTable("TP_CE_AuditEvent");
        Delete.Column("PreviousHash").FromTable("TP_CE_AuditEvent");
    }
}
