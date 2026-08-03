using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-03 08:00:00", "TwinParticles.CheckEngine erp sync schema", MigrationProcessType.Installation)]
public sealed class ErpSyncSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_ErpSyncJob")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("JobId").AsGuid().NotNullable()
            .WithColumn("EntityType").AsInt32().NotNullable()
            .WithColumn("Direction").AsInt32().NotNullable()
            .WithColumn("IdempotencyKey").AsString(256).NotNullable()
            .WithColumn("Payload").AsString(int.MaxValue).NotNullable()
            .WithColumn("AttemptCount").AsInt32().NotNullable()
            .WithColumn("Status").AsString(64).NotNullable()
            .WithColumn("ConflictCode").AsString(128).Nullable()
            .WithColumn("CreatedUtc").AsDateTime2().NotNullable();

        Create.Index("IX_TP_CE_ErpSyncJob_JobId")
            .OnTable("TP_CE_ErpSyncJob")
            .OnColumn("JobId").Ascending()
            .WithOptions().Unique();

        Create.Index("IX_TP_CE_ErpSyncJob_IdempotencyKey")
            .OnTable("TP_CE_ErpSyncJob")
            .OnColumn("IdempotencyKey").Ascending()
            .WithOptions().Unique();
    }
}
