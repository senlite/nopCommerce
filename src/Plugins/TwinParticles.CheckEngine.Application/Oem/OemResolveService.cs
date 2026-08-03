using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Oem;

namespace TwinParticles.CheckEngine.Application.Oem;

public sealed class OemResolveService
{
    private const int DefaultSupersessionDepth = 10;

    private readonly IOemNormalizationService _normalizationService;
    private readonly IOemSearchReadRepository _searchRepository;
    private readonly OemSupersessionService _supersessionService;

    public OemResolveService(IOemNormalizationService normalizationService, IOemSearchReadRepository searchRepository, OemSupersessionService supersessionService)
    {
        _normalizationService = normalizationService;
        _searchRepository = searchRepository;
        _supersessionService = supersessionService;
    }

    public async Task<OemResolveResult> ResolveAsync(OemResolveQuery query, CancellationToken cancellationToken)
    {
        var normalized = _normalizationService.Normalize(query.Number);
        if (string.IsNullOrWhiteSpace(normalized))
            return OemResolveResult.Fail("oem.not_found");

        var matches = await _searchRepository.FindByNormalizedNumberAsync(normalized, query.ManufacturerId, cancellationToken);
        if (matches.Count == 0)
            return OemResolveResult.Fail("oem.not_found");

        if (query.ManufacturerId is null)
        {
            var manufacturerIds = matches.Select(x => x.ManufacturerId).Distinct().ToList();
            if (manufacturerIds.Count > 1)
                return OemResolveResult.Fail("oem.ambiguous_manufacturer", manufacturerIds);
        }

        var resolved = matches[0];

        int? currentOemNumberId = null;
        var chain = await _supersessionService.GetSupersessionChainAsync(resolved.Id, DefaultSupersessionDepth, cancellationToken);
        if (chain.Success && chain.ChainOemNumberIds.Count > 1)
            currentOemNumberId = chain.ChainOemNumberIds[^1];

        var result = OemResolveResult.Ok();
        result.OemNumberId = resolved.Id;
        result.ManufacturerId = resolved.ManufacturerId;
        result.DisplayNumber = resolved.DisplayNumber;
        result.NormalizedNumber = resolved.NormalizedNumber;
        result.IsObsolete = resolved.IsObsolete;
        result.CurrentOemNumberId = currentOemNumberId;
        result.ProductIds = [];

        return result;
    }
}
