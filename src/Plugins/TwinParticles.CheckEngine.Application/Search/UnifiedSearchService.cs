using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Search;

public sealed class UnifiedSearchService
{
    private readonly NaturalLanguageIntentParser _naturalLanguageIntentParser;
    private readonly IBilingualSearchTextNormalizer _bilingualNormalizer;
    private readonly FitmentEvaluationService _fitmentEvaluationService;
    private readonly OemResolveService _oemResolveService;
    private readonly IProductSearchReadRepository _productSearchReadRepository;
    private readonly ISearchIndexHealthService _searchIndexHealthService;
    private readonly ISearchAnalyticsService? _searchAnalyticsService;
    private readonly VinDecodeApplicationService _vinDecodeService;

    public UnifiedSearchService(
        IProductSearchReadRepository productSearchReadRepository,
        VinDecodeApplicationService vinDecodeService,
        OemResolveService oemResolveService,
        FitmentEvaluationService fitmentEvaluationService,
        ISearchIndexHealthService searchIndexHealthService,
        IBilingualSearchTextNormalizer bilingualNormalizer,
        NaturalLanguageIntentParser naturalLanguageIntentParser,
        ISearchAnalyticsService? searchAnalyticsService = null)
    {
        _productSearchReadRepository = productSearchReadRepository;
        _vinDecodeService = vinDecodeService;
        _oemResolveService = oemResolveService;
        _fitmentEvaluationService = fitmentEvaluationService;
        _searchIndexHealthService = searchIndexHealthService;
        _bilingualNormalizer = bilingualNormalizer;
        _naturalLanguageIntentParser = naturalLanguageIntentParser;
        _searchAnalyticsService = searchAnalyticsService;
    }

    public async Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var normalizedText = _bilingualNormalizer.Normalize(query.RawText, query.Locale);
        var mode = await ResolveModeAsync(query, normalizedText, cancellationToken);

        var healthy = await _searchIndexHealthService.IsHealthyAsync(cancellationToken);
        var degraded = !healthy;

        IReadOnlyList<SearchHit> hits;
        var modeUsed = mode;

        if (mode == SearchMode.NaturalLanguage)
        {
            hits = await SearchNaturalLanguageAsync(query, normalizedText, cancellationToken);
            modeUsed = SearchMode.NaturalLanguage;
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

        // Drill-down filters are applied uniformly here so facet selections work across every lane
        // (vehicle-tree and OEM projections never saw brand/price filters at the repository).
        var filtered = ApplyFacetFilters(hits, query.Filters);

        // Facets and total describe the whole filtered result, so both are computed before paging.
        var facets = SearchFacetAggregator.Aggregate(filtered, query.VehicleConfigurationId.HasValue);
        var total = filtered.Count;

        var ranked = filtered
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.ProductId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var recovery = total == 0 ? BuildRecovery(query) : [];

        long? analyticsId = null;
        if (_searchAnalyticsService is not null)
        {
            try
            {
                stopwatch.Stop();
                analyticsId = await _searchAnalyticsService.RecordSearchAsync(
                    normalizedText,
                    modeUsed,
                    query.Locale,
                    total,
                    query.VehicleConfigurationId.HasValue,
                    query.WidenFitment,
                    degraded,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken);
            }
            catch (System.OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // Analytics is non-critical: search results must remain available if its store fails.
            }
        }

        return new SearchResult
        {
            ModeUsed = modeUsed,
            Hits = ranked,
            Total = total,
            Facets = facets,
            Suggestions = recovery.Select(action => action.Label).ToList(),
            Recovery = recovery,
            IsDegraded = degraded,
            AnalyticsId = analyticsId
        };
    }

