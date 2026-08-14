using System.Collections.Generic;
using System.Linq;

namespace TwinParticles.CheckEngine.Application.Images;

/// <summary>Outcome of a bulk image replacement queued by SKU list (FR-631, doc 26 "Bulk").</summary>
public sealed class BatchImageReplacementResult
{
    public IReadOnlyList<BatchImageReplacementItemResult> Items { get; init; } = [];

    public int Replaced => Items.Count(item => item.Status == BatchImageReplacementStatus.Replaced);

    public int NotFound => Items.Count(item => item.Status == BatchImageReplacementStatus.SkuNotFound);

    public int Quarantined => Items.Count(item => item.Status == BatchImageReplacementStatus.Quarantined);

    public int Failed => Items.Count(item => item.Status == BatchImageReplacementStatus.Failed);
}

public sealed class BatchImageReplacementItemResult
{
    public required string Sku { get; init; }

    public int? ProductId { get; init; }

    public BatchImageReplacementStatus Status { get; init; }

    public string? ErrorCode { get; init; }
}

public enum BatchImageReplacementStatus
{
    Replaced = 1,
    SkuNotFound = 2,
    Quarantined = 3,
    Failed = 4
}
