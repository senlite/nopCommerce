using System.Net;
using System.Text;

namespace Nop.Plugin.Misc.GMaster.Services;

/// <summary>
/// Creates original, trademark-free category illustrations.
/// These are bundled data-URI-free SVGs rather than unreliable third-party hotlinks.
/// </summary>
public sealed class GMasterImageFactory
{
    public byte[] CreateCategorySvg(GMasterCategoryDefinition category)
    {
        var title = WebUtility.HtmlEncode(category.EnglishName);
        var ArabicTitle = WebUtility.HtmlEncode(category.ArabicName);
        var glyph = WebUtility.HtmlEncode(category.GlyphPath);

        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="1200" height="800" viewBox="0 0 1200 800" role="img" aria-labelledby="title desc">
              <title id="title">{title}</title>
              <desc id="desc">Original GMaster category illustration</desc>
              <defs>
                <linearGradient id="background" x1="0" y1="0" x2="1" y2="1">
                  <stop offset="0" stop-color="#08151f"/>
                  <stop offset="1" stop-color="#123d50"/>
                </linearGradient>
                <linearGradient id="accent" x1="0" y1="0" x2="1" y2="1">
                  <stop offset="0" stop-color="#24c4d8"/>
                  <stop offset="1" stop-color="#f2b544"/>
                </linearGradient>
              </defs>
              <rect width="1200" height="800" rx="48" fill="url(#background)"/>
              <circle cx="930" cy="160" r="220" fill="#24c4d8" opacity=".08"/>
              <circle cx="180" cy="690" r="260" fill="#f2b544" opacity=".06"/>
              <g transform="translate(364 105) scale(4)" fill="none" stroke="url(#accent)" stroke-width="3.25" stroke-linecap="round" stroke-linejoin="round">
                <path d="{glyph}"/>
              </g>
              <text x="600" y="610" text-anchor="middle" font-family="Arial, sans-serif" font-size="54" font-weight="700" fill="#f4f7f9">{title}</text>
              <text x="600" y="680" text-anchor="middle" direction="rtl" font-family="Arial, sans-serif" font-size="42" fill="#bdcbd2">{ArabicTitle}</text>
              <text x="600" y="744" text-anchor="middle" font-family="Arial, sans-serif" font-size="22" letter-spacing="7" fill="#24c4d8">GMASTER PARTS</text>
            </svg>
            """;

        return Encoding.UTF8.GetBytes(svg);
    }
}
