using FluentMigrator;
using FluentMigrator.SqlServer;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

/// <summary>
/// OEM supersession walk indexes (forward and reverse relation lookups).
/// </summary>
[NopMigration("2026-08-03 11:10:00", "TwinParticles.CheckEngine hot-path OEM relation walk indexes", MigrationProcessType.Installation)]
public sealed class HotPathOemRelationWalkIndexesMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Index("IX_TP_CE_OemRelation_FromOemNumberId_IsActive")
            .OnTable("TP_CE_OemRelation")
            .OnColumn("FromOemNumberId").Ascending()
            .OnColumn("IsActive").Ascending()
            .WithOptions().NonClustered()
            .Include("ToOemNumberId")
            .Include("RelationTypeId");

        Create.Index("IX_TP_CE_OemRelation_ToOemNumberId")
            .OnTable("TP_CE_OemRelation")
            .OnColumn("ToOemNumberId").Ascending()
            .WithOptions().NonClustered()
            .Include("FromOemNumberId")
            .Include("RelationTypeId")
            .Include("IsActive");
    }
}
