using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Infrastructure.ImportPipeline;

/// <summary>
/// Best-effort PDF text extractor for simple text-based supplier PDFs.
/// Binary/image-only PDFs return an empty row set (caller surfaces extraction failure).
/// </summary>
public sealed class PdfImportExtractionParser : IImportExtractionParser
{
    public bool CanParse(ImportSourceFormat format) => format == ImportSourceFormat.Pdf;

    public Task<IReadOnlyList<ImportExtractedRow>> ParseAsync(ImportExtractionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Content is null || request.Content.Length == 0)
            return Task.FromResult<IReadOnlyList<ImportExtractedRow>>([]);

        var text = ExtractLatinText(request.Content);
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult<IReadOnlyList<ImportExtractedRow>>([]);

        // Prefer delimiter tables (comma/tab/pipe).
        var lines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.Length > 2)
            .ToList();

        if (lines.Count < 2)
            return Task.FromResult<IReadOnlyList<ImportExtractedRow>>([]);

        var delimiter = DetectDelimiter(lines[0]);
        if (delimiter is null)
            return Task.FromResult<IReadOnlyList<ImportExtractedRow>>([]);

        var headers = Split(lines[0], delimiter.Value);
        if (headers.Count == 0)
            return Task.FromResult<IReadOnlyList<ImportExtractedRow>>([]);

        var rows = new List<ImportExtractedRow>();
        var rowNumber = 1;
        for (var i = 1; i < lines.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = Split(lines[i], delimiter.Value);
            if (values.All(string.IsNullOrWhiteSpace))
                continue;

            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var col = 0; col < headers.Count; col++)
                fields[headers[col]] = col < values.Count ? values[col] : null;

            rows.Add(new ImportExtractedRow { RowNumber = rowNumber++, Fields = fields });
        }

        return Task.FromResult<IReadOnlyList<ImportExtractedRow>>(rows);
    }

    private static char? DetectDelimiter(string headerLine)
    {
        if (headerLine.Contains('\t')) return '\t';
        if (headerLine.Contains('|')) return '|';
        if (headerLine.Contains(',')) return ',';
        if (headerLine.Contains(';')) return ';';
        return null;
    }

    private static List<string> Split(string line, char delimiter)
        => line.Split(delimiter).Select(x => x.Trim().Trim('"')).ToList();

    private static string ExtractLatinText(byte[] content)
    {
        // Pull printable ASCII runs from PDF content streams (sufficient for simple text PDFs).
        var raw = Encoding.Latin1.GetString(content);
        var matches = Regex.Matches(raw, @"\((?:\\.|[^\\)])*\)");
        if (matches.Count == 0)
        {
            // Fallback: contiguous printable runs.
            var sb = new StringBuilder();
            foreach (var ch in raw)
            {
                if (ch is >= ' ' and <= '~' or '\n' or '\r' or '\t')
                    sb.Append(ch);
                else
                    sb.Append(' ');
            }

            return CollapseWhitespace(sb.ToString());
        }

        var texts = new StringBuilder();
        foreach (Match match in matches)
        {
            var token = match.Value.Trim('(', ')');
            token = token.Replace("\\n", "\n", StringComparison.Ordinal)
                .Replace("\\r", "\r", StringComparison.Ordinal)
                .Replace("\\t", "\t", StringComparison.Ordinal)
                .Replace("\\(", "(", StringComparison.Ordinal)
                .Replace("\\)", ")", StringComparison.Ordinal);
            texts.Append(token);
            texts.Append(' ');
        }

        return CollapseWhitespace(texts.ToString());
    }

    private static string CollapseWhitespace(string value)
    {
        using var reader = new StringReader(value);
        var output = new StringBuilder();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = Regex.Replace(line, @"[ \t]{2,}", " ").Trim();
            if (trimmed.Length == 0)
                continue;
            output.AppendLine(trimmed);
        }

        return output.ToString();
    }
}
