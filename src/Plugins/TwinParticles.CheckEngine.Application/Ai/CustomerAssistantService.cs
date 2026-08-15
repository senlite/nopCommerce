using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Ai;

public sealed class CustomerAssistantService
{
    private readonly IAiCompletionPort? _aiCompletionPort;
    private readonly IProductSearchReadRepository? _productSearchReadRepository;
    private readonly AiPromptResolver? _promptResolver;

    public CustomerAssistantService(
        IAiCompletionPort? aiCompletionPort = null,
        IProductSearchReadRepository? productSearchReadRepository = null,
        AiPromptResolver? promptResolver = null)
    {
        _aiCompletionPort = aiCompletionPort;
        _productSearchReadRepository = productSearchReadRepository;
        _promptResolver = promptResolver;
    }

    public async Task<CustomerAssistantResponse> AskAsync(
        string question,
        int? vehicleConfigurationId,
        CancellationToken cancellationToken)
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

        var catalogContext = await BuildCatalogContextAsync(normalizedQuestion, vehicleConfigurationId, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        var query = new SearchQuery
        {
            RawText = question,
            Mode = SearchMode.Keyword,
            VehicleConfigurationId = vehicleConfigurationId,
            Page = 1,
            PageSize = 5
        };

        var hits = vehicleConfigurationId.HasValue
            ? await _productSearchReadRepository!.SearchByVehicleTreeAsync(query, cancellationToken)
            : await _productSearchReadRepository!.SearchKeywordAsync(query, cancellationToken);

        return hits
            .Take(5)
            .Select(hit => $"ProductId={hit.ProductId}; Name={hit.Name}")
            .ToList();
    }
}

public sealed class CustomerAssistantResponse
{
    public string Answer { get; init; } = string.Empty;

    public bool Grounded { get; init; }

    public string? ErrorCode { get; init; }

    public IReadOnlyList<string> Citations { get; init; } = [];
}
