using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.ImportPipeline;

public interface IImportPipelineRepository
{
    Task<ImportBatch?> GetBatchAsync(int batchId, CancellationToken cancellationToken);

    Task<ImportBatch?> GetBatchByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken);

    Task UpsertBatchAsync(ImportBatch batch, CancellationToken cancellationToken);

    Task<IReadOnlyList<ImportRow>> GetRowsAsync(int batchId, CancellationToken cancellationToken);

    Task ReplaceRowsAsync(int batchId, IReadOnlyList<ImportRow> rows, CancellationToken cancellationToken);
}
