using System.Collections.Generic;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportTranslationHookService
{
    public void Apply(IReadOnlyList<ImportPipelineRowState> rows, bool enabled)
    {
        if (!enabled)
            return;

        foreach (var row in rows)
        {
            if (!row.Fields.ContainsKey("translated"))
                row.Fields = new Dictionary<string, string?>(row.Fields)
                {
                    ["translated"] = "true"
                };
        }
    }
}
