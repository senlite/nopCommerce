using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Models;

public sealed class SeoLandingPageModel
{
    public string Title { get; init; } = string.Empty;

    public string Heading { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string CanonicalUrl { get; init; } = string.Empty;

    public string HreflangUrlEn { get; init; } = string.Empty;

    public string HreflangUrlAr { get; init; } = string.Empty;

    public string StructuredDataJsonLd { get; init; } = string.Empty;

    public bool IsIndexable { get; init; }

    public bool IsPartLanding { get; init; }

    public IReadOnlyList<SeoLandingProductModel> Products { get; init; } = [];
}

public sealed class SeoLandingProductModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;
}
