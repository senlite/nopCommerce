using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-16 18:00:00", "TwinParticles.CheckEngine vendor onboarding and agreement acceptance", MigrationProcessType.Installation)]
public sealed class VendorOnboardingSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        if (Schema.Table("TP_CE_Vendor").Exists())
            return;

        Create.Table("TP_CE_Vendor")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("LegalName").AsString(256).NotNullable()
            .WithColumn("TradingName").AsString(256).Nullable()
            .WithColumn("ContactEmail").AsString(256).NotNullable()
            .WithColumn("ApplicantCustomerId").AsInt32().Nullable()
            .WithColumn("TaxIdsJson").AsString(int.MaxValue).NotNullable()
            .WithColumn("CategoriesCsv").AsString(1024).NotNullable()
            .WithColumn("StatusId").AsInt32().NotNullable()
            .WithColumn("BankingSecretProtected").AsString(4096).Nullable()
            .WithColumn("ReviewNotes").AsString(1024).Nullable()
            .WithColumn("CreatedUtc").AsDateTime2().NotNullable()
            .WithColumn("UpdatedUtc").AsDateTime2().NotNullable();

        Create.Table("TP_CE_VendorAgreementAcceptance")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("VendorId").AsInt32().NotNullable().ForeignKey("TP_CE_Vendor", "Id")
            .WithColumn("AgreementVersion").AsString(64).NotNullable()
            .WithColumn("AcceptedUtc").AsDateTime2().NotNullable()
            .WithColumn("AcceptedBy").AsString(128).NotNullable();

        Create.Index("IX_TP_CE_Vendor_StatusId_CreatedUtc")
            .OnTable("TP_CE_Vendor")
            .OnColumn("StatusId").Ascending()
            .OnColumn("CreatedUtc").Descending();

        Create.Index("IX_TP_CE_VendorAgreementAcceptance_VendorId_Version")
            .OnTable("TP_CE_VendorAgreementAcceptance")
            .OnColumn("VendorId").Ascending()
            .OnColumn("AgreementVersion").Ascending();
    }
}
