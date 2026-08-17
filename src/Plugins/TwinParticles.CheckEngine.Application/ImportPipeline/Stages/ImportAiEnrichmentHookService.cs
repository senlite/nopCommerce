using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportAiEnrichmentHookService
{
    private readonly IAiCompletionPort? _aiCompletionPort;
    private readonly IAiFeatureToggle? _featureToggle;
    private readonly AiContentCandidateService? _contentCandidateService;
    private readonly AiPromptResolver? _promptResolver;
    private readonly IAllowedSpecificationKeyCatalog? _specificationKeyCatalog;

    public ImportAiEnrichmentHookService(
        IAiCompletionPort? aiCompletionPort = null,
        IAiFeatureToggle? featureToggle = null,
        AiContentCandidateService? contentCandidateService = null,
        AiPromptResolver? promptResolver = null,
        IAllowedSpecificationKeyCatalog? specificationKeyCatalog = null)
    {
        _aiCompletionPort = aiCompletionPort;
        _featureToggle = featureToggle;
        _contentCandidateService = contentCandidateService;
        _promptResolver = promptResolver;
        _specificationKeyCatalog = specificationKeyCatalog;
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

            var descriptionPrompt = _promptResolver?.Format(AiFeatureKeys.ImportEnrichment, new Dictionary<string, string?>
            {
                ["name"] = name,
                ["oem"] = oem
            }) ?? $"Enrich product description. Name={name}; Oem={oem}";

            var descriptionResult = Complete(descriptionPrompt, AiFeatureKeys.ImportEnrichment, AiFeatureKeys.ImportEnrichment);
            if (descriptionResult.Success)
            {
                row.Fields = new Dictionary<string, string?>(row.Fields)
                {
                    ["aiEnriched"] = "true",
                    ["aiDescriptionCandidate"] = descriptionResult.Text
                };

                _contentCandidateService?.SaveCandidateAsync(
                    AiGenerationEntityType.ProductDescription,
                    row.RowNumber,
                    AiFeatureKeys.ImportEnrichment,
                    "en",
                    descriptionResult.Text,
                    AiFeatureKeys.ImportEnrichment,
                    descriptionResult.PromptHash,
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

            var specificationPrompt = _promptResolver?.Format(AiFeatureKeys.ImportSpecification, new Dictionary<string, string?>
            {
                ["name"] = name,
                ["oem"] = oem
            }) ?? $"Extract specifications. Name={name}; Oem={oem}";

            var specificationResult = Complete(specificationPrompt, AiFeatureKeys.ImportEnrichment, AiFeatureKeys.ImportSpecification);
            if (specificationResult.Success)
            {
                var catalog = _specificationKeyCatalog ?? new AllowedSpecificationKeyCatalog();
                var validation = AiSpecificationCandidateFormatter.Validate(specificationResult.Text, catalog);
                if (!string.IsNullOrWhiteSpace(validation.FormattedText))
                {
                    var fields = new Dictionary<string, string?>(row.Fields)
                    {
                        ["aiSpecificationCandidate"] = validation.FormattedText
                    };

                    if (validation.UnknownKeys.Count > 0)
                        fields["aiSpecificationUnknownKeys"] = string.Join(", ", validation.UnknownKeys);

                    row.Fields = fields;

                    _contentCandidateService?.SaveCandidateAsync(
                        AiGenerationEntityType.Specification,
                        row.RowNumber,
                        AiFeatureKeys.ImportSpecification,
                        "en",
                        validation.FormattedText,
                        AiFeatureKeys.ImportSpecification,
                        specificationResult.PromptHash,
                        validation.QualityScore,
                        default).GetAwaiter().GetResult();
                }
            }
        }
    }

    private AiCompletionResult Complete(string prompt, string featureKey, string promptKey)
    {
        try
        {
            return _aiCompletionPort!.CompleteAsync(new AiCompletionRequest
            {
                FeatureKey = featureKey,
                PromptKey = promptKey,
                Prompt = prompt,
                MaxTokens = 256
            }, default).GetAwaiter().GetResult();
        }
        catch
        {
            return new AiCompletionResult
            {
                Success = false,
                ErrorCode = "ai.provider_degraded"
            };
        }
    }
}
