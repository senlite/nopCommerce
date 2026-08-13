using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-13 12:05:00", "TwinParticles.CheckEngine encrypted garage VIN storage", MigrationProcessType.Installation)]
public sealed class GarageVinEncryptionMigration : Migration
{
    public override void Up()
    {
        // AES ciphertext + version prefix is longer than a 17-character VIN.
        Alter.Column("Vin")
            .OnTable("TP_CE_GarageVehicle")
            .AsString(512)
            .Nullable();
    }

    public override void Down()
    {
        // Do not destructively shrink encrypted values before the owning GarageSchema migration drops
        // the table during uninstall. A 64-character rollback column cannot represent ciphertext.
    }
}
