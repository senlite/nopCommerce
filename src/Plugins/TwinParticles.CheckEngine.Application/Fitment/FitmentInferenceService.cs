using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Application.Fitment;

public sealed class FitmentInferenceService
{
    public const decimal AiInferenceConfidenceCap = 0.5m;

    private readonly IAiCompletionPort? _aiCompletionPort;
    private readonly IFitmentClaimWriteRepository _writeRepository;
    private readonly IFitmentReviewQueueRepository _reviewQueueRepository;
    private readonly AiPromptResolver? _promptResolver;

    public FitmentInferenceService(
        IFitmentClaimWriteRepository writeRepository,
        IFitmentReviewQueueRepository reviewQueueRepository,
        IAiCompletionPort? aiCompletionPort = null,
        AiPromptResolver? promptResolver = null)
    {
        _writeRepository = writeRepository;
        _reviewQueueRepository = reviewQueueRepository;
        _aiCompletionPort = aiCompletionPort;
        _promptResolver = promptResolver;
    }

    public async Task<FitmentClaim?> InferCandidateAsync(
        int productId,
        int vehicleConfigurationId,
        string productName,
        CancellationToken cancellationToken,
        string actor = "ai.inference")
    {
        if (productId <= 0 || vehicleConfigurationId <= 0 || _aiCompletionPort is null)
            return null;

        var prompt = _promptResolver?.Format(AiFeatureKeys.FitmentInference, new Dictionary<string, string?>
        {
            ["productId"] = productId.ToString(),
            ["configurationId"] = vehicleConfigurationId.ToString(),
            ["attributes"] = productName
        }) ?? $"""
            Given product "{productName}" (id {productId}) and vehicle configuration {vehicleConfigurationId},
            respond with one word: Fits, DoesNotFit, or Unknown.
            """;

        var result = await _aiCompletionPort.CompleteAsync(new AiCompletionRequest
        {
            FeatureKey = AiFeatureKeys.FitmentInference,
            PromptKey = AiFeatureKeys.FitmentInference,
            Prompt = prompt,
            MaxTokens = 8,
            Temperature = 0
        }, cancellationToken);

        if (!result.Success)
            return null;

        var status = ParseStatus(result.Text);
        var claim = new FitmentClaim
        {
            ProductId = productId,
            VehicleConfigurationId = vehicleConfigurationId,
            Status = status,
            Confidence = AiInferenceConfidenceCap,
            SafetyClass = SafetyClass.Standard,
            IsPublished = false,
            IsActive = true,
            Provenance = new FitmentClaimProvenance
            {
                SourceKind = FitmentSourceKind.AiInference,
                SourceReference = result.PromptHash,
                CreatedBy = actor,
                CreatedUtc = DateTimeOffset.UtcNow
            }
        };

        await _writeRepository.UpsertAsync(claim, cancellationToken);
        if (claim.Id > 0)
            await _reviewQueueRepository.EnqueueAsync(claim.Id, "fitment.ai_inference", cancellationToken);

        return claim;
    }

    private static FitmentStatus ParseStatus(string text)
    {
        var structured = TryParseStructuredVerdict(text);
        if (structured.HasValue)
            return structured.Value;

        if (text.Contains("DoesNotFit", StringComparison.OrdinalIgnoreCase)
            || text.Contains("does not fit", StringComparison.OrdinalIgnoreCase))
        {
            return FitmentStatus.DoesNotFit;
        }

        if (text.Contains("Fits", StringComparison.OrdinalIgnoreCase))
            return FitmentStatus.Fits;

        return FitmentStatus.Unknown;
    }

    private static FitmentStatus? TryParseStructuredVerdict(string text)
    {
        try
        {
            var json = ExtractJson(text);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    if (element.TryGetProperty("verdict", out var verdict))
                        return MapVerdict(verdict.GetString());
                }

                return null;
            }

            if (root.TryGetProperty("verdict", out var singleVerdict))
                return MapVerdict(singleVerdict.GetString());
        }
        catch
        {
            /* fall back to token parsing */
        }

        return null;
    }

    private static FitmentStatus? MapVerdict(string? verdict)
    {
        if (string.IsNullOrWhiteSpace(verdict))
            return null;

        if (verdict.Contains("DoesNotFit", StringComparison.OrdinalIgnoreCase)
            || verdict.Contains("does not fit", StringComparison.OrdinalIgnoreCase))
        {
            return FitmentStatus.DoesNotFit;
        }

        if (verdict.Contains("Fits", StringComparison.OrdinalIgnoreCase))
            return FitmentStatus.Fits;

        if (verdict.Contains("Unknown", StringComparison.OrdinalIgnoreCase))
            return FitmentStatus.Unknown;

        return null;
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        if (start < 0)
            start = text.IndexOf('[');

        var end = text.LastIndexOf('}');
        if (end < start)
            end = text.LastIndexOf(']');

        if (start >= 0 && end > start)
            return text[start..(end + 1)];

        return text;
    }
}
