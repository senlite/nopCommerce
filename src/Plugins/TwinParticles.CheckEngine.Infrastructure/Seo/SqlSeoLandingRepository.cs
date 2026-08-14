using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Seo;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Seo;

public sealed class SqlSeoLandingRepository : ISeoLandingRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlSeoLandingRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task UpsertAsync(SeoLandingPage page, CancellationToken cancellationToken)
    {
        const string updateSql = @"UPDATE TP_CE_SeoLanding
SET UrlPath = @urlPath,
    CanonicalUrlPath = @canonicalUrlPath,
    HreflangPathEn = @hreflangPathEn,
    HreflangPathAr = @hreflangPathAr,
    StructuredDataJsonLd = @structuredDataJsonLd,
    IsIndexable = @isIndexable
WHERE LandingType = @landingType
  AND Locale = @locale
  AND ((VehicleConfigurationId IS NULL AND @vehicleConfigurationId IS NULL) OR VehicleConfigurationId = @vehicleConfigurationId)
  AND ((ProductId IS NULL AND @productId IS NULL) OR ProductId = @productId)";

        var updated = await _dataProvider.ExecuteNonQueryAsync(updateSql,
            new DataParameter("urlPath", page.UrlPath),
            new DataParameter("canonicalUrlPath", page.CanonicalUrlPath),
            new DataParameter("hreflangPathEn", page.HreflangPathEn),
            new DataParameter("hreflangPathAr", page.HreflangPathAr),
            new DataParameter("structuredDataJsonLd", page.StructuredDataJsonLd),
            new DataParameter("isIndexable", page.IsIndexable),
            new DataParameter("landingType", (int)page.Type),
            new DataParameter("locale", page.Locale),
            new DataParameter("vehicleConfigurationId", page.VehicleConfigurationId),
            new DataParameter("productId", page.ProductId));

        if (updated > 0)
            return;

        var createdUtc = page.CreatedUtc == default ? DateTime.UtcNow : page.CreatedUtc;
        var inserted = await _dataProvider.QueryAsync<ScalarIntRow>(
            @"INSERT INTO TP_CE_SeoLanding
(LandingType, VehicleConfigurationId, ProductId, Locale, UrlPath, CanonicalUrlPath, HreflangPathEn, HreflangPathAr, StructuredDataJsonLd, IsIndexable, CreatedUtc)
VALUES
(@landingType, @vehicleConfigurationId, @productId, @locale, @urlPath, @canonicalUrlPath, @hreflangPathEn, @hreflangPathAr, @structuredDataJsonLd, @isIndexable, @createdUtc);
" + CheckEngineSql.SelectInsertedIntId() + @"",
            new DataParameter("landingType", (int)page.Type),
            new DataParameter("vehicleConfigurationId", page.VehicleConfigurationId),
            new DataParameter("productId", page.ProductId),
            new DataParameter("locale", page.Locale),
            new DataParameter("urlPath", page.UrlPath),
            new DataParameter("canonicalUrlPath", page.CanonicalUrlPath),
            new DataParameter("hreflangPathEn", page.HreflangPathEn),
            new DataParameter("hreflangPathAr", page.HreflangPathAr),
            new DataParameter("structuredDataJsonLd", page.StructuredDataJsonLd),
            new DataParameter("isIndexable", page.IsIndexable),
            new DataParameter("createdUtc", createdUtc));

        page.Id = inserted.Single().Value;
        page.CreatedUtc = createdUtc;
    }

    public async Task<IReadOnlyList<SeoLandingPage>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<SeoLandingRow>(
            @"SELECT Id, LandingType, VehicleConfigurationId, ProductId, Locale, UrlPath, CanonicalUrlPath,
       HreflangPathEn, HreflangPathAr, StructuredDataJsonLd, IsIndexable, CreatedUtc
FROM TP_CE_SeoLanding
ORDER BY Id");

        return rows.Select(Map).ToList();
    }

    private static SeoLandingPage Map(SeoLandingRow row)
        => new()
        {
            Id = row.Id,
            Type = (SeoLandingPageType)row.LandingType,
            VehicleConfigurationId = row.VehicleConfigurationId,
            ProductId = row.ProductId,
            Locale = row.Locale,
            UrlPath = row.UrlPath,
            CanonicalUrlPath = row.CanonicalUrlPath,
            HreflangPathEn = row.HreflangPathEn,
            HreflangPathAr = row.HreflangPathAr,
            StructuredDataJsonLd = row.StructuredDataJsonLd,
            IsIndexable = row.IsIndexable,
            CreatedUtc = row.CreatedUtc
        };

    private sealed class ScalarIntRow
    {
        public int Value { get; set; }
    }

    private sealed class SeoLandingRow
    {
        public int Id { get; set; }
        public int LandingType { get; set; }
        public int? VehicleConfigurationId { get; set; }
        public int? ProductId { get; set; }
        public string Locale { get; set; } = string.Empty;
        public string UrlPath { get; set; } = string.Empty;
        public string CanonicalUrlPath { get; set; } = string.Empty;
        public string HreflangPathEn { get; set; } = string.Empty;
        public string HreflangPathAr { get; set; } = string.Empty;
        public string StructuredDataJsonLd { get; set; } = string.Empty;
        public bool IsIndexable { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
