using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Infrastructure.ImportPipeline;

/// <summary>
/// Test/dev fallback that does not touch the host catalog.
/// </summary>
public sealed class NullImportProductPublisher : IImportProductPublisher
{
    public Task<ImportProductPublishResult> PublishAsync(
        IReadOnlyDictionary<string, string?> fields,
        int? matchedOemNumberId,
        CancellationToken cancellationToken)
    {
        if (fields.TryGetValue("productId", out var raw) && int.TryParse(raw, out var productId) && productId > 0)
            return Task.FromResult(ImportProductPublishResult.Ok(productId));

        // Synthetic success for approved rows without host catalog access (unit tests / dry environments).
        return Task.FromResult(ImportProductPublishResult.Ok(0));
    }
}
