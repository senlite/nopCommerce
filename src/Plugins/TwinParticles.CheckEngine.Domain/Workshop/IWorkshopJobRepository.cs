using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Workshop;

public interface IWorkshopJobRepository
{
    Task<WorkshopAccount?> GetAccountByCustomerIdAsync(int customerId, CancellationToken cancellationToken);

    Task<WorkshopAccount?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken);

    Task<int> InsertJobAsync(WorkshopJob job, CancellationToken cancellationToken);

    Task UpdateJobAsync(WorkshopJob job, CancellationToken cancellationToken);

    Task<WorkshopJob?> GetJobAsync(int jobId, CancellationToken cancellationToken);

    Task<int> InsertJobVehicleAsync(WorkshopJobVehicle vehicle, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkshopJobVehicle>> GetJobVehiclesAsync(int jobId, CancellationToken cancellationToken);

    Task<WorkshopJobVehicle?> GetJobVehicleAsync(int jobVehicleId, CancellationToken cancellationToken);

    Task<int> InsertJobLineAsync(WorkshopJobLine line, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkshopJobLine>> GetJobLinesAsync(int jobId, CancellationToken cancellationToken);

    Task<decimal> ResolveTradePriceAsync(int priceListId, int productId, CancellationToken cancellationToken);
}
