using System.Collections.Generic;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportSeoGenerationHookService
{
    private readonly IAiCompletionPort? _aiCompletionPort;
    private readonly IAiFeatureToggle? _featureToggle;
    private readonly AiContentCandidateService? _contentCandidateService;
    private readonly AiPromptResolver? _promptResolver;

    public ImportSeoGenerationHookService(
        IAiCompletionPort? aiCompletionPort = null,
        IAiFeatureToggle? featureToggle = null,
        AiContentCandidateService? contentCandidateService = null,
        AiPromptResolver? promptResolver = null)
    {
        _aiCompletionPort = aiCompletionPort;
        _featureToggle = featureToggle;
        _contentCandidateService = contentCandidateService;
        _promptResolver = promptResolver;
    }

    public void Apply(IReadOnlyList<ImportPipelineRowState> rows, bool enabled)
    {
        if (!enabled)
            return;

        if (_featureToggle is not null && !_featureToggle.IsEnabled(AiFeatureKeys.ImportSeo))
            return;

        if (_aiCompletionPort is null)
        {
            foreach (var row in rows)
            {
                if (!row.Fields.ContainsKey("seoGenerated"))
                    row.Fields = new Dictionary<string, string?>(row.Fields)
                    {
                        ["seoGenerated"] = "true"
                    };
            }

            return;
        }

        foreach (var row in rows)
        {
            row.Fields.TryGetValue("name", out var name);
            var prompt = _promptResolver?.Format(AiFeatureKeys.ImportSeo, new Dictionary<string, string?>
            {
                ["name"] = name
            }) ?? $"Generate SEO title and meta description for: {name}";

            var result = _aiCompletionPort.CompleteAsync(new AiCompletionRequest
            {
                FeatureKey = AiFeatureKeys.ImportSeo,
                PromptKey = AiFeatureKeys.ImportSeo,
                Prompt = prompt
            }, default).GetAwaiter().GetResult();

            if (!result.Success)
                continue;

            row.Fields = new Dictionary<string, string?>(row.Fields)
            {
                ["seoGenerated"] = "true",
                ["seoCandidate"] = result.Text
            };

            _contentCandidateService?.SaveCandidateAsync(
                AiGenerationEntityType.SeoMetadata,
                row.RowNumber,
                AiFeatureKeys.ImportSeo,
                "en",
                result.Text,
                AiFeatureKeys.ImportSeo,
                result.PromptHash,
                null,
                default).GetAwaiter().GetResult();
        }
    }
}
