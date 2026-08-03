using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Images;

namespace TwinParticles.CheckEngine.Infrastructure.Images;

public sealed class InMemoryProductImageRepository : IProductImageRepository
{
    private readonly ConcurrentDictionary<int, ProductImageRecord> _items = new();

    public Task<ProductImageRecord?> GetPrimaryAsync(int productId, CancellationToken cancellationToken)
    {
        _items.TryGetValue(productId, out var record);
        return Task.FromResult(record);
    }

    public Task UpsertPrimaryAsync(ProductImageRecord record, CancellationToken cancellationToken)
    {
        _items[record.ProductId] = record;
        return Task.CompletedTask;
    }
}
