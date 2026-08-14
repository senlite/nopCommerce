using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Infrastructure.ImportPipeline;

public sealed class NullImportPdfOcrPort : IImportPdfOcrPort
{
    public static NullImportPdfOcrPort Instance { get; } = new();

    public Task<string?> TryExtractTextAsync(byte[] pdfContent, CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);
}
