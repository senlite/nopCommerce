using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Extraction;

public sealed class ImportExtractionService
{
    private readonly IReadOnlyList<IImportExtractionParser> _parsers;

    public ImportExtractionService(IEnumerable<IImportExtractionParser> parsers)
    {
        _parsers = parsers.ToList();
    }

    public async Task<ImportExtractionResult> ExtractAsync(ImportExtractionRequest request, CancellationToken cancellationToken)
    {
        var parser = _parsers.FirstOrDefault(x => x.CanParse(request.Format));
        if (parser is null)
            return ImportExtractionResult.Fail("import.unsupported_format");

        var rows = await parser.ParseAsync(request, cancellationToken);
        return ImportExtractionResult.Ok(rows);
    }
}
