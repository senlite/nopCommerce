using System.Collections.Generic;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportImageAssignmentService
{
    public void Apply(IReadOnlyList<ImportPipelineRowState> rows)
    {
        foreach (var row in rows)
        {
            if (row.Fields.TryGetValue("image", out var image)
                && !string.IsNullOrWhiteSpace(image))
            {
                row.ImageUrl = image;
            }
            else
            {
                row.ImageUrl = null;
            }
        }
    }
}
