using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Ai;

public interface IAiCompletionCache
{
    Task<AiCompletionResult?> TryGetAsync(string cacheKey, CancellationToken cancellationToken);

    Task SetAsync(string cacheKey, AiCompletionResult result, CancellationToken cancellationToken);
}
