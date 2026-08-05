using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Oem;

public interface IProductOemMapRepository
{
    Task UpsertAsync(ProductOemMap map, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductOemMap>> GetByProductIdAsync(int productId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductOemMap>> GetByOemNumberIdAsync(int oemNumberId, CancellationToken cancellationToken);
}
