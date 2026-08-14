using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

/// <summary>
/// Durable, shared search-index state plus a catalog keyword projection. State survives restarts and
/// is shared across web-farm nodes (the previous in-memory health flag was process-local). The
/// projection is an incremental-rebuild index that keyword search prefers, degrading to the live
/// catalog when it is unhealthy or not yet built (FR-446, ADR-014).
/// </summary>
[NopMigration("2026-08-14 00:30:00", "TwinParticles.CheckEngine search index projection", MigrationProcessType.Installation)]
public sealed class SearchIndexProjectionMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_SearchIndexState")
            .WithColumn("Id").AsInt32().PrimaryKey()
            .WithColumn("IsHealthy").AsBoolean().NotNullable()
            .WithColumn("LastRebuildUtc").AsDateTime2().Nullable()
            .WithColumn("LastCursorUtc").AsDateTime2().Nullable()
            .WithColumn("IndexedCount").AsInt32().NotNullable()
            .WithColumn("LastDegradedReason").AsString(256).Nullable()
            .WithColumn("LastDegradedUtc").AsDateTime2().Nullable();

        Create.Table("TP_CE_SearchIndex")
            .WithColumn("ProductId").AsInt32().PrimaryKey()
            .WithColumn("Name").AsString(1000).NotNullable()
            .WithColumn("NormalizedText").AsString(int.MaxValue).NotNullable()
            .WithColumn("Sku").AsString(400).Nullable()
            .WithColumn("Mpn").AsString(400).Nullable()
            .WithColumn("Price").AsDecimal(18, 4).NotNullable()
            .WithColumn("UpdatedUtc").AsDateTime2().NotNullable()
            .WithColumn("IndexedUtc").AsDateTime2().NotNullable();

        Create.Index("IX_TP_CE_SearchIndex_Sku")
            .OnTable("TP_CE_SearchIndex")
            .OnColumn("Sku").Ascending();

        Create.Index("IX_TP_CE_SearchIndex_Mpn")
            .OnTable("TP_CE_SearchIndex")
            .OnColumn("Mpn").Ascending();

        Create.Index("IX_TP_CE_SearchIndex_UpdatedUtc")
            .OnTable("TP_CE_SearchIndex")
            .OnColumn("UpdatedUtc").Ascending();
    }
}
