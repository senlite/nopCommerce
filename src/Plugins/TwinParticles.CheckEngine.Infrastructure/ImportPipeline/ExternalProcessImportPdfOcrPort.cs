using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Infrastructure.ImportPipeline;

/// <summary>
/// Shells out to an operator-provided OCR command. The command receives the PDF path as %1 and
/// must write extracted text to stdout. Example: tesseract %1 stdout -l eng
/// </summary>
public sealed class ExternalProcessImportPdfOcrPort : IImportPdfOcrPort
{
    private readonly string _commandTemplate;

    public ExternalProcessImportPdfOcrPort(string commandTemplate)
    {
        if (string.IsNullOrWhiteSpace(commandTemplate))
            throw new ArgumentException("OCR command template is required.", nameof(commandTemplate));

        _commandTemplate = commandTemplate.Trim();
    }

    public async Task<string?> TryExtractTextAsync(byte[] pdfContent, CancellationToken cancellationToken)
    {
        if (pdfContent is null || pdfContent.Length == 0)
            return null;

        var tempDir = Path.Combine(Path.GetTempPath(), "checkengine-ocr", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var pdfPath = Path.Combine(tempDir, "import.pdf");

        try
        {
            await File.WriteAllBytesAsync(pdfPath, pdfContent, cancellationToken);
            var commandLine = _commandTemplate.Replace("%1", pdfPath, StringComparison.Ordinal);
            var output = await RunProcessAsync(commandLine, cancellationToken);
            return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
        }
        catch
        {
            return null;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best-effort temp cleanup.
            }
        }
    }

    private static async Task<string?> RunProcessAsync(string commandLine, CancellationToken cancellationToken)
    {
        var splitIndex = commandLine.IndexOf(' ');
        var fileName = splitIndex > 0 ? commandLine[..splitIndex] : commandLine;
        var arguments = splitIndex > 0 ? commandLine[(splitIndex + 1)..] : string.Empty;

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        if (!process.Start())
            return null;

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0 ? stdout : null;
    }
}
