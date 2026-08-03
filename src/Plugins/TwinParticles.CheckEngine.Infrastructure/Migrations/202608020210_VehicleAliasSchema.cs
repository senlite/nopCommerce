using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-02 02:10:00", "TwinParticles.CheckEngine vehicle alias schema", MigrationProcessType.Installation)]
public sealed class VehicleAliasSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_VehicleAlias")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("NodeType").AsString(32).NotNullable()
            .WithColumn("NodeId").AsInt32().NotNullable()
            .WithColumn("Locale").AsString(16).NotNullable()
            .WithColumn("AliasText").AsString(256).NotNullable()
            .WithColumn("NormalizedAlias").AsString(256).NotNullable();

        Create.Index("IX_TP_CE_VehicleAlias_NodeType_NormalizedAlias")
            .OnTable("TP_CE_VehicleAlias")
            .OnColumn("NodeType").Ascending()
            .OnColumn("NormalizedAlias").Ascending()
            .WithOptions().Unique();
    }
}
