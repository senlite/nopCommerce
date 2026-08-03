using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Infrastructure.ImportPipeline;

public sealed class CsvImportExtractionParser : IImportExtractionParser
{
    public bool CanParse(ImportSourceFormat format) => format == ImportSourceFormat.Csv;

    public Task<IReadOnlyList<ImportExtractedRow>> ParseAsync(ImportExtractionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var encoding = DetectEncoding(request.Content);
        var text = encoding.GetString(request.Content);

        using var reader = new StringReader(text);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
            return Task.FromResult<IReadOnlyList<ImportExtractedRow>>([]);

        headerLine = headerLine.TrimStart('\uFEFF');
        var headers = SplitCsvLine(headerLine);
        var rows = new List<ImportExtractedRow>();

        string? line;
        var rowNumber = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = SplitCsvLine(line);
            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                var key = headers[i];
                var value = i < values.Count ? values[i] : null;
                fields[key] = value;
            }

            rows.Add(new ImportExtractedRow
            {
                RowNumber = rowNumber,
                Fields = fields
            });

            rowNumber++;
        }

        return Task.FromResult<IReadOnlyList<ImportExtractedRow>>(rows);
    }

    private static Encoding DetectEncoding(byte[] content)
    {
        if (content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }

    private static List<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString().Trim());
        return values;
    }
}
