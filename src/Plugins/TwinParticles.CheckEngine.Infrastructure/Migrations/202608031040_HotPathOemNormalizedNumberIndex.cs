using FluentMigrator;
using FluentMigrator.SqlServer;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

/// <summary>
/// OEM hot-path covering index for manufacturer-agnostic normalized lookup (NFR-006).
/// </summary>
[NopMigration("2026-08-03 10:40:00", "TwinParticles.CheckEngine hot-path OEM normalized number index", MigrationProcessType.Installation)]
public sealed class HotPathOemNormalizedNumberIndexMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Index("IX_TP_CE_OemNumber_NormalizedNumber")
            .OnTable("TP_CE_OemNumber")
            .OnColumn("NormalizedNumber").Ascending()
            .WithOptions().NonClustered()
            .Include("ManufacturerId")
            .Include("DisplayNumber")
            .Include("IsActive");
    }
}
