using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.ImportPipeline;

/// <summary>
/// Publishes an approved import row into the host catalog.
/// Implementations must be transactional per row and never invent fitment.
/// </summary>
public interface IImportProductPublisher
{
    Task<ImportProductPublishResult> PublishAsync(
        IReadOnlyDictionary<string, string?> fields,
        int? matchedOemNumberId,
        CancellationToken cancellationToken);
}

public sealed class ImportProductPublishResult
{
    public bool Success { get; init; }

    public int? ProductId { get; init; }

    public string? ErrorCode { get; init; }

    public static ImportProductPublishResult Ok(int productId) => new()
    {
        Success = true,
        ProductId = productId
    };

    public static ImportProductPublishResult Fail(string errorCode) => new()
    {
        Success = false,
        ErrorCode = errorCode
    };
}
