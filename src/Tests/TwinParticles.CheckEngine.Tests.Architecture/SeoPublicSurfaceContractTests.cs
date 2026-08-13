using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Seo;
using TwinParticles.CheckEngine.Infrastructure.Seo;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// Guards the public SEO surface, reciprocal metadata, thin-page noindex policy, and durable sitemap
/// projection (H1.29-H1.30).
/// </summary>
[TestFixture]
public class SeoPublicSurfaceContractTests
{
    private static string ReadFileFromRepo(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }

    [Test]
    public void Public_Routes_Should_Expose_English_And_Arabic_Vehicle_And_Part_Pages()
    {
        var routes = ReadFileFromRepo(
            "TwinParticles.CheckEngine", "Infrastructure", "RouteProvider.cs");

        routes.Should().Contain("vehicles/config-{vehicleConfigurationId:int}");
        routes.Should().Contain("ar/vehicles/config-{vehicleConfigurationId:int}");
        routes.Should().Contain("parts/product-{productId:int}/for/config-{vehicleConfigurationId:int}");
        routes.Should().Contain("ar/parts/product-{productId:int}/for/config-{vehicleConfigurationId:int}");
    }

    [Test]
    public void Landing_View_Should_Emit_Canonical_Hreflang_Noindex_And_JsonLd()
    {
        var view = ReadFileFromRepo(
            "TwinParticles.CheckEngine", "Views", "Seo", "Landing.cshtml");

        view.Should().Contain("AddCanonicalUrlParts");
        view.Should().Contain("hreflang=\\\"en\\\"");
        view.Should().Contain("hreflang=\\\"ar\\\"");
        view.Should().Contain("hreflang=\\\"x-default\\\"");
        view.Should().Contain("noindex, follow");
        view.Should().Contain("AddJsonLdParts");
    }

    [Test]
    public void Host_Sitemap_Consumer_Should_Project_Durable_Landings()
    {
        var consumer = ReadFileFromRepo(
            "TwinParticles.CheckEngine", "Consumers", "SeoSitemapCreatedConsumer.cs");

        consumer.Should().Contain("IConsumer<SitemapCreatedEvent>");
        consumer.Should().Contain("_repository.GetAllAsync");
        consumer.Should().Contain("page.IsIndexable");
        consumer.Should().Contain("alternateLocations");
    }

    [Test]
    public void Indexability_Should_Require_A_Published_Sellable_Fit()
    {
        var policy = ReadFileFromRepo(
            "TwinParticles.CheckEngine.Infrastructure", "Seo", "SqlSeoIndexabilityPolicy.cs");

        policy.Should().Contain("c.FitmentStatusId = 1");
        policy.Should().Contain("c.IsPublished = 1");
        policy.Should().Contain("c.IsActive = 1");
        policy.Should().Contain("p.Published = 1");
        policy.Should().Contain("p.Deleted = 0");
    }

    [Test]
    public async Task Sitemap_Service_Should_Rehydrate_From_Repository_On_Every_Instance()
    {
        var repository = new FakeRepository([
            new SeoLandingPage { UrlPath = "/vehicles/config-1", IsIndexable = true },
            new SeoLandingPage { UrlPath = "/vehicles/config-2", IsIndexable = false }
        ]);

        var firstProcess = new SqlBackedSeoSitemapService(repository);
        var restartedProcess = new SqlBackedSeoSitemapService(repository);

        (await firstProcess.GetUrlsAsync(CancellationToken.None))
            .Should().ContainSingle().Which.Should().Be("/vehicles/config-1");
        (await restartedProcess.GetUrlsAsync(CancellationToken.None))
            .Should().ContainSingle().Which.Should().Be("/vehicles/config-1",
                "sitemap state comes from durable repository data, not process memory");
    }

    private sealed class FakeRepository : ISeoLandingRepository
    {
        private readonly IReadOnlyList<SeoLandingPage> _pages;

        public FakeRepository(IReadOnlyList<SeoLandingPage> pages)
        {
            _pages = pages;
        }

        public Task UpsertAsync(SeoLandingPage page, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<IReadOnlyList<SeoLandingPage>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult(_pages);
    }
}
