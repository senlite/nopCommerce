using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.ImportPipeline;

/// <summary>
/// Optional OCR fallback for image-only supplier PDFs (H1.20). Disabled unless the operator
/// configures an external OCR command (for example Tesseract).
/// </summary>
public interface IImportPdfOcrPort
{
    Task<string?> TryExtractTextAsync(byte[] pdfContent, CancellationToken cancellationToken);
}
