using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Images;

public interface IProductImageRepository
{
    Task<ProductImageRecord?> GetPrimaryAsync(int productId, CancellationToken cancellationToken);

    Task UpsertPrimaryAsync(ProductImageRecord record, CancellationToken cancellationToken);
}
