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
        if (catalogContext.Count == 0)
        {
            return new CustomerAssistantResponse
            {
                Answer = "I could not find matching catalog items for that question.",
                Grounded = true
            };
        }

        var redactedQuestion = AiPromptPrivacy.RedactVins(normalizedQuestion);
        var contextBlock = string.Join('\n', catalogContext.Select((line, index) => $"{index + 1}. {line}"));
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
            Citations = catalogContext
        };
    }

    private static string BuildFallbackPrompt(string question, string contextBlock) =>
        $"""
            Answer the shopper question using ONLY the catalog lines below.
            If the answer is not in the context, say you do not know.
            Never invent part numbers or prices.
            Question: {question}
            Catalog:
            {contextBlock}
            """;

    private async Task<IReadOnlyList<string>> BuildCatalogContextAsync(
        string question,
        int? vehicleConfigurationId,
        string locale,
        CancellationToken cancellationToken)
    {
        var hits = await RetrieveQuestionRelevantHitsAsync(question, locale, cancellationToken);
        hits = await FilterToVerifiedFitAsync(hits, vehicleConfigurationId, cancellationToken);

        return hits
            .Take(5)
            .Select(FormatCitation)
            .ToList();
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

        return await _productSearchReadRepository!.SearchKeywordAsync(new SearchQuery
        {
            RawText = question,
            Mode = SearchMode.Keyword,
            Locale = locale,
            Page = 1,
            PageSize = 8
        }, cancellationToken);
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

    private static string FormatCitation(SearchHit hit)
    {
        var parts = new List<string> { $"ProductId={hit.ProductId}", $"Name={hit.Name}" };
        if (!string.IsNullOrWhiteSpace(hit.Brand))
            parts.Add($"Brand={hit.Brand}");
        if (hit.Price.HasValue)
            parts.Add($"Price={hit.Price.Value:0.##}");

        return string.Join("; ", parts);
    }
}

public sealed class CustomerAssistantResponse
{
    public string Answer { get; init; } = string.Empty;

    public bool Grounded { get; init; }

    public string? ErrorCode { get; init; }

    public IReadOnlyList<string> Citations { get; init; } = [];
}
