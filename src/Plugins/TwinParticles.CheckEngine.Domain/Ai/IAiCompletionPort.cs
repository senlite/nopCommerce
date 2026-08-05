using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Ai;

public interface IAiCompletionPort
{
    Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct);
}
