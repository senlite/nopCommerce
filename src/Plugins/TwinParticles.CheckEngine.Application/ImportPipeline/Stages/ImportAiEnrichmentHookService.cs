using System.Collections.Generic;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportAiEnrichmentHookService
{
    private const string FeatureKey = "import.ai.enrichment";

    private readonly IAiCompletionPort? _aiCompletionPort;
    private readonly IAiFeatureToggle? _featureToggle;

    public ImportAiEnrichmentHookService(
        IAiCompletionPort? aiCompletionPort = null,
        IAiFeatureToggle? featureToggle = null)
    {
        _aiCompletionPort = aiCompletionPort;
        _featureToggle = featureToggle;
    }

    public void Apply(IReadOnlyList<ImportPipelineRowState> rows, bool enabled)
    {
        if (!enabled)
            return;

        if (_featureToggle is not null && !_featureToggle.IsEnabled(FeatureKey))
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
                    PromptKey = FeatureKey,
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
                if (!row.Fields.ContainsKey("aiEnriched"))
                {
                    row.Fields = new Dictionary<string, string?>(row.Fields)
                    {
                        ["aiEnriched"] = "true"
                    };
                }
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
