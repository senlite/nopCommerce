using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Search;

public sealed class SemanticSearchService
{
    private readonly IAiEmbeddingPort? _embeddingPort;
    private readonly IAiFeatureToggle? _featureToggle;
    private readonly ISearchEmbeddingIndex? _embeddingIndex;

    public SemanticSearchService(
        ISearchEmbeddingIndex? embeddingIndex = null,
        IAiEmbeddingPort? embeddingPort = null,
        IAiFeatureToggle? featureToggle = null)
    {
        _embeddingIndex = embeddingIndex;
        _embeddingPort = embeddingPort;
        _featureToggle = featureToggle;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        if (_embeddingPort is null || _embeddingIndex is null)
            return [];

        if (_featureToggle is not null && !_featureToggle.IsEnabled(AiFeatureKeys.SearchSemantic))
            return [];

        var text = query.RawText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return [];

        if (!await _embeddingIndex.IsReadyAsync(query.Locale, cancellationToken))
            return [];

        var embedding = await _embeddingPort.EmbedAsync(new AiEmbeddingRequest
        {
            FeatureKey = AiFeatureKeys.SearchSemantic,
            Text = text,
            Locale = query.Locale
        }, cancellationToken);

        if (!embedding.Success || embedding.Vector.Length == 0)
            return [];

        var take = query.PageSize > 0 ? query.PageSize : 24;
        return await _embeddingIndex.SearchSimilarAsync(embedding.Vector, query.Locale, take, cancellationToken);
    }
}
