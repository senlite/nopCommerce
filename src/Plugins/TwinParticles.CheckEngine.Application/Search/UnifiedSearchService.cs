using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Search;

public sealed class UnifiedSearchService
{
    private readonly IAiCompletionPort? _aiCompletionPort;
    private readonly IBilingualSearchTextNormalizer _bilingualNormalizer;
    private readonly FitmentEvaluationService _fitmentEvaluationService;
    private readonly OemResolveService _oemResolveService;
    private readonly IProductSearchReadRepository _productSearchReadRepository;
    private readonly ISearchIndexHealthService _searchIndexHealthService;
    private readonly VinDecodeApplicationService _vinDecodeService;

    public UnifiedSearchService(
        IProductSearchReadRepository productSearchReadRepository,
        VinDecodeApplicationService vinDecodeService,
        OemResolveService oemResolveService,
        FitmentEvaluationService fitmentEvaluationService,
        ISearchIndexHealthService searchIndexHealthService,
        IBilingualSearchTextNormalizer bilingualNormalizer,
        IAiCompletionPort? aiCompletionPort = null)
    {
        _productSearchReadRepository = productSearchReadRepository;
        _vinDecodeService = vinDecodeService;
        _oemResolveService = oemResolveService;
        _fitmentEvaluationService = fitmentEvaluationService;
        _searchIndexHealthService = searchIndexHealthService;
        _bilingualNormalizer = bilingualNormalizer;
        _aiCompletionPort = aiCompletionPort;
    }

    public async Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var normalizedText = _bilingualNormalizer.Normalize(query.RawText, query.Locale);
        var mode = await ResolveModeAsync(query, normalizedText, cancellationToken);

        var healthy = await _searchIndexHealthService.IsHealthyAsync(cancellationToken);
        var degraded = !healthy;

        IReadOnlyList<SearchHit> hits;
        var modeUsed = mode;

        if (mode == SearchMode.NaturalLanguage)
        {
            var (keywords, succeeded) = await ExtractNaturalLanguageKeywordsAsync(normalizedText, cancellationToken);
            if (succeeded)
            {
                hits = await _productSearchReadRepository.SearchKeywordAsync(
                    CloneQuery(query, keywords, SearchMode.Keyword),
                    cancellationToken);
                modeUsed = SearchMode.NaturalLanguage;
            }
            else
            {
                hits = await _productSearchReadRepository.SearchKeywordAsync(
                    CloneQuery(query, normalizedText, SearchMode.Keyword),
                    cancellationToken);
                modeUsed = SearchMode.Keyword;
            }
        }
        else
        {
            hits = mode switch
            {
                SearchMode.Vin => await SearchVinAsync(query, normalizedText, cancellationToken),
                SearchMode.Oem => await SearchOemAsync(query, normalizedText, cancellationToken),
                SearchMode.VehicleTree => await _productSearchReadRepository.SearchByVehicleTreeAsync(query, cancellationToken),
                SearchMode.Category => await _productSearchReadRepository.SearchByCategoryAsync(query, cancellationToken),
                _ => await _productSearchReadRepository.SearchKeywordAsync(CloneQuery(query, normalizedText, query.Mode), cancellationToken)
            };
        }

        if (degraded && hits.Count == 0)
        {
            hits = await _productSearchReadRepository.SearchKeywordAsync(CloneQuery(query, query.RawText, SearchMode.Keyword), cancellationToken);
        }

        if (query.VehicleConfigurationId.HasValue)
            hits = await ApplyFitmentFilterAsync(hits, query.VehicleConfigurationId.Value, query.WidenFitment, cancellationToken);

        var ranked = hits
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.ProductId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var facets = ranked
            .Where(x => x.CategoryId.HasValue)
            .GroupBy(x => x.CategoryId!.Value)
            .Select(group => new SearchFacet
            {
                Key = "categoryId",
                Value = group.Key.ToString(),
                Count = group.Count()
            })
            .ToList();

        var suggestions = ranked.Count == 0
            ? BuildZeroResultSuggestions(query)
            : new List<string>();

