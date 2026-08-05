using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-03 10:30:00", "TwinParticles.CheckEngine fitment review queue event schema", MigrationProcessType.Installation)]
public sealed class FitmentReviewQueueEventSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_FitmentReviewQueueEvent")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("ClaimId").AsInt32().NotNullable().ForeignKey("TP_CE_FitmentClaim", "Id")
            .WithColumn("ReasonCode").AsString(128).NotNullable()
            .WithColumn("CreatedUtc").AsDateTime2().NotNullable();

        Create.Index("IX_TP_CE_FitmentReviewQueueEvent_ClaimId")
            .OnTable("TP_CE_FitmentReviewQueueEvent")
            .OnColumn("ClaimId").Ascending();

        Create.Index("IX_TP_CE_FitmentReviewQueueEvent_CreatedUtc")
            .OnTable("TP_CE_FitmentReviewQueueEvent")
            .OnColumn("CreatedUtc").Ascending();
    }
}
