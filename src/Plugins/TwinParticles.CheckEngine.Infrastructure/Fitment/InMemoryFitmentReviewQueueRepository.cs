using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Infrastructure.Fitment;

public sealed class InMemoryFitmentReviewQueueRepository : IFitmentReviewQueueRepository
{
    private readonly ConcurrentQueue<(int ClaimId, string ReasonCode)> _events = new();

    public Task EnqueueAsync(int claimId, string reasonCode, CancellationToken cancellationToken)
    {
        _events.Enqueue((claimId, reasonCode));
        return Task.CompletedTask;
    }

    public Task DequeueAsync(int claimId, string reasonCode, CancellationToken cancellationToken)
    {
        _events.Enqueue((claimId, reasonCode));
        return Task.CompletedTask;
    }
}