    private static IReadOnlyList<SearchHit> ApplyFacetFilters(IReadOnlyList<SearchHit> hits, SearchFilters filters)
    {
        IEnumerable<SearchHit> filtered = hits;

        if (filters.CategoryId is > 0)
            filtered = filtered.Where(hit => hit.CategoryId == filters.CategoryId);

        if (!string.IsNullOrWhiteSpace(filters.Brand))
            filtered = filtered.Where(hit =>
                string.Equals(hit.Brand, filters.Brand, System.StringComparison.OrdinalIgnoreCase));

        if (filters.PriceMin.HasValue)
            filtered = filtered.Where(hit => hit.Price is null || hit.Price >= filters.PriceMin.Value);

        if (filters.PriceMax.HasValue)
            filtered = filtered.Where(hit => hit.Price is null || hit.Price <= filters.PriceMax.Value);

        return filtered as IReadOnlyList<SearchHit> ?? filtered.ToList();
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

        var wordCount = normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        if (wordCount >= 2)
            return SearchMode.NaturalLanguage;

        return SearchMode.Keyword;
    }

    private async Task<IReadOnlyList<SearchHit>> SearchNaturalLanguageAsync(
        SearchQuery query,
        string normalizedText,
        CancellationToken cancellationToken)
    {
        var intent = await _naturalLanguageIntentParser.ParseAsync(normalizedText, query.Locale, cancellationToken);

        if (intent.VehicleConfigurationId is > 0)
        {
            return await _productSearchReadRepository.SearchByVehicleTreeAsync(
                CloneQuery(query, string.Empty, SearchMode.VehicleTree, intent.VehicleConfigurationId),
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(intent.OemNumber))
        {
            return await SearchOemAsync(query, intent.OemNumber, cancellationToken);
        }

        var keywords = intent.PartTerms.Count > 0
            ? string.Join(' ', intent.PartTerms)
            : intent.KeywordFallback;

        if (string.IsNullOrWhiteSpace(keywords))
            keywords = normalizedText;

        return await _productSearchReadRepository.SearchKeywordAsync(
            CloneQuery(query, keywords, SearchMode.Keyword),
            cancellationToken);
    }

    private async Task<IReadOnlyList<SearchHit>> SearchVinAsync(SearchQuery query, string normalizedText, CancellationToken cancellationToken)
    {
        var decode = await _vinDecodeService.DecodeAsync(normalizedText, cancellationToken);
        if (!string.Equals(decode.Outcome, "SingleMatch", StringComparison.Ordinal) || decode.Candidates.Count != 1)
            return [];

        // A VIN is vehicle context, not a product keyword. Search the published fitment projection
        // for the one decoded configuration; passing the 17-character VIN into keyword search
        // cannot match a catalog title and previously fell through to invented demo products.
        var vehicleQuery = CloneQuery(
            query,
            string.Empty,
            SearchMode.VehicleTree,
            decode.Candidates[0].VehicleConfigurationId);

        return await _productSearchReadRepository.SearchByVehicleTreeAsync(vehicleQuery, cancellationToken);
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

            // The widened lane carries unverified results only; they stay unbadged because
            // FitsActiveContext is false, so a part is never presented as a confirmed fit.
            var unverified = fitment.Outcome is FitmentStatus.Unknown or FitmentStatus.NeedsDisambiguation;

            if (fitment.Outcome == FitmentStatus.Fits || (widenFitment && unverified))
                filtered.Add(hit);
        }

        return filtered;
    }

    private static IReadOnlyList<SearchRecoveryAction> BuildRecovery(SearchQuery query)
    {
        var recovery = new List<SearchRecoveryAction>();

        // A vehicle-scoped miss is most often over-strict fitment; offer the widen lane first.
        if (query.VehicleConfigurationId.HasValue && !query.WidenFitment)
            recovery.Add(new SearchRecoveryAction
            {
                Kind = "widen_fitment",
                Label = "Include parts with unconfirmed fit for your vehicle."
            });

        recovery.Add(new SearchRecoveryAction
        {
            Kind = "select_vehicle",
            Label = "Select your vehicle to see parts that fit."
        });

        recovery.Add(new SearchRecoveryAction
        {
            Kind = "broaden_keyword",
            Label = "Check the OEM/VIN format or try a broader keyword."
        });

        return recovery;
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
