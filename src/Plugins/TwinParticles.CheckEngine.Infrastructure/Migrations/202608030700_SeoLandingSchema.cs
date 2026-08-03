using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-03 07:00:00", "TwinParticles.CheckEngine seo landing schema", MigrationProcessType.Installation)]
public sealed class SeoLandingSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("TP_CE_SeoLanding")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("LandingType").AsInt32().NotNullable()
            .WithColumn("VehicleConfigurationId").AsInt32().Nullable()
            .WithColumn("ProductId").AsInt32().Nullable()
            .WithColumn("Locale").AsString(8).NotNullable()
            .WithColumn("UrlPath").AsString(512).NotNullable()
            .WithColumn("CanonicalUrlPath").AsString(512).NotNullable()
            .WithColumn("HreflangPathEn").AsString(512).NotNullable()
            .WithColumn("HreflangPathAr").AsString(512).NotNullable()
            .WithColumn("StructuredDataJsonLd").AsString(int.MaxValue).NotNullable()
            .WithColumn("IsIndexable").AsBoolean().NotNullable()
            .WithColumn("CreatedUtc").AsDateTime2().NotNullable();

        Create.Index("IX_TP_CE_SeoLanding_TypeLocaleVehicleProduct")
            .OnTable("TP_CE_SeoLanding")
            .OnColumn("LandingType").Ascending()
            .OnColumn("Locale").Ascending()
            .OnColumn("VehicleConfigurationId").Ascending()
            .OnColumn("ProductId").Ascending();
    }
}
