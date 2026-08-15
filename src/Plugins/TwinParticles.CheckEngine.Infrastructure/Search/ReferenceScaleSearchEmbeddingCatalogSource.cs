using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

/// <summary>
/// Loads the reference-scale search catalog from the bundled test corpus JSON.
/// </summary>
public sealed class ReferenceScaleSearchEmbeddingCatalogSource : ISearchEmbeddingCatalogSource
{
    public Task<IReadOnlyList<SearchEmbeddingDocument>> GetDocumentsAsync(string locale, CancellationToken cancellationToken)
    {
        var documents = LoadCatalog().Documents;
        var filtered = string.IsNullOrWhiteSpace(locale)
            ? documents
            : documents.Where(document => string.Equals(document.Locale, locale, StringComparison.OrdinalIgnoreCase)).ToList();

        return Task.FromResult<IReadOnlyList<SearchEmbeddingDocument>>(filtered);
    }

    public Task<IReadOnlyList<SearchEmbeddingDocument>> GetStaleDocumentsAsync(string locale, CancellationToken cancellationToken) =>
        GetDocumentsAsync(locale, cancellationToken);

    public async Task<int> GetCatalogCountAsync(string locale, CancellationToken cancellationToken)
    {
        var documents = await GetDocumentsAsync(locale, cancellationToken);
        return documents.Count;
    }

    public async Task<int> GetStaleCountAsync(string locale, CancellationToken cancellationToken)
    {
        var documents = await GetStaleDocumentsAsync(locale, cancellationToken);
        return documents.Count;
    }

    public static ReferenceScaleCatalog LoadCatalog()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Tests", "corpus", "search", "reference-scale-search-catalog.json");
            if (File.Exists(candidate))
                return Deserialize(File.ReadAllText(candidate));

            candidate = Path.Combine(dir.FullName, "Tests", "corpus", "search", "reference-scale-search-catalog.json");
            if (File.Exists(candidate))
                return Deserialize(File.ReadAllText(candidate));
        }

        throw new FileNotFoundException("Unable to locate reference-scale-search-catalog.json");
    }

    private static ReferenceScaleCatalog Deserialize(string json) =>
        JsonSerializer.Deserialize<ReferenceScaleCatalog>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException("Invalid reference-scale-search-catalog.json");

    public sealed class ReferenceScaleCatalog
    {
        public List<SearchEmbeddingDocument> Documents { get; set; } = [];
    }
}
