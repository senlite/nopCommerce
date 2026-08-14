using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;

public static class BmwVinPatternCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<BmwVinPatternCatalogDocument> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin.Data.bmw-vin-patterns.json")
            ?? throw new FileNotFoundException("Embedded BMW VIN pattern catalog was not found.");

        var document = await JsonSerializer.DeserializeAsync<BmwVinPatternCatalogDocument>(stream, SerializerOptions, cancellationToken);
        return document ?? new BmwVinPatternCatalogDocument();
    }
}
