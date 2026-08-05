using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-05 11:00:00", "TwinParticles.CheckEngine audit event schema", MigrationProcessType.Installation)]
public sealed class AuditEventSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_AuditEvent")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Actor").AsString(256).NotNullable()
            .WithColumn("Action").AsString(128).NotNullable()
            .WithColumn("EntityType").AsString(128).NotNullable()
            .WithColumn("EntityId").AsString(128).NotNullable()
            .WithColumn("BeforeJson").AsString(int.MaxValue).Nullable()
            .WithColumn("AfterJson").AsString(int.MaxValue).Nullable()
            .WithColumn("CreatedUtc").AsDateTime2().NotNullable();

        Create.Index("IX_TP_CE_AuditEvent_CreatedUtc")
            .OnTable("TP_CE_AuditEvent")
            .OnColumn("CreatedUtc").Ascending();

        Create.Index("IX_TP_CE_AuditEvent_Entity")
            .OnTable("TP_CE_AuditEvent")
            .OnColumn("EntityType").Ascending()
            .OnColumn("EntityId").Ascending();
    }
}
