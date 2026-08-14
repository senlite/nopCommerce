using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Seo;
using Nop.Web.Controllers;
using TwinParticles.CheckEngine.Application.Seo;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Domain.Seo;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;
using TwinParticles.CheckEngine.Models;

namespace TwinParticles.CheckEngine.Controllers;

/// <summary>
/// Public, crawlable vehicle and part-for-vehicle landing pages (H1.29).
/// </summary>
public sealed class SeoLandingController : BasePublicController
{
    private readonly IProductSearchReadRepository _productSearchRepository;
    private readonly IProductService _productService;
    private readonly SeoLandingService _seoLandingService;
    private readonly IUrlRecordService _urlRecordService;
    private readonly IVehicleAdminRepository _vehicleRepository;
    private readonly IWebHelper _webHelper;

    public SeoLandingController(
        SeoLandingService seoLandingService,
        IVehicleAdminRepository vehicleRepository,
        IProductSearchReadRepository productSearchRepository,
        IProductService productService,
        IUrlRecordService urlRecordService,
        IWebHelper webHelper)
    {
        _seoLandingService = seoLandingService;
        _vehicleRepository = vehicleRepository;
        _productSearchRepository = productSearchRepository;
        _productService = productService;
        _urlRecordService = urlRecordService;
        _webHelper = webHelper;
    }

