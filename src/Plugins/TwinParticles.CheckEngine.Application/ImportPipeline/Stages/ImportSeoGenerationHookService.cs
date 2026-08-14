using System.Collections.Generic;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportSeoGenerationHookService
{
    private readonly IAiCompletionPort? _aiCompletionPort;

    public ImportSeoGenerationHookService(IAiCompletionPort? aiCompletionPort = null)
    {
        _aiCompletionPort = aiCompletionPort;
    }

    public void Apply(IReadOnlyList<ImportPipelineRowState> rows, bool enabled)
    {
        if (!enabled)
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
            var result = _aiCompletionPort.CompleteAsync(new AiCompletionRequest
            {
                PromptKey = "import.seo",
                Prompt = $"Generate SEO metadata for: {name}"
            }, default).GetAwaiter().GetResult();

            if (!result.Success)
                continue;

            if (!row.Fields.ContainsKey("seoGenerated"))
                row.Fields = new Dictionary<string, string?>(row.Fields)
                {
                    ["seoGenerated"] = "true"
                };
        }
    }
}
