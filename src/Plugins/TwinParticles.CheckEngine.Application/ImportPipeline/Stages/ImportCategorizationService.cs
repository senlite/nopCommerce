using System.Collections.Generic;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportCategorizationService
{
    public void Apply(IReadOnlyList<ImportPipelineRowState> rows)
    {
        foreach (var row in rows)
        {
            if (row.Fields.TryGetValue("category", out var category)
                && !string.IsNullOrWhiteSpace(category))
            {
                row.Category = category;
            }
            else
            {
                row.Category = "Uncategorized";
            }
        }
    }
}
