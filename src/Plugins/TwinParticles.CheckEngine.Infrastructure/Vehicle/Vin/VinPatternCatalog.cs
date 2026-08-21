using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;

public static class VinPatternCatalog
{
    public const string BmwMakeCode = "BMW";

    private const string ResourcePrefix = "TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin.Data.";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<IReadOnlyList<BmwVinPatternCatalogDocument>> LoadAllAsync(CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var names = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                           && name.EndsWith("-vin-patterns.json", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var catalogs = new List<BmwVinPatternCatalogDocument>(names.Length);
        foreach (var name in names)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var stream = assembly.GetManifestResourceStream(name)
                                     ?? throw new FileNotFoundException($"Embedded VIN pattern catalog {name} was not found.");
            var document = await JsonSerializer.DeserializeAsync<BmwVinPatternCatalogDocument>(stream, SerializerOptions, cancellationToken)
                           ?? new BmwVinPatternCatalogDocument();
            catalogs.Add(WithMakeCode(document, name));
        }

        return catalogs;
    }

    public static bool IsBmw(BmwVinPatternCatalogDocument catalog)
        => string.Equals(catalog.MakeCode, BmwMakeCode, StringComparison.OrdinalIgnoreCase);

    private static BmwVinPatternCatalogDocument WithMakeCode(BmwVinPatternCatalogDocument document, string resourceName)
    {
        if (!string.IsNullOrWhiteSpace(document.MakeCode))
            return document;

        var fileName = resourceName[ResourcePrefix.Length..];
        var slug = fileName.Replace("-vin-patterns.json", string.Empty, StringComparison.Ordinal);
        return new BmwVinPatternCatalogDocument
        {
            Version = document.Version,
            MakeCode = slug.Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant(),
            Wmis = document.Wmis,
            Patterns = document.Patterns
        };
    }
}
