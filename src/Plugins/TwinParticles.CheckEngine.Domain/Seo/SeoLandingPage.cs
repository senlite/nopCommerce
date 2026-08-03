using System;

namespace TwinParticles.CheckEngine.Domain.Seo;

public sealed class SeoLandingPage
{
    public int Id { get; set; }

    public SeoLandingPageType Type { get; set; }

    public int? VehicleConfigurationId { get; set; }

    public int? ProductId { get; set; }

    public string Locale { get; set; } = "en";

    public string UrlPath { get; set; } = string.Empty;

    public string CanonicalUrlPath { get; set; } = string.Empty;

    public string HreflangPathEn { get; set; } = string.Empty;

    public string HreflangPathAr { get; set; } = string.Empty;

    public string StructuredDataJsonLd { get; set; } = string.Empty;

    public bool IsIndexable { get; set; }

    public DateTime CreatedUtc { get; set; }
}
