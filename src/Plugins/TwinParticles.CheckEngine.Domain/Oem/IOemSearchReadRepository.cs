using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Oem;

public interface IOemSearchReadRepository
{
    Task<IReadOnlyList<OemNumber>> FindByNormalizedNumberAsync(string normalizedNumber, int? manufacturerId, CancellationToken cancellationToken);

    // Prefix lookup for OEM typeahead (FR-415). Default member keeps existing test doubles valid;
    // the SQL repository overrides it with an indexed LIKE against normalized numbers.
    Task<IReadOnlyList<OemNumber>> FindByNormalizedPrefixAsync(string normalizedPrefix, int take, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<OemNumber>>([]);
}
