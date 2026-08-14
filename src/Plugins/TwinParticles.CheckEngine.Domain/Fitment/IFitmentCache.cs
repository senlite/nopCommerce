using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Fitment;

/// <summary>
/// Caches fitment verdicts. The key must include the full evaluation context: qualifiers mean the
/// same product and vehicle can resolve differently for a left- vs right-hand-drive car, an early vs
/// late build year, and so on. Keying on product and vehicle alone would serve the first computed
/// verdict to every context and could report a fit for a vehicle the part does not fit.
/// </summary>
public interface IFitmentCache
{
    Task<FitmentEvaluationResult?> GetAsync(FitmentEvaluationContext context, CancellationToken cancellationToken);

    Task SetAsync(FitmentEvaluationContext context, FitmentEvaluationResult result, CancellationToken cancellationToken);

    /// <summary>Removes every cached verdict for a product and vehicle across all contexts.</summary>
    Task InvalidateAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken);
}
