using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportOemMatchingService
{
    private readonly OemResolveService _oemResolveService;

    public ImportOemMatchingService(OemResolveService oemResolveService)
    {
        _oemResolveService = oemResolveService;
    }

    public async Task<IReadOnlyDictionary<int, OemResolveResult>> MatchAsync(IReadOnlyList<ImportNormalizedRow> rows, CancellationToken cancellationToken)
    {
        var result = new Dictionary<int, OemResolveResult>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.OemNumberRaw))
            {
                result[row.RowNumber] = OemResolveResult.Fail("import.oem_missing");
                continue;
            }

            result[row.RowNumber] = await _oemResolveService.ResolveAsync(new OemResolveQuery
            {
                Number = row.OemNumberRaw
            }, cancellationToken);
        }

        return result;
    }
}
