using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.ImportPipeline;

public interface IImportExtractionParser
{
    bool CanParse(ImportSourceFormat format);

    Task<IReadOnlyList<ImportExtractedRow>> ParseAsync(ImportExtractionRequest request, CancellationToken cancellationToken);
}
