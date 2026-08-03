using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Fitment;

public interface IFitmentCache
{
    Task<FitmentEvaluationResult?> GetAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken);

    Task SetAsync(int productId, int vehicleConfigurationId, FitmentEvaluationResult result, CancellationToken cancellationToken);

    Task InvalidateAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken);
}
