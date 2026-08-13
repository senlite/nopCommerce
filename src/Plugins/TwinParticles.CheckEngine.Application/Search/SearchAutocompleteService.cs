using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Queries;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Services;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Search;

/// <summary>
/// Composes typeahead suggestions from three sources (FR-415): vehicle aliases, OEM number prefixes
/// and product name/SKU prefixes. Each source is capped and failures are isolated so a single slow
/// or unavailable source never blocks the others.
/// </summary>
public sealed class SearchAutocompleteService
{
    private const int MinPrefixLength = 2;

    private readonly IProductSearchReadRepository _productSearchReadRepository;
    private readonly IOemSearchReadRepository _oemSearchReadRepository;
    private readonly IOemNormalizationService _oemNormalizationService;
    private readonly VehicleAliasApplicationService _vehicleAliasService;
    private readonly IBilingualSearchTextNormalizer _bilingualNormalizer;

    public SearchAutocompleteService(
        IProductSearchReadRepository productSearchReadRepository,
        IOemSearchReadRepository oemSearchReadRepository,
        IOemNormalizationService oemNormalizationService,
        VehicleAliasApplicationService vehicleAliasService,
        IBilingualSearchTextNormalizer bilingualNormalizer)
    {
        _productSearchReadRepository = productSearchReadRepository;
        _oemSearchReadRepository = oemSearchReadRepository;
        _oemNormalizationService = oemNormalizationService;
        _vehicleAliasService = vehicleAliasService;
        _bilingualNormalizer = bilingualNormalizer;
    }

    public async Task<AutocompleteResult> SuggestAsync(string term, string locale, int take, CancellationToken cancellationToken)
    {
        var trimmed = (term ?? string.Empty).Trim();
        if (trimmed.Length < MinPrefixLength)
            return new AutocompleteResult();

        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? "en" : locale;
        var perSource = take <= 0 ? 5 : Math.Min(take, 10);

        var vehicles = await SuggestVehiclesAsync(trimmed, normalizedLocale, perSource, cancellationToken);
        var oems = await SuggestOemsAsync(trimmed, perSource, cancellationToken);
        var products = await SuggestProductsAsync(trimmed, normalizedLocale, perSource, cancellationToken);

        return new AutocompleteResult
        {
            Vehicles = vehicles,
            Oems = oems,
            Products = products
        };
    }

    private async Task<IReadOnlyList<AutocompleteSuggestion>> SuggestVehiclesAsync(string term, string locale, int take, CancellationToken cancellationToken)
    {
        try
        {
            var aliases = await _vehicleAliasService.SearchAsync(new SearchVehicleAliasesQuery
            {
                Term = term,
                Locale = locale,
                Take = take
            }, cancellationToken);

            return aliases
                .Select(alias => new AutocompleteSuggestion
                {
                    Kind = "vehicle",
                    Label = alias.AliasText,
                    Value = alias.AliasText,
                    // Only configuration nodes resolve to a single fitment context; broader nodes
                    // (make/model/generation) become free-text vehicle suggestions.
                    VehicleConfigurationId = string.Equals(alias.NodeType, "configuration", StringComparison.OrdinalIgnoreCase)
                        ? alias.NodeId
                        : null
                })
                .Take(take)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<IReadOnlyList<AutocompleteSuggestion>> SuggestOemsAsync(string term, int take, CancellationToken cancellationToken)
    {
        try
        {
            var normalized = _oemNormalizationService.Normalize(term);
            if (string.IsNullOrWhiteSpace(normalized))
                return [];

            var matches = await _oemSearchReadRepository.FindByNormalizedPrefixAsync(normalized, take, cancellationToken);
            return matches
                .Select(match => new AutocompleteSuggestion
                {
                    Kind = "oem",
                    Label = match.DisplayNumber,
                    Value = match.DisplayNumber
                })
                .Take(take)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<IReadOnlyList<AutocompleteSuggestion>> SuggestProductsAsync(string term, string locale, int take, CancellationToken cancellationToken)
    {
        try
        {
            var normalized = _bilingualNormalizer.Normalize(term, locale);
            var hits = await _productSearchReadRepository.SuggestProductsAsync(normalized, locale, take, cancellationToken);
            return hits
                .Select(hit => new AutocompleteSuggestion
                {
                    Kind = "product",
                    Label = hit.Name,
                    Value = hit.Name,
                    ProductId = hit.ProductId
                })
                .Take(take)
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