    [HttpGet]
    public async Task<IActionResult> Vehicle(
        int vehicleConfigurationId,
        string locale,
        CancellationToken cancellationToken)
    {
        var vehicle = await ResolveVehicleAsync(vehicleConfigurationId, cancellationToken);
        if (vehicle is null)
            return NotFound();

        var landing = await GenerateVehicleLocalePairAsync(
            vehicleConfigurationId,
            locale,
            cancellationToken);
        if (landing is null)
            return NotFound();

        var hits = await _productSearchRepository.SearchByVehicleTreeAsync(new SearchQuery
        {
            Mode = SearchMode.VehicleTree,
            VehicleConfigurationId = vehicleConfigurationId,
            Page = 1,
            PageSize = 100,
            Locale = locale
        }, cancellationToken);

        var products = await BuildProductsAsync(hits.Select(hit => hit.ProductId), cancellationToken);
        var isArabic = IsArabic(locale);
        var vehicleName = $"{vehicle.Value.MakeName} {vehicle.Value.ModelName} {vehicle.Value.GenerationCode} {vehicle.Value.TrimName}";

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Seo/Landing.cshtml", new SeoLandingPageModel
        {
            Title = isArabic ? $"قطع غيار {vehicleName}" : $"{vehicleName} Parts",
            Heading = isArabic ? $"قطع الغيار المتوافقة مع {vehicleName}" : $"Parts that fit {vehicleName}",
            Description = isArabic
                ? "قطع منشورة تم التحقق من توافقها مع هذه السيارة."
                : "Published parts with verified fitment for this vehicle.",
            CanonicalUrl = Absolute(landing.CanonicalUrlPath),
            HreflangUrlEn = Absolute(landing.HreflangPathEn),
            HreflangUrlAr = Absolute(landing.HreflangPathAr),
            StructuredDataJsonLd = landing.StructuredDataJsonLd,
            IsIndexable = landing.IsIndexable,
            Products = products
        });
    }

    [HttpGet]
    public async Task<IActionResult> PartForVehicle(
        int productId,
        int vehicleConfigurationId,
        string locale,
        CancellationToken cancellationToken)
    {
        var vehicle = await ResolveVehicleAsync(vehicleConfigurationId, cancellationToken);
        var product = await _productService.GetProductByIdAsync(productId);
        if (vehicle is null || product is null || product.Deleted || !product.Published)
            return NotFound();

        var landing = await GeneratePartLocalePairAsync(
            productId,
            vehicleConfigurationId,
            locale,
            cancellationToken);
        if (landing is null)
            return NotFound();

        var products = await BuildProductsAsync([productId], cancellationToken);
        var isArabic = IsArabic(locale);
        var vehicleName = $"{vehicle.Value.MakeName} {vehicle.Value.ModelName} {vehicle.Value.GenerationCode} {vehicle.Value.TrimName}";

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Seo/Landing.cshtml", new SeoLandingPageModel
        {
            Title = isArabic
                ? $"{product.Name} متوافق مع {vehicleName}"
                : $"{product.Name} for {vehicleName}",
            Heading = isArabic
                ? $"توافق {product.Name} مع {vehicleName}"
                : $"{product.Name} fitment for {vehicleName}",
            Description = isArabic
                ? "صفحة توافق للقطعة والسيارة، مبنية على مطالبة توافق منشورة."
                : "Part and vehicle compatibility based on a published fitment claim.",
            CanonicalUrl = Absolute(landing.CanonicalUrlPath),
            HreflangUrlEn = Absolute(landing.HreflangPathEn),
            HreflangUrlAr = Absolute(landing.HreflangPathAr),
            StructuredDataJsonLd = landing.StructuredDataJsonLd,
            IsIndexable = landing.IsIndexable,
            IsPartLanding = true,
            Products = products
        });
    }

    private async Task<SeoLandingPage?> GenerateVehicleLocalePairAsync(
        int vehicleConfigurationId,
        string locale,
        CancellationToken cancellationToken)
    {
        var requestedLocale = IsArabic(locale) ? "ar" : "en";
        var requested = await _seoLandingService.GenerateVehicleLandingAsync(
            vehicleConfigurationId,
            requestedLocale,
            cancellationToken);
        if (!requested.Success)
            return null;

        await _seoLandingService.GenerateVehicleLandingAsync(
            vehicleConfigurationId,
            requestedLocale == "ar" ? "en" : "ar",
            cancellationToken);

        return requested.Landing;
    }

    private async Task<SeoLandingPage?> GeneratePartLocalePairAsync(
        int productId,
        int vehicleConfigurationId,
        string locale,
        CancellationToken cancellationToken)
    {
        var requestedLocale = IsArabic(locale) ? "ar" : "en";
        var requested = await _seoLandingService.GeneratePartForVehicleLandingAsync(
            productId,
            vehicleConfigurationId,
            requestedLocale,
            cancellationToken);
        if (!requested.Success)
            return null;

        await _seoLandingService.GeneratePartForVehicleLandingAsync(
            productId,
            vehicleConfigurationId,
            requestedLocale == "ar" ? "en" : "ar",
            cancellationToken);

        return requested.Landing;
    }

    private async Task<IReadOnlyList<SeoLandingProductModel>> BuildProductsAsync(
        IEnumerable<int> productIds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ids = productIds.Distinct().ToArray();
        if (ids.Length == 0)
            return [];

        var products = await _productService.GetProductsByIdsAsync(ids);
        var result = new List<SeoLandingProductModel>();
        foreach (var product in products.Where(product => product.Published && !product.Deleted && product.VisibleIndividually))
        {
            var slug = await _urlRecordService.GetSeNameAsync(product);
            result.Add(new SeoLandingProductModel
            {
                Id = product.Id,
                Name = product.Name,
                Url = "/" + slug
            });
        }

        return result;
    }

    private async Task<(string MakeName, string ModelName, string GenerationCode, string TrimName)?> ResolveVehicleAsync(
        int configurationId,
        CancellationToken cancellationToken)
    {
        var configuration = await _vehicleRepository.GetConfigurationByIdAsync(configurationId, cancellationToken);
        if (configuration is null || !configuration.IsActive)
            return null;

        var generation = await _vehicleRepository.GetGenerationByIdAsync(configuration.GenerationId, cancellationToken);
        if (generation is null || !generation.IsActive)
            return null;

        var model = await _vehicleRepository.GetModelByIdAsync(generation.ModelId, cancellationToken);
        if (model is null || !model.IsActive)
            return null;

        var make = await _vehicleRepository.GetMakeByIdAsync(model.MakeId, cancellationToken);
        if (make is null || !make.IsActive)
            return null;

        return (make.Name, model.Name, generation.Code, configuration.TrimName);
    }

    private string Absolute(string relativePath)
        => _webHelper.GetStoreLocation().TrimEnd('/') + "/" + relativePath.TrimStart('/');

    private static bool IsArabic(string locale)
        => locale?.StartsWith("ar", StringComparison.OrdinalIgnoreCase) == true;
}
