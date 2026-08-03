using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Fitment;

public interface IFitmentReviewQueueRepository
{
    Task EnqueueAsync(int claimId, string reasonCode, CancellationToken cancellationToken);
}
