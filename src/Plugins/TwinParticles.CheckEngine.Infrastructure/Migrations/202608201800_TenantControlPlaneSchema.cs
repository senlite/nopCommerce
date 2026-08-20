using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-20 18:00:00", "TwinParticles.CheckEngine Horizon 5 tenant control plane", MigrationProcessType.Installation)]
public sealed class TenantControlPlaneSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        if (!Schema.Table("TP_CE_Tenant").Exists())
        {
            Create.Table("TP_CE_Tenant")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Slug").AsString(64).NotNullable()
                .WithColumn("DisplayName").AsString(256).NotNullable()
                .WithColumn("StatusId").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("IsolationModeId").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("ConnectionName").AsString(128).NotNullable().WithDefaultValue(string.Empty)
                .WithColumn("HostnamesCsv").AsString(1024).NotNullable().WithDefaultValue(string.Empty)
                .WithColumn("IsSelfHosted").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("CreatedUtc").AsDateTime2().NotNullable()
                .WithColumn("UpdatedUtc").AsDateTime2().NotNullable();

            Create.Index("IX_TP_CE_Tenant_Slug")
                .OnTable("TP_CE_Tenant")
                .OnColumn("Slug").Ascending()
                .WithOptions().Unique();
        }

        if (!Schema.Table("TP_CE_TenantSetting").Exists())
        {
            Create.Table("TP_CE_TenantSetting")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantId").AsInt32().NotNullable().ForeignKey("TP_CE_Tenant", "Id")
                .WithColumn("SettingKey").AsString(128).NotNullable()
                .WithColumn("SettingValue").AsString(int.MaxValue).NotNullable();

            Create.Index("IX_TP_CE_TenantSetting_TenantId_SettingKey")
                .OnTable("TP_CE_TenantSetting")
                .OnColumn("TenantId").Ascending()
                .OnColumn("SettingKey").Ascending()
                .WithOptions().Unique();
        }

        if (!Schema.Table("TP_CE_TenantApiKey").Exists())
        {
            Create.Table("TP_CE_TenantApiKey")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantId").AsInt32().NotNullable().ForeignKey("TP_CE_Tenant", "Id")
                .WithColumn("KeyPrefix").AsString(16).NotNullable()
                .WithColumn("KeyHash").AsString(64).NotNullable()
                .WithColumn("ScopesCsv").AsString(512).NotNullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedUtc").AsDateTime2().NotNullable()
                .WithColumn("LastUsedUtc").AsDateTime2().Nullable();

            Create.Index("IX_TP_CE_TenantApiKey_KeyHash")
                .OnTable("TP_CE_TenantApiKey")
                .OnColumn("KeyHash").Ascending()
                .WithOptions().Unique();

            Create.Index("IX_TP_CE_TenantApiKey_TenantId")
                .OnTable("TP_CE_TenantApiKey")
                .OnColumn("TenantId").Ascending();
        }

        if (!Schema.Table("TP_CE_TenantWebhook").Exists())
        {
            Create.Table("TP_CE_TenantWebhook")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantId").AsInt32().NotNullable().ForeignKey("TP_CE_Tenant", "Id")
                .WithColumn("TargetUrl").AsString(1024).NotNullable()
                .WithColumn("SigningSecretProtected").AsString(512).NotNullable()
                .WithColumn("EventTypesCsv").AsString(512).NotNullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedUtc").AsDateTime2().NotNullable();

            Create.Index("IX_TP_CE_TenantWebhook_TenantId")
                .OnTable("TP_CE_TenantWebhook")
                .OnColumn("TenantId").Ascending();
        }

        if (!Schema.Table("TP_CE_TenantUsageDaily").Exists())
        {
            Create.Table("TP_CE_TenantUsageDaily")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantId").AsInt32().NotNullable().ForeignKey("TP_CE_Tenant", "Id")
                .WithColumn("Metric").AsString(64).NotNullable()
                .WithColumn("DayUtc").AsDateTime2().NotNullable()
                .WithColumn("Quantity").AsDecimal(18, 4).NotNullable();

            Create.Index("IX_TP_CE_TenantUsageDaily_TenantId_Metric_DayUtc")
                .OnTable("TP_CE_TenantUsageDaily")
                .OnColumn("TenantId").Ascending()
                .OnColumn("Metric").Ascending()
                .OnColumn("DayUtc").Ascending()
                .WithOptions().Unique();
        }
    }
}
