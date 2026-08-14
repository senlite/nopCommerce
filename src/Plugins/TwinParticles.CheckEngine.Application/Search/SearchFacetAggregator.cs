using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Search;

/// <summary>
/// Computes category, brand, price and fitment facets (FR-410) over the full filtered hit set.
/// Facets must be aggregated <em>before</em> pagination so the counts describe the whole result,
/// not just the current page.
/// </summary>
public static class SearchFacetAggregator
{
    // Ascending, non-overlapping price bands in store currency. The final open-ended band captures
    // everything above the last boundary.
    private static readonly decimal[] PriceBoundaries = [50m, 100m, 250m, 500m, 1000m];

    public static IReadOnlyList<SearchFacet> Aggregate(IReadOnlyList<SearchHit> hits, bool vehicleContextApplied)
    {
        if (hits.Count == 0)
            return [];

        var facets = new List<SearchFacet>();

        facets.AddRange(hits
            .Where(hit => hit.CategoryId.HasValue)
            .GroupBy(hit => hit.CategoryId!.Value)
            .Select(group => new SearchFacet
            {
                Key = "category",
                Value = group.Key.ToString(CultureInfo.InvariantCulture),
                Label = group.Select(hit => hit.CategoryName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)),
                Count = group.Count()
            })
            .OrderByDescending(facet => facet.Count)
            .ThenBy(facet => facet.Value));

        facets.AddRange(hits
            .Where(hit => !string.IsNullOrWhiteSpace(hit.Brand))
            .GroupBy(hit => hit.Brand!)
            .Select(group => new SearchFacet
            {
                Key = "brand",
                Value = group.Key,
                Label = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(facet => facet.Count)
            .ThenBy(facet => facet.Value));

        facets.AddRange(hits
            .Where(hit => hit.Price.HasValue)
            .GroupBy(hit => PriceBandValue(hit.Price!.Value))
            .Select(group => new SearchFacet
            {
                Key = "price",
                Value = group.Key,
                Label = group.Key,
                Count = group.Count()
            })
            .OrderBy(facet => PriceBandSortKey(facet.Value)));

        // Fitment status is only meaningful once a vehicle context has been applied; without it every
        // hit shares the same (unknown) status and the facet is noise.
        if (vehicleContextApplied)
        {
            facets.AddRange(hits
                .GroupBy(hit => hit.FitsActiveContext ? "fits" : "unverified")
                .Select(group => new SearchFacet
                {
                    Key = "fitment",
                    Value = group.Key,
                    Label = group.Key,
                    Count = group.Count()
                })
                .OrderBy(facet => facet.Value));
        }

        return facets;
    }

    private static string PriceBandValue(decimal price)
    {
        decimal lower = 0m;
        foreach (var boundary in PriceBoundaries)
        {
            if (price < boundary)
                return FormatBand(lower, boundary);

            lower = boundary;
        }

        return $"{FormatNumber(lower)}+";
    }

    private static string FormatBand(decimal lower, decimal upper)
        => $"{FormatNumber(lower)}-{FormatNumber(upper)}";

    private static string FormatNumber(decimal value)
        => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static decimal PriceBandSortKey(string value)
    {
        var head = value.Split('-', '+')[0];
        return decimal.TryParse(head, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : decimal.MaxValue;
    }
}
