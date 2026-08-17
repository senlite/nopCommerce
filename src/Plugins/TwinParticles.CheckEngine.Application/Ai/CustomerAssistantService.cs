using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Ai;

public sealed class CustomerAssistantService
{
    private readonly IAiCompletionPort? _aiCompletionPort;
    private readonly IProductSearchReadRepository? _productSearchReadRepository;
    private readonly SemanticSearchService? _semanticSearchService;
    private readonly FitmentEvaluationService? _fitmentEvaluationService;
    private readonly AiPromptResolver? _promptResolver;

    public CustomerAssistantService(
        IAiCompletionPort? aiCompletionPort = null,
        IProductSearchReadRepository? productSearchReadRepository = null,
        AiPromptResolver? promptResolver = null,
        SemanticSearchService? semanticSearchService = null,
        FitmentEvaluationService? fitmentEvaluationService = null)
    {
        _aiCompletionPort = aiCompletionPort;
        _productSearchReadRepository = productSearchReadRepository;
        _promptResolver = promptResolver;
        _semanticSearchService = semanticSearchService;
        _fitmentEvaluationService = fitmentEvaluationService;
    }

    public async Task<CustomerAssistantResponse> AskAsync(
        string question,
        int? vehicleConfigurationId,
        CancellationToken cancellationToken,
        string locale = "en")
    {
        var normalizedQuestion = question?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedQuestion))
        {
            return new CustomerAssistantResponse
            {
                Answer = string.Empty,
                ErrorCode = "assistant.empty_question"
            };
        }

        if (_aiCompletionPort is null || _productSearchReadRepository is null)
        {
            return new CustomerAssistantResponse
            {
                Answer = string.Empty,
                ErrorCode = "ai.disabled"
            };
        }

        var catalogContext = await BuildCatalogContextAsync(
            normalizedQuestion,
            vehicleConfigurationId,
            locale,
            cancellationToken);
        if (catalogContext.Citations.Count == 0)
        {
            return new CustomerAssistantResponse
            {
                Answer = vehicleConfigurationId.HasValue
                    ? "I could not find catalog items verified to fit your active vehicle for that question."
                    : "I could not find matching catalog items for that question.",
                Grounded = true,
                VehicleScoped = vehicleConfigurationId.HasValue
            };
        }

        var redactedQuestion = AiPromptPrivacy.RedactVins(normalizedQuestion);
        var contextBlock = AssistantCitationFormatter.BuildContextBlock(catalogContext.Citations);
        var prompt = _promptResolver is not null
            ? _promptResolver.Format(AiFeatureKeys.CustomerAssistant, new Dictionary<string, string?>
            {
                ["context"] = contextBlock,
                ["question"] = redactedQuestion
            })
            : BuildFallbackPrompt(redactedQuestion, contextBlock);

        var result = await _aiCompletionPort.CompleteAsync(new AiCompletionRequest
        {
            FeatureKey = AiFeatureKeys.CustomerAssistant,
            PromptKey = AiFeatureKeys.CustomerAssistant,
            Prompt = prompt,
            MaxTokens = 256,
            Temperature = 0
        }, cancellationToken);

        if (!result.Success)
        {
            return new CustomerAssistantResponse
            {
                Answer = string.Empty,
                ErrorCode = result.ErrorCode ?? "ai.completion_failed"
            };
        }

        return new CustomerAssistantResponse
        {
            Answer = result.Text,
            Grounded = true,
            Citations = catalogContext.Citations,
            VehicleScoped = catalogContext.VehicleScoped
        };
    }

    private sealed record CatalogContextResult(IReadOnlyList<AssistantCitation> Citations, bool VehicleScoped);

    private static string BuildFallbackPrompt(string question, string contextBlock) =>
        $"""
            Answer the shopper question using ONLY the catalog lines below.
            If the answer is not in the context, say you do not know.
            Never invent part numbers or prices.
            Question: {question}
            Catalog:
            {contextBlock}
            """;

    private async Task<CatalogContextResult> BuildCatalogContextAsync(
        string question,
        int? vehicleConfigurationId,
        string locale,
        CancellationToken cancellationToken)
    {
        var hits = await RetrieveQuestionRelevantHitsAsync(question, locale, cancellationToken);
        var vehicleScoped = vehicleConfigurationId.HasValue && _fitmentEvaluationService is not null;
        hits = await FilterToVerifiedFitAsync(hits, vehicleConfigurationId, cancellationToken);

        var citations = hits
            .Take(5)
            .Select(AssistantCitationFormatter.FromHit)
            .ToList();

        return new CatalogContextResult(citations, vehicleScoped);
    }

    private async Task<IReadOnlyList<SearchHit>> RetrieveQuestionRelevantHitsAsync(
        string question,
        string locale,
        CancellationToken cancellationToken)
    {
        if (_semanticSearchService is not null)
        {
            var semanticHits = await _semanticSearchService.SearchAsync(new SearchQuery
            {
                RawText = question,
                Mode = SearchMode.Semantic,
                Locale = locale,
                Page = 1,
                PageSize = 8
            }, cancellationToken);

            if (semanticHits.Count > 0)
                return semanticHits;
        }

        // Keyword search matches the query as a phrase, so a conversational sentence finds nothing.
        // Try progressively less precise retrieval terms and stop at the first that returns catalog rows.
        foreach (var candidate in AssistantRetrievalQueryBuilder.BuildCandidates(question))
        {
            var hits = await _productSearchReadRepository!.SearchKeywordAsync(new SearchQuery
            {
                RawText = candidate,
                Mode = SearchMode.Keyword,
                Locale = locale,
                Page = 1,
                PageSize = 8
            }, cancellationToken);

            if (hits.Count > 0)
                return hits;
        }

        return [];
    }

    private async Task<IReadOnlyList<SearchHit>> FilterToVerifiedFitAsync(
        IReadOnlyList<SearchHit> hits,
        int? vehicleConfigurationId,
        CancellationToken cancellationToken)
    {
        if (!vehicleConfigurationId.HasValue || _fitmentEvaluationService is null)
            return hits;

        var fitting = new List<SearchHit>();
        foreach (var hit in hits)
        {
            var fitment = await _fitmentEvaluationService.EvaluateAsync(new FitmentEvaluationContext
            {
                ProductId = hit.ProductId,
                VehicleConfigurationId = vehicleConfigurationId.Value
            }, cancellationToken);

            if (fitment.Outcome == FitmentStatus.Fits)
                fitting.Add(hit);
        }

        return fitting;
    }
}

public sealed class CustomerAssistantResponse
{
    public string Answer { get; init; } = string.Empty;

    public bool Grounded { get; init; }

    public string? ErrorCode { get; init; }

    public IReadOnlyList<AssistantCitation> Citations { get; init; } = [];

    public bool VehicleScoped { get; init; }
}
