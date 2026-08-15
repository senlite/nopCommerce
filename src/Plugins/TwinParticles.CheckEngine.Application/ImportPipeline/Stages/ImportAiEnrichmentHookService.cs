using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Application.L10n;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportAiEnrichmentHookService
{
    private readonly IAiCompletionPort? _aiCompletionPort;
    private readonly IAiFeatureToggle? _featureToggle;
    private readonly AiContentCandidateService? _contentCandidateService;

    public ImportAiEnrichmentHookService(
        IAiCompletionPort? aiCompletionPort = null,
        IAiFeatureToggle? featureToggle = null,
        AiContentCandidateService? contentCandidateService = null)
    {
        _aiCompletionPort = aiCompletionPort;
        _featureToggle = featureToggle;
        _contentCandidateService = contentCandidateService;
    }

    public void Apply(IReadOnlyList<ImportPipelineRowState> rows, bool enabled)
    {
        if (!enabled)
            return;

        if (_featureToggle is not null && !_featureToggle.IsEnabled(AiFeatureKeys.ImportEnrichment))
            return;

        if (_aiCompletionPort is null)
            return;

        foreach (var row in rows)
        {
            row.Fields.TryGetValue("name", out var name);
            row.Fields.TryGetValue("oem", out var oem);

            AiCompletionResult result;
            try
            {
                result = _aiCompletionPort.CompleteAsync(new AiCompletionRequest
                {
                    FeatureKey = AiFeatureKeys.ImportEnrichment,
                    PromptKey = AiFeatureKeys.ImportEnrichment,
                    Prompt = $"Enrich product description. Name={name}; Oem={oem}",
                    MaxTokens = 256
                }, default).GetAwaiter().GetResult();
            }
            catch
            {
                result = new AiCompletionResult
                {
                    Success = false,
                    ErrorCode = "ai.provider_degraded"
                };
            }

            if (result.Success)
            {
                row.Fields = new Dictionary<string, string?>(row.Fields)
                {
                    ["aiEnriched"] = "true",
                    ["aiDescriptionCandidate"] = result.Text
                };

                _contentCandidateService?.SaveCandidateAsync(
                    AiGenerationEntityType.ProductDescription,
                    row.RowNumber,
                    AiFeatureKeys.ImportEnrichment,
                    "en",
                    result.Text,
                    AiFeatureKeys.ImportEnrichment,
                    result.PromptHash,
                    null,
                    default).GetAwaiter().GetResult();
            }
            else
            {
                row.Fields = new Dictionary<string, string?>(row.Fields)
                {
                    ["aiEnrichmentStatus"] = "skipped"
                };
            }
        }
    }
}