        return new SearchResult
        {
            ModeUsed = modeUsed,
            Hits = ranked,
            Facets = facets,
            Suggestions = suggestions,
            IsDegraded = degraded
        };
    }

    public Task RebuildIndexAsync(CancellationToken cancellationToken)
    {
        return _searchIndexHealthService.RebuildAsync(cancellationToken);
    }

    private async Task<SearchMode> ResolveModeAsync(SearchQuery query, string normalizedText, CancellationToken cancellationToken)
    {
        if (query.Mode != SearchMode.Auto)
            return query.Mode;

        if (TwinParticles.CheckEngine.Domain.Vehicle.Vin.TryCreate(normalizedText, out _, out _, enforceCheckDigit: true))
            return SearchMode.Vin;

        var oemResolved = await _oemResolveService.ResolveAsync(new OemResolveQuery { Number = normalizedText }, cancellationToken);
        if (oemResolved.Success)
            return SearchMode.Oem;

        return SearchMode.Keyword;
    }

    private async Task<(string Keywords, bool Succeeded)> ExtractNaturalLanguageKeywordsAsync(string text, CancellationToken cancellationToken)
    {
        if (_aiCompletionPort is null)
            return (text, false);

        try
        {
            var result = await _aiCompletionPort.CompleteAsync(new AiCompletionRequest
            {
                PromptKey = "search.natural_language",
                Prompt = $"Extract plain search keywords from this automotive query. Return only keywords, no punctuation.\nQuery: {text}",
                MaxTokens = 64,
                Temperature = 0
            }, cancellationToken);

            if (!result.Success || string.IsNullOrWhiteSpace(result.Text))
                return (text, false);

            return (result.Text.Trim(), true);
        }
        catch
        {
            return (text, false);
        }
    }

    private async Task<IReadOnlyList<SearchHit>> SearchVinAsync(SearchQuery query, string normalizedText, CancellationToken cancellationToken)
    {
        var decode = await _vinDecodeService.DecodeAsync(normalizedText, cancellationToken);
        var nextVehicleConfigurationId = query.VehicleConfigurationId;

        if (decode.Candidates.Count > 0)
            nextVehicleConfigurationId = decode.Candidates[0].VehicleConfigurationId;

        var keywordQuery = CloneQuery(query, normalizedText, SearchMode.Keyword, nextVehicleConfigurationId);
        return await _productSearchReadRepository.SearchKeywordAsync(keywordQuery, cancellationToken);
    }

    private async Task<IReadOnlyList<SearchHit>> SearchOemAsync(SearchQuery query, string normalizedText, CancellationToken cancellationToken)
    {
        var resolved = await _oemResolveService.ResolveAsync(new OemResolveQuery
        {
            Number = normalizedText
        }, cancellationToken);

        if (!resolved.Success || !resolved.OemNumberId.HasValue)
            return [];

        return await _productSearchReadRepository.SearchByOemIdAsync(resolved.OemNumberId.Value, query, cancellationToken);
    }

    private async Task<IReadOnlyList<SearchHit>> ApplyFitmentFilterAsync(IReadOnlyList<SearchHit> hits, int vehicleConfigurationId, bool widenFitment, CancellationToken cancellationToken)
    {
        var filtered = new List<SearchHit>();

        foreach (var hit in hits)
        {
            var fitment = await _fitmentEvaluationService.EvaluateAsync(new FitmentEvaluationContext
            {
                ProductId = hit.ProductId,
                VehicleConfigurationId = vehicleConfigurationId
            }, cancellationToken);

            hit.FitsActiveContext = fitment.Outcome == FitmentStatus.Fits;

            if (fitment.Outcome == FitmentStatus.Fits || (widenFitment && fitment.Outcome == FitmentStatus.Unknown))
                filtered.Add(hit);
        }

        return filtered;
    }

    private static IReadOnlyList<string> BuildZeroResultSuggestions(SearchQuery query)
    {
        var suggestions = new List<string>();

        if (!query.WidenFitment)
            suggestions.Add("Try widening fitment to include unknown compatibility results.");

        suggestions.Add("Check OEM/VIN format or use a broader keyword.");
        return suggestions;
    }

    private static SearchQuery CloneQuery(SearchQuery query, string rawText, SearchMode mode, int? vehicleConfigurationId = null)
    {
        return new SearchQuery
        {
            RawText = rawText,
            Mode = mode,
            VehicleConfigurationId = vehicleConfigurationId ?? query.VehicleConfigurationId,
            WidenFitment = query.WidenFitment,
            Filters = query.Filters,
            Page = query.Page,
            PageSize = query.PageSize,
            Locale = query.Locale
        };
    }
}
