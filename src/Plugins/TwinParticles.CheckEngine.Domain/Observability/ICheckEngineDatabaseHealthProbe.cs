using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Observability;

/// <summary>
/// Thin DB health port so Application stays free of Nop.Data.
/// Infrastructure implements this with INopDataProvider.
/// </summary>
public interface ICheckEngineDatabaseHealthProbe
{
    Task<bool> CanQueryAsync(CancellationToken cancellationToken);
}
