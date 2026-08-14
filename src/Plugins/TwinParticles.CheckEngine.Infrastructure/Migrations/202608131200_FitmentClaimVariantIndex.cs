using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

/// <summary>
/// Relaxes the fitment claim lookup index from unique to non-unique (H1.9f).
///
/// A part legitimately has more than one claim for the same vehicle configuration when the claims
/// are separated by qualifiers — for example a left-hand-drive claim and a right-hand-drive claim
/// for the same product and generation. The original unique index on
/// (ProductId, VehicleConfigurationId) made those variants impossible to store. The lookup key is
/// preserved (same name and columns) so the 1×1 read path stays efficient; only the uniqueness
/// constraint is dropped.
/// </summary>
[NopMigration("2026-08-13 12:00:00", "TwinParticles.CheckEngine fitment claim variant index", MigrationProcessType.Installation)]
public sealed class FitmentClaimVariantIndexMigration : Migration
{
    private const string IndexName = "IX_TP_CE_FitmentClaim_ProductId_VehicleConfigurationId";
    private const string TableName = "TP_CE_FitmentClaim";

    public override void Up()
    {
        Delete.Index(IndexName).OnTable(TableName);

        Create.Index(IndexName)
            .OnTable(TableName)
            .OnColumn("ProductId").Ascending()
            .OnColumn("VehicleConfigurationId").Ascending();
    }

    public override void Down()
    {
        Delete.Index(IndexName).OnTable(TableName);

        Create.Index(IndexName)
            .OnTable(TableName)
            .OnColumn("ProductId").Ascending()
            .OnColumn("VehicleConfigurationId").Ascending()
            .WithOptions().Unique();
    }
}
