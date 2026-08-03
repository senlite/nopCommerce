using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Infrastructure.ImportPipeline;

public sealed class ExcelImportExtractionParser : IImportExtractionParser
{
    public bool CanParse(ImportSourceFormat format) => format == ImportSourceFormat.Excel;

    public Task<IReadOnlyList<ImportExtractedRow>> ParseAsync(ImportExtractionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<ImportExtractedRow>>([]);
    }
}
