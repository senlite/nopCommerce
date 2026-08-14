using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Services.Events;
using Nop.Web.Models.Sitemap;
using TwinParticles.CheckEngine.Domain.Seo;

namespace TwinParticles.CheckEngine.Consumers;

/// <summary>
/// Adds durable Check Engine landing URLs to nopCommerce's standard /sitemap.xml. The host sitemap
/// remains the only XML writer; this consumer only contributes SQL-backed URLs and localized
/// alternates through nopCommerce's supported event extension point.
/// </summary>
public sealed class SeoSitemapCreatedConsumer : IConsumer<SitemapCreatedEvent>
{
    private readonly ISeoLandingRepository _repository;
    private readonly IWebHelper _webHelper;

    public SeoSitemapCreatedConsumer(ISeoLandingRepository repository, IWebHelper webHelper)
    {
        _repository = repository;
        _webHelper = webHelper;
    }

    public async Task HandleEventAsync(SitemapCreatedEvent eventMessage)
    {
        var pages = await _repository.GetAllAsync(CancellationToken.None);
        var storeLocation = _webHelper.GetStoreLocation().TrimEnd('/');

        var groups = pages
            .Where(page => page.IsIndexable)
            .GroupBy(page => new { page.Type, page.VehicleConfigurationId, page.ProductId });

        foreach (var group in groups)
        {
            var representative = group.First();
            var location = Absolute(storeLocation, representative.HreflangPathEn);
            if (eventMessage.SitemapUrls.Any(url =>
                    string.Equals(url.Location, location, StringComparison.OrdinalIgnoreCase)))
                continue;

            var alternateLocations = new[]
                {
                    Absolute(storeLocation, representative.HreflangPathEn),
                    Absolute(storeLocation, representative.HreflangPathAr)
                }
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            eventMessage.SitemapUrls.Add(new SitemapUrlModel(
                location,
                alternateLocations,
                UpdateFrequency.Weekly,
                group.Max(page => page.CreatedUtc)));
        }
    }

    private static string Absolute(string storeLocation, string relativePath)
        => storeLocation + "/" + relativePath.TrimStart('/');
}
