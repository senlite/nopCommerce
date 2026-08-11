using System.Globalization;
using System.Text;

namespace Nop.Plugin.Misc.GMaster.Services;

public sealed class GMasterCatalogParser
{
    public IReadOnlyList<GMasterCatalogItem> Parse(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            throw new InvalidOperationException("The GMaster catalog file is empty.");

        var lines = ReadCsvRows(csv);
        if (lines.Count < 2)
            throw new InvalidOperationException("The GMaster catalog contains no product rows.");

        var header = lines[0];
        var expected = new[]
        {
            "sku", "name_ar", "name_en", "oem", "vehicle_models",
            "category_key", "cost_price", "selling_price", "source_file"
        };

        if (!header.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("The GMaster catalog header does not match the expected schema.");

        var items = new List<GMasterCatalogItem>(lines.Count - 1);
        var skus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var rowIndex = 1; rowIndex < lines.Count; rowIndex++)
        {
            var row = lines[rowIndex];
            if (row.Count != expected.Length)
                throw new InvalidOperationException($"Catalog row {rowIndex + 1} has {row.Count} columns; expected {expected.Length}.");

            var sku = row[0].Trim();
            var ArabicName = row[1].Trim();
            var englishName = row[2].Trim();
            var categoryKey = row[5].Trim();

            if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(ArabicName) ||
                string.IsNullOrWhiteSpace(englishName) || string.IsNullOrWhiteSpace(categoryKey))
            {
                throw new InvalidOperationException($"Catalog row {rowIndex + 1} is missing a required value.");
            }

            EnsurePlainText(row, rowIndex + 1);

            if (!skus.Add(sku))
                throw new InvalidOperationException($"Duplicate SKU '{sku}' in catalog row {rowIndex + 1}.");

            if (!decimal.TryParse(row[6], NumberStyles.Number, CultureInfo.InvariantCulture, out var cost) || cost <= 0)
                throw new InvalidOperationException($"Invalid cost price on catalog row {rowIndex + 1}.");

            if (!decimal.TryParse(row[7], NumberStyles.Number, CultureInfo.InvariantCulture, out var selling) ||
                selling <= cost)
            {
                throw new InvalidOperationException($"Selling price must be greater than cost on catalog row {rowIndex + 1}.");
            }

            items.Add(new GMasterCatalogItem(
                sku,
                ArabicName,
                englishName,
                row[3].Trim(),
                row[4].Trim(),
                categoryKey,
                cost,
                selling,
                row[8].Trim()));
        }

        return items;
    }

    private static void EnsurePlainText(IReadOnlyList<string> fields, int rowNumber)
    {
        // The bundled source is data, never markup. Reject tags rather than attempting to sanitize
        // attacker-controlled HTML into product names, metadata, or admin comments.
        if (fields.Any(value => value.Contains('<') || value.Contains('>')))
            throw new InvalidOperationException($"Catalog row {rowNumber} contains HTML markup.");

        if (fields.Any(value => value.IndexOf('\0') >= 0))
            throw new InvalidOperationException($"Catalog row {rowNumber} contains a null character.");
    }

    /// <summary>
    /// RFC-4180-style parser supporting quoted commas, escaped quotes, and new lines inside fields.
    /// </summary>
    private static List<List<string>> ReadCsvRows(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var ch = csv[i];

            if (ch == '"')
            {
                if (quoted && i + 1 < csv.Length && csv[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }

                continue;
            }

            if (ch == ',' && !quoted)
            {
                row.Add(field.ToString());
                field.Clear();
                continue;
            }

            if ((ch == '\r' || ch == '\n') && !quoted)
            {
                if (ch == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                    i++;

                row.Add(field.ToString());
                field.Clear();

                if (row.Any(value => !string.IsNullOrWhiteSpace(value)))
                    rows.Add(row);

                row = new List<string>();
                continue;
            }

            field.Append(ch);
        }

        if (quoted)
            throw new InvalidOperationException("The GMaster catalog contains an unterminated quoted field.");

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            if (row.Any(value => !string.IsNullOrWhiteSpace(value)))
                rows.Add(row);
        }

        if (rows.Count > 0 && rows[0].Count > 0)
            rows[0][0] = rows[0][0].TrimStart('\uFEFF');

        return rows;
    }
}
