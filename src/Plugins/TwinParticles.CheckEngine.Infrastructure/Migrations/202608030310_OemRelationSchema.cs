using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-03 03:10:00", "TwinParticles.CheckEngine OEM relation schema", MigrationProcessType.Installation)]
public sealed class OemRelationSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_OemRelation")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("FromOemNumberId").AsInt32().NotNullable().ForeignKey("TP_CE_OemNumber", "Id")
            .WithColumn("ToOemNumberId").AsInt32().NotNullable().ForeignKey("TP_CE_OemNumber", "Id")
            .WithColumn("RelationTypeId").AsInt32().NotNullable()
            .WithColumn("ValidFromUtc").AsDateTime2().Nullable()
            .WithColumn("ValidToUtc").AsDateTime2().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable();

        Create.Index("IX_TP_CE_OemRelation_From_To_Type")
            .OnTable("TP_CE_OemRelation")
            .OnColumn("FromOemNumberId").Ascending()
            .OnColumn("ToOemNumberId").Ascending()
            .OnColumn("RelationTypeId").Ascending()
            .WithOptions().Unique();
    }
}
