using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Seo;

namespace TwinParticles.CheckEngine.Application.Seo;

public sealed class SeoLandingService
{
    private readonly ISeoLandingRepository _repository;
    private readonly ISeoIndexabilityPolicy _indexabilityPolicy;
    private readonly ISeoPerformanceBudgetService _performanceBudgetService;
    private readonly ISeoSitemapService _sitemapService;
    private readonly ISeoStructuredDataService _structuredDataService;
    private readonly ISeoUrlService _urlService;

    public SeoLandingService(
        ISeoLandingRepository repository,
        ISeoUrlService urlService,
        ISeoStructuredDataService structuredDataService,
        ISeoSitemapService sitemapService,
        ISeoPerformanceBudgetService performanceBudgetService,
        ISeoIndexabilityPolicy indexabilityPolicy)
    {
        _repository = repository;
        _urlService = urlService;
        _structuredDataService = structuredDataService;
        _sitemapService = sitemapService;
        _performanceBudgetService = performanceBudgetService;
        _indexabilityPolicy = indexabilityPolicy;
    }

    public async Task<SeoLandingGenerationResult> GenerateVehicleLandingAsync(int vehicleConfigurationId, string locale, CancellationToken cancellationToken)
    {
        if (vehicleConfigurationId <= 0)
            return new SeoLandingGenerationResult { Success = false, ErrorCode = "seo.invalid_vehicle" };

        var normalizedLocale = NormalizeLocale(locale);
        var url = _urlService.BuildVehicleLandingPath(vehicleConfigurationId, normalizedLocale);
        var page = new SeoLandingPage
        {
            Type = SeoLandingPageType.Vehicle,
            VehicleConfigurationId = vehicleConfigurationId,
            Locale = normalizedLocale,
            UrlPath = url,
            CanonicalUrlPath = url,
            HreflangPathEn = _urlService.BuildVehicleLandingPath(vehicleConfigurationId, "en"),
            HreflangPathAr = _urlService.BuildVehicleLandingPath(vehicleConfigurationId, "ar"),
            StructuredDataJsonLd = _structuredDataService.BuildVehicleLandingJsonLd(vehicleConfigurationId, normalizedLocale),
            IsIndexable = _performanceBudgetService.MeetsBudget() &&
                          await _indexabilityPolicy.IsVehicleLandingIndexableAsync(vehicleConfigurationId, cancellationToken),
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.UpsertAsync(page, cancellationToken);
        return new SeoLandingGenerationResult { Success = true, Landing = page };
    }

    public async Task<SeoLandingGenerationResult> GeneratePartForVehicleLandingAsync(int productId, int vehicleConfigurationId, string locale, CancellationToken cancellationToken)
    {
        if (productId <= 0 || vehicleConfigurationId <= 0)
            return new SeoLandingGenerationResult { Success = false, ErrorCode = "seo.invalid_part_vehicle" };

        var normalizedLocale = NormalizeLocale(locale);
        var url = _urlService.BuildPartForVehicleLandingPath(productId, vehicleConfigurationId, normalizedLocale);
        var page = new SeoLandingPage
        {
            Type = SeoLandingPageType.PartForVehicle,
            ProductId = productId,
            VehicleConfigurationId = vehicleConfigurationId,
            Locale = normalizedLocale,
            UrlPath = url,
            CanonicalUrlPath = url,
            HreflangPathEn = _urlService.BuildPartForVehicleLandingPath(productId, vehicleConfigurationId, "en"),
            HreflangPathAr = _urlService.BuildPartForVehicleLandingPath(productId, vehicleConfigurationId, "ar"),
            StructuredDataJsonLd = _structuredDataService.BuildPartForVehicleJsonLd(productId, vehicleConfigurationId, normalizedLocale),
            IsIndexable = _performanceBudgetService.MeetsBudget() &&
                          await _indexabilityPolicy.IsPartForVehicleLandingIndexableAsync(
                              productId,
                              vehicleConfigurationId,
                              cancellationToken),
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.UpsertAsync(page, cancellationToken);
        return new SeoLandingGenerationResult { Success = true, Landing = page };
    }

    public async Task RebuildSitemapAsync(CancellationToken cancellationToken)
    {
        var pages = await _repository.GetAllAsync(cancellationToken);
        await _sitemapService.RebuildAsync(pages, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetSitemapUrlsAsync(CancellationToken cancellationToken)
        => _sitemapService.GetUrlsAsync(cancellationToken);

    private static string NormalizeLocale(string locale)
    {
        if (locale.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
            return "ar";

        return "en";
    }
}
