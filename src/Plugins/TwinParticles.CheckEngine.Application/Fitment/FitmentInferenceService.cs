using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Application.Fitment;

public sealed class FitmentInferenceService
{
    public const decimal AiInferenceConfidenceCap = 0.5m;

    private readonly IAiCompletionPort? _aiCompletionPort;
    private readonly IFitmentClaimWriteRepository _writeRepository;
    private readonly IFitmentReviewQueueRepository _reviewQueueRepository;

    public FitmentInferenceService(
        IFitmentClaimWriteRepository writeRepository,
        IFitmentReviewQueueRepository reviewQueueRepository,
        IAiCompletionPort? aiCompletionPort = null)
    {
        _writeRepository = writeRepository;
        _reviewQueueRepository = reviewQueueRepository;
        _aiCompletionPort = aiCompletionPort;
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

        var result = await _aiCompletionPort.CompleteAsync(new AiCompletionRequest
        {
            FeatureKey = AiFeatureKeys.FitmentInference,
            PromptKey = AiFeatureKeys.FitmentInference,
            Prompt = $"""
                Given product "{productName}" (id {productId}) and vehicle configuration {vehicleConfigurationId},
                respond with one word: Fits, DoesNotFit, or Unknown.
                """,
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
        if (text.Contains("DoesNotFit", StringComparison.OrdinalIgnoreCase)
            || text.Contains("does not fit", StringComparison.OrdinalIgnoreCase))
        {
            return FitmentStatus.DoesNotFit;
        }

        if (text.Contains("Fits", StringComparison.OrdinalIgnoreCase))
            return FitmentStatus.Fits;

        return FitmentStatus.Unknown;
    }
}
