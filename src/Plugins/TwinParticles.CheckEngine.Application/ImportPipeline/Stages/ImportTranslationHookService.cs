using System.Collections.Generic;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportTranslationHookService
{
    private readonly IAiCompletionPort? _aiCompletionPort;

    public ImportTranslationHookService(IAiCompletionPort? aiCompletionPort = null)
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
            var result = _aiCompletionPort.CompleteAsync(new AiCompletionRequest
            {
                PromptKey = "import.translation",
                Prompt = $"Translate product text: {name}"
            }, default).GetAwaiter().GetResult();

            if (!result.Success)
                continue;

            if (!row.Fields.ContainsKey("translated"))
                row.Fields = new Dictionary<string, string?>(row.Fields)
                {
                    ["translated"] = "true"
                };
        }
    }
}
