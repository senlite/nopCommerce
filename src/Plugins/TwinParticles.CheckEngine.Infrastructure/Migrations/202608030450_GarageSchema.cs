using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-03 04:50:00", "TwinParticles.CheckEngine garage schema", MigrationProcessType.Installation)]
public sealed class GarageSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_Garage")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("CustomerId").AsInt32().NotNullable()
            .WithColumn("ActiveGarageVehicleId").AsInt32().Nullable()
            .WithColumn("CreatedUtc").AsDateTime2().NotNullable()
            .WithColumn("UpdatedUtc").AsDateTime2().NotNullable();

        Create.Table("TP_CE_GarageVehicle")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("GarageId").AsInt32().NotNullable().ForeignKey("TP_CE_Garage", "Id")
            .WithColumn("VehicleConfigurationId").AsInt32().Nullable()
            .WithColumn("Vin").AsString(64).Nullable()
            .WithColumn("Label").AsString(256).NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable()
            .WithColumn("CreatedUtc").AsDateTime2().NotNullable();

        Create.Table("TP_CE_GarageOem")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("GarageId").AsInt32().NotNullable().ForeignKey("TP_CE_Garage", "Id")
            .WithColumn("OemNumberId").AsInt32().NotNullable()
            .WithColumn("DisplayNumber").AsString(128).Nullable()
            .WithColumn("CreatedUtc").AsDateTime2().NotNullable();

        Create.Index("IX_TP_CE_Garage_CustomerId")
            .OnTable("TP_CE_Garage")
            .OnColumn("CustomerId").Ascending()
            .WithOptions().Unique();
    }
}
