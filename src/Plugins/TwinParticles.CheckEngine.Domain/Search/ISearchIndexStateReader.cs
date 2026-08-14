using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Search;

/// <summary>Exposes the durable search projection state so the read path can decide whether to
/// serve from the projection or degrade to the live catalog.</summary>
public interface ISearchIndexStateReader
{
    Task<SearchIndexState> GetStateAsync(CancellationToken cancellationToken);
}
