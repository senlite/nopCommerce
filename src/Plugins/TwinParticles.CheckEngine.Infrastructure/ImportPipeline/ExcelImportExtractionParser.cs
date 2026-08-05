using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Infrastructure.ImportPipeline;

/// <summary>
/// Minimal OpenXML (.xlsx) first-sheet reader — no third-party spreadsheet dependency.
/// </summary>
public sealed class ExcelImportExtractionParser : IImportExtractionParser
{
    public bool CanParse(ImportSourceFormat format) => format == ImportSourceFormat.Excel;

    public Task<IReadOnlyList<ImportExtractedRow>> ParseAsync(ImportExtractionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Content is null || request.Content.Length == 0)
            return Task.FromResult<IReadOnlyList<ImportExtractedRow>>([]);

        // Allow CSV bytes mistakenly labeled as Excel.
        if (LooksLikeCsv(request.Content))
            return new CsvImportExtractionParser().ParseAsync(request, cancellationToken);

        try
        {
            using var stream = new MemoryStream(request.Content, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

            var sharedStrings = ReadSharedStrings(archive);
            var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
                ?? archive.Entries.FirstOrDefault(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase) && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

            if (sheetEntry is null)
                return Task.FromResult<IReadOnlyList<ImportExtractedRow>>([]);

            using var sheetStream = sheetEntry.Open();
            var sheetDoc = XDocument.Load(sheetStream);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            var rowsXml = sheetDoc.Root?.Element(ns + "sheetData")?.Elements(ns + "row").ToList() ?? [];
            if (rowsXml.Count == 0)
                return Task.FromResult<IReadOnlyList<ImportExtractedRow>>([]);

            var headerCells = rowsXml[0].Elements(ns + "c").Select(c => GetCellValue(c, ns, sharedStrings)).ToList();
            if (headerCells.Count == 0 || headerCells.All(string.IsNullOrWhiteSpace))
                return Task.FromResult<IReadOnlyList<ImportExtractedRow>>([]);

            var extracted = new List<ImportExtractedRow>();
            var rowNumber = 1;
            for (var i = 1; i < rowsXml.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var values = rowsXml[i].Elements(ns + "c").Select(c => GetCellValue(c, ns, sharedStrings)).ToList();
                if (values.All(string.IsNullOrWhiteSpace))
                    continue;

                var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (var col = 0; col < headerCells.Count; col++)
                {
                    var key = headerCells[col];
                    if (string.IsNullOrWhiteSpace(key))
                        continue;
                    fields[key!] = col < values.Count ? values[col] : null;
                }

                extracted.Add(new ImportExtractedRow { RowNumber = rowNumber, Fields = fields });
                rowNumber++;
            }

            return Task.FromResult<IReadOnlyList<ImportExtractedRow>>(extracted);
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<ImportExtractedRow>>([]);
        }
    }

    private static bool LooksLikeCsv(byte[] content)
    {
        var sampleLength = Math.Min(content.Length, 512);
        var sample = Encoding.UTF8.GetString(content, 0, sampleLength);
        return sample.Contains(',') && sample.Contains('\n') && !sample.Contains("PK");
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
            return [];

        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return doc.Root?.Elements(ns + "si")
            .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => (string?)t ?? string.Empty)))
            .ToList() ?? [];
    }

    private static string? GetCellValue(XElement cell, XNamespace ns, IReadOnlyList<string> sharedStrings)
    {
        var type = (string?)cell.Attribute("t");
        var value = (string?)cell.Element(ns + "v");
        if (value is null)
            return (string?)cell.Element(ns + "is")?.Element(ns + "t");

        if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
            && index >= 0
            && index < sharedStrings.Count)
        {
            return sharedStrings[index];
        }

        return value;
    }
}
