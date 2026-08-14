using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-13 12:50:00", "TwinParticles.CheckEngine privacy-safe search analytics", MigrationProcessType.Installation)]
public sealed class SearchAnalyticsMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_SearchAnalytics")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("QueryFingerprint").AsString(64).NotNullable()
            .WithColumn("ModeId").AsInt32().NotNullable()
            .WithColumn("Locale").AsString(16).NotNullable()
            .WithColumn("ResultCount").AsInt32().NotNullable()
            .WithColumn("HasVehicleContext").AsBoolean().NotNullable()
            .WithColumn("WidenFitment").AsBoolean().NotNullable()
            .WithColumn("IsDegraded").AsBoolean().NotNullable()
            .WithColumn("DurationBucketMs").AsInt32().NotNullable()
            .WithColumn("CreatedUtc").AsDateTime2().NotNullable()
            .WithColumn("ClickedProductId").AsInt32().Nullable()
            .WithColumn("ClickedUtc").AsDateTime2().Nullable();

        Create.Index("IX_TP_CE_SearchAnalytics_CreatedUtc")
            .OnTable("TP_CE_SearchAnalytics")
            .OnColumn("CreatedUtc").Descending();

        Create.Index("IX_TP_CE_SearchAnalytics_QueryFingerprint_CreatedUtc")
            .OnTable("TP_CE_SearchAnalytics")
            .OnColumn("QueryFingerprint").Ascending()
            .OnColumn("CreatedUtc").Descending();
    }
}
