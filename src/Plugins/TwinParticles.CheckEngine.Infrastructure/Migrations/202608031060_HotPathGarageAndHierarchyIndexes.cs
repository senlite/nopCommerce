using FluentMigrator;
using FluentMigrator.SqlServer;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

/// <summary>
/// Garage child load and vehicle hierarchy FK indexes (garage context + NFR-007 tree browse).
/// </summary>
[NopMigration("2026-08-03 11:00:00", "TwinParticles.CheckEngine hot-path garage and hierarchy indexes", MigrationProcessType.Installation)]
public sealed class HotPathGarageAndHierarchyIndexesMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Index("IX_TP_CE_GarageVehicle_GarageId")
            .OnTable("TP_CE_GarageVehicle")
            .OnColumn("GarageId").Ascending()
            .WithOptions().NonClustered()
            .Include("IsActive")
            .Include("VehicleConfigurationId");

        Create.Index("IX_TP_CE_GarageOem_GarageId")
            .OnTable("TP_CE_GarageOem")
            .OnColumn("GarageId").Ascending()
            .WithOptions().NonClustered();

        Create.Index("IX_TP_CE_VehicleModel_MakeId")
            .OnTable("TP_CE_VehicleModel")
            .OnColumn("MakeId").Ascending()
            .WithOptions().NonClustered()
            .Include("IsActive");

        Create.Index("IX_TP_CE_VehicleGeneration_ModelId")
            .OnTable("TP_CE_VehicleGeneration")
            .OnColumn("ModelId").Ascending()
            .WithOptions().NonClustered()
            .Include("IsActive");

        Create.Index("IX_TP_CE_VehicleBody_GenerationId")
            .OnTable("TP_CE_VehicleBody")
            .OnColumn("GenerationId").Ascending()
            .WithOptions().NonClustered()
            .Include("IsActive");

        Create.Index("IX_TP_CE_VehicleEngine_BodyId")
            .OnTable("TP_CE_VehicleEngine")
            .OnColumn("BodyId").Ascending()
            .WithOptions().NonClustered()
            .Include("IsActive");

        Create.Index("IX_TP_CE_VehicleConfiguration_GenerationId")
            .OnTable("TP_CE_VehicleConfiguration")
            .OnColumn("GenerationId").Ascending()
            .WithOptions().NonClustered()
            .Include("IsActive");
    }
}
