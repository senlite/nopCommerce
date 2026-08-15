using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Application.L10n;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportTranslationHookService
{
    private readonly IAiCompletionPort? _aiCompletionPort;
    private readonly IAiFeatureToggle? _featureToggle;
    private readonly AutomotiveGlossaryService _glossaryService;
    private readonly AiContentCandidateService? _contentCandidateService;

    public ImportTranslationHookService(
        AutomotiveGlossaryService glossaryService,
        IAiCompletionPort? aiCompletionPort = null,
        IAiFeatureToggle? featureToggle = null,
        AiContentCandidateService? contentCandidateService = null)
    {
        _glossaryService = glossaryService;
        _aiCompletionPort = aiCompletionPort;
        _featureToggle = featureToggle;
        _contentCandidateService = contentCandidateService;
    }

    public void Apply(IReadOnlyList<ImportPipelineRowState> rows, bool enabled)
    {
        if (!enabled)
            return;

        if (_featureToggle is not null && !_featureToggle.IsEnabled(AiFeatureKeys.ImportTranslation))
            return;

        if (_aiCompletionPort is null)
        {
            foreach (var row in rows)
            {
                if (!row.Fields.ContainsKey("translated"))
                    row.Fields = new Dictionary<string, string?>(row.Fields)
                    {
                        ["translated"] = "true"
                    };
            }

            return;
        }

        foreach (var row in rows)
        {
            row.Fields.TryGetValue("name", out var name);
            var prompt = _glossaryService.BuildTranslationPromptAsync(name ?? string.Empty, "ar", default)
                .GetAwaiter().GetResult();

            var result = _aiCompletionPort.CompleteAsync(new AiCompletionRequest
            {
                FeatureKey = AiFeatureKeys.ImportTranslation,
                PromptKey = AiFeatureKeys.ImportTranslation,
                Prompt = prompt
            }, default).GetAwaiter().GetResult();

            if (!result.Success)
                continue;

            var glossaryValid = _glossaryService.ValidateTranslation(name ?? string.Empty, result.Text);
            row.Fields = new Dictionary<string, string?>(row.Fields)
            {
                ["translated"] = glossaryValid ? "true" : "pending_review",
                ["translationCandidate"] = result.Text,
                ["translationGlossaryValid"] = glossaryValid ? "true" : "false"
            };

            _contentCandidateService?.SaveCandidateAsync(
                AiGenerationEntityType.Translation,
                row.RowNumber,
                AiFeatureKeys.ImportTranslation,
                "ar",
                result.Text,
                AiFeatureKeys.ImportTranslation,
                result.PromptHash,
                glossaryValid ? 1m : 0.5m,
                default).GetAwaiter().GetResult();
        }
    }
}
