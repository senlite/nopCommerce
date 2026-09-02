using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TwinParticles.CheckEngine.Domain.Configuration;
using TwinParticles.CheckEngine.Domain.Images;

namespace TwinParticles.CheckEngine.Application.Images;

/// <summary>
/// Operator-configured supplier image sourcing (H1.24): CSV manifests and URL templates.
/// Licensed third-party image APIs remain an external integration boundary.
/// </summary>
public sealed class SupplierImageSourcingService
{
    private readonly BatchImageReplacementService _batchService;
    private readonly CheckEngineSettings _settings;

    public SupplierImageSourcingService(
        BatchImageReplacementService batchService,
        IOptions<CheckEngineSettings> settings)
    {
        _batchService = batchService;
        _settings = settings.Value;
    }

    public async Task<BatchImageReplacementResult> ReplaceFromManifestCsvAsync(
        string csvContent,
        string actor,
        CancellationToken cancellationToken)
    {
        var items = ParseManifestCsv(csvContent);
        return await _batchService.ReplaceBySkuAsync(items, actor, cancellationToken);
    }

    public async Task<BatchImageReplacementResult> ReplaceFromUrlTemplateAsync(
        IReadOnlyList<string> skus,
        string actor,
        CancellationToken cancellationToken,
        string? urlTemplateOverride = null)
    {
        var template = ResolveUrlTemplate(urlTemplateOverride, _settings.Images.SupplierImageUrlTemplate);
        if (template is null)
        {
            return new BatchImageReplacementResult
            {
                Items =
                [
                    new BatchImageReplacementItemResult
                    {
                        Sku = string.Empty,
                        Status = BatchImageReplacementStatus.Failed,
                        ErrorCode = "image.supplier.template_not_configured"
                    }
                ]
            };
        }

        return await _batchService.ReplaceBySkuAsync(ExpandUrlTemplate(template, skus), actor, cancellationToken);
    }

    public static string? ResolveUrlTemplate(string? overrideTemplate, string? configuredTemplate)
    {
        if (!string.IsNullOrWhiteSpace(overrideTemplate))
            return overrideTemplate.Trim();
        if (!string.IsNullOrWhiteSpace(configuredTemplate))
            return configuredTemplate.Trim();
        return null;
    }

    public static IReadOnlyList<BatchImageReplacementItem> ExpandUrlTemplate(string template, IReadOnlyList<string> skus)
        => skus
            .Where(sku => !string.IsNullOrWhiteSpace(sku))
            .Select(sku =>
            {
                var trimmed = sku.Trim();
                return new BatchImageReplacementItem
                {
                    Sku = trimmed,
                    SourceUrl = template.Replace("{sku}", trimmed, StringComparison.OrdinalIgnoreCase),
                    SeoName = trimmed
                };
            })
            .ToList();

    public static IReadOnlyList<BatchImageReplacementItem> ParseManifestCsv(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
            return [];

        using var reader = new StringReader(csvContent);
        var items = new List<BatchImageReplacementItem>();
        string? line;
        var isHeader = true;

        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                continue;

            if (isHeader && parts[0].Equals("sku", StringComparison.OrdinalIgnoreCase))
            {
                isHeader = false;
                continue;
            }

            isHeader = false;
            items.Add(new BatchImageReplacementItem
            {
                Sku = parts[0],
                SourceUrl = parts[1],
                SeoName = parts.Length > 2 ? parts[2] : parts[0]
            });
        }

        return items;
    }
}
