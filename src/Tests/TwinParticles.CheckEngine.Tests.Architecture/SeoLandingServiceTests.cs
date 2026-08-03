using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Seo;
using TwinParticles.CheckEngine.Domain.Seo;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SeoLandingServiceTests
{
    [Test]
    public async Task GenerateVehicleLandingAsync_Should_Create_Localized_Hreflang_And_JsonLd()
    {
        var repository = new FakeSeoLandingRepository();
        var service = CreateService(repository);

        var result = await service.GenerateVehicleLandingAsync(10041, "ar", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Landing.Should().NotBeNull();
        result.Landing!.UrlPath.Should().StartWith("/ar/");
        result.Landing.HreflangPathEn.Should().StartWith("/vehicles/");
        result.Landing.HreflangPathAr.Should().StartWith("/ar/vehicles/");
        result.Landing.StructuredDataJsonLd.Should().Contain("schema.org");
        result.Landing.IsIndexable.Should().BeTrue();
    }

    [Test]
    public async Task RebuildSitemapAsync_Should_Include_Only_Indexable_Pages()
    {
        var repository = new FakeSeoLandingRepository();
        var sitemap = new FakeSeoSitemapService();
        var service = new SeoLandingService(repository, new FakeSeoUrlService(), new FakeSeoStructuredDataService(), sitemap, new FakeSeoPerformanceBudgetService(true));

        await repository.UpsertAsync(new SeoLandingPage { Type = SeoLandingPageType.Vehicle, VehicleConfigurationId = 1, Locale = "en", UrlPath = "/vehicles/config-1", CanonicalUrlPath = "/vehicles/config-1", HreflangPathEn = "/vehicles/config-1", HreflangPathAr = "/ar/vehicles/config-1", StructuredDataJsonLd = "{}", IsIndexable = true }, CancellationToken.None);
        await repository.UpsertAsync(new SeoLandingPage { Type = SeoLandingPageType.Vehicle, VehicleConfigurationId = 2, Locale = "en", UrlPath = "/vehicles/config-2", CanonicalUrlPath = "/vehicles/config-2", HreflangPathEn = "/vehicles/config-2", HreflangPathAr = "/ar/vehicles/config-2", StructuredDataJsonLd = "{}", IsIndexable = false }, CancellationToken.None);

        await service.RebuildSitemapAsync(CancellationToken.None);
        var urls = await service.GetSitemapUrlsAsync(CancellationToken.None);

        urls.Should().ContainSingle();
        urls.Single().Should().Be("/vehicles/config-1");
    }

    private static SeoLandingService CreateService(FakeSeoLandingRepository repository)
    {
        return new SeoLandingService(
            repository,
            new FakeSeoUrlService(),
            new FakeSeoStructuredDataService(),
            new FakeSeoSitemapService(),
            new FakeSeoPerformanceBudgetService(true));
    }

    private sealed class FakeSeoLandingRepository : ISeoLandingRepository
    {
        private readonly System.Collections.Generic.List<SeoLandingPage> _pages = [];

        public Task UpsertAsync(SeoLandingPage page, CancellationToken cancellationToken)
        {
            _pages.RemoveAll(x => x.Type == page.Type && x.Locale == page.Locale && x.VehicleConfigurationId == page.VehicleConfigurationId && x.ProductId == page.ProductId);
            _pages.Add(page);
            return Task.CompletedTask;
        }

        public Task<System.Collections.Generic.IReadOnlyList<SeoLandingPage>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<System.Collections.Generic.IReadOnlyList<SeoLandingPage>>(_pages.ToList());
    }

    private sealed class FakeSeoUrlService : ISeoUrlService
    {
        public string BuildVehicleLandingPath(int vehicleConfigurationId, string locale)
            => locale == "ar" ? $"/ar/vehicles/config-{vehicleConfigurationId}" : $"/vehicles/config-{vehicleConfigurationId}";

        public string BuildPartForVehicleLandingPath(int productId, int vehicleConfigurationId, string locale)
            => locale == "ar" ? $"/ar/parts/product-{productId}/for/config-{vehicleConfigurationId}" : $"/parts/product-{productId}/for/config-{vehicleConfigurationId}";
    }

    private sealed class FakeSeoStructuredDataService : ISeoStructuredDataService
    {
        public string BuildVehicleLandingJsonLd(int vehicleConfigurationId, string locale)
            => "{\"@context\":\"https://schema.org\",\"@type\":\"ItemList\"}";

        public string BuildPartForVehicleJsonLd(int productId, int vehicleConfigurationId, string locale)
            => "{\"@context\":\"https://schema.org\",\"@type\":\"Product\"}";
    }

    private sealed class FakeSeoSitemapService : ISeoSitemapService
    {
        private System.Collections.Generic.List<string> _urls = [];

        public Task RebuildAsync(System.Collections.Generic.IReadOnlyList<SeoLandingPage> pages, CancellationToken cancellationToken)
        {
            _urls = pages.Where(x => x.IsIndexable).Select(x => x.UrlPath).Distinct().ToList();
            return Task.CompletedTask;
        }

        public Task<System.Collections.Generic.IReadOnlyList<string>> GetUrlsAsync(CancellationToken cancellationToken)
            => Task.FromResult<System.Collections.Generic.IReadOnlyList<string>>(_urls);
    }

    private sealed class FakeSeoPerformanceBudgetService : ISeoPerformanceBudgetService
    {
        private readonly bool _value;

        public FakeSeoPerformanceBudgetService(bool value)
        {
            _value = value;
        }

        public bool MeetsBudget() => _value;
    }
}
