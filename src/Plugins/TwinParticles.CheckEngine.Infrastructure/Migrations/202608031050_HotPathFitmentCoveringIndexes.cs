using FluentMigrator;
using FluentMigrator.SqlServer;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

/// <summary>
/// Fitment hot-path covering and review-queue indexes (NFR-001, NFR-003, NFR-008, AC-10.3).
/// </summary>
[NopMigration("2026-08-03 10:50:00", "TwinParticles.CheckEngine hot-path fitment covering indexes", MigrationProcessType.Installation)]
public sealed class HotPathFitmentCoveringIndexesMigration : AutoReversingMigration
{
    public override void Up()
    {
        // Covering index for vehicle-constrained published product lookup (search / garage filter).
        // Name kept under 64 chars so it is valid on MySQL as well as SQL Server.
        Create.Index("IX_TP_CE_FitmentClaim_VehicleConfig_IsPublished_Covering")
            .OnTable("TP_CE_FitmentClaim")
            .OnColumn("VehicleConfigurationId").Ascending()
            .OnColumn("IsPublished").Ascending()
            .WithOptions().NonClustered()
            .Include("ProductId")
            .Include("FitmentStatusId")
            .Include("Confidence");

        // Covering index for PDP fitment badge path.
        Create.Index("IX_TP_CE_FitmentClaim_ProductId_IsPublished_Covering")
            .OnTable("TP_CE_FitmentClaim")
            .OnColumn("ProductId").Ascending()
            .OnColumn("IsPublished").Ascending()
            .WithOptions().NonClustered()
            .Include("VehicleConfigurationId")
            .Include("FitmentStatusId")
            .Include("Confidence");

        // Admin review queue: unpublished or ambiguous/rejected statuses ordered by confidence.
        Create.Index("IX_TP_CE_FitmentClaim_ReviewQueue")
            .OnTable("TP_CE_FitmentClaim")
            .OnColumn("IsPublished").Ascending()
            .OnColumn("FitmentStatusId").Ascending()
            .WithOptions().NonClustered()
            .Include("Confidence")
            .Include("ProductId")
            .Include("VehicleConfigurationId");
    }
}
