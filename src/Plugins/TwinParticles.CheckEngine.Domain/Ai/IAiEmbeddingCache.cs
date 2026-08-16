using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Ai;

public interface IAiEmbeddingCache
{
    Task<AiEmbeddingResult?> TryGetAsync(string cacheKey, CancellationToken cancellationToken);

    Task SetAsync(string cacheKey, AiEmbeddingResult result, CancellationToken cancellationToken);
}
