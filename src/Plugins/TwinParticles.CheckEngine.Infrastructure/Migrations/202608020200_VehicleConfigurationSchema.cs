using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-02 02:00:00", "TwinParticles.CheckEngine vehicle configuration schema", MigrationProcessType.Installation)]
public sealed class VehicleConfigurationSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_VehicleConfiguration")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("GenerationId").AsInt32().NotNullable().ForeignKey("TP_CE_VehicleGeneration", "Id")
            .WithColumn("BodyId").AsInt32().Nullable().ForeignKey("TP_CE_VehicleBody", "Id")
            .WithColumn("EngineId").AsInt32().Nullable().ForeignKey("TP_CE_VehicleEngine", "Id")
            .WithColumn("MarketId").AsInt32().Nullable().ForeignKey("TP_CE_VehicleMarket", "Id")
            .WithColumn("TrimName").AsString(128).Nullable()
            .WithColumn("ProductionFromYear").AsInt32().Nullable()
            .WithColumn("ProductionToYear").AsInt32().Nullable()
            .WithColumn("Fingerprint").AsString(256).NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable();

        Create.Index("IX_TP_CE_VehicleConfiguration_Fingerprint")
            .OnTable("TP_CE_VehicleConfiguration")
            .OnColumn("Fingerprint").Ascending()
            .WithOptions().Unique();
    }
}
