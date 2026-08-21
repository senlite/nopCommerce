using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

public sealed class VehicleReferenceCatalogDocument
{
    [JsonPropertyName("makeCode")]
    public string MakeCode { get; init; } = string.Empty;

    [JsonPropertyName("makeName")]
    public string MakeName { get; init; } = string.Empty;

    [JsonPropertyName("makeArabicAlias")]
    public string MakeArabicAlias { get; init; } = string.Empty;

    [JsonPropertyName("models")]
    public IReadOnlyList<VehicleReferenceModelDocument> Models { get; init; } = [];
}

public sealed class VehicleReferenceModelDocument
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("arabicAlias")]
    public string ArabicAlias { get; init; } = string.Empty;

    [JsonPropertyName("generations")]
    public IReadOnlyList<VehicleReferenceGenerationDocument> Generations { get; init; } = [];
}

public sealed class VehicleReferenceGenerationDocument
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("startYear")]
    public int StartYear { get; init; }

    [JsonPropertyName("endYear")]
    public int? EndYear { get; init; }

    [JsonPropertyName("bodies")]
    public IReadOnlyList<VehicleReferenceBodyDocument> Bodies { get; init; } = [];

    [JsonPropertyName("engines")]
    public IReadOnlyList<VehicleReferenceEngineDocument> Engines { get; init; } = [];

    [JsonPropertyName("trims")]
    public IReadOnlyList<VehicleReferenceTrimDocument> Trims { get; init; } = [];
}

public sealed class VehicleReferenceBodyDocument
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("doors")]
    public int Doors { get; init; }
}

public sealed class VehicleReferenceEngineDocument
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("fuelType")]
    public string FuelType { get; init; } = string.Empty;

    [JsonPropertyName("displacementCc")]
    public int DisplacementCc { get; init; }

    [JsonPropertyName("powerHp")]
    public int PowerHp { get; init; }
}

public sealed class VehicleReferenceTrimDocument
{
    [JsonPropertyName("trimName")]
    public string TrimName { get; init; } = string.Empty;

    [JsonPropertyName("bodyCode")]
    public string BodyCode { get; init; } = string.Empty;

    [JsonPropertyName("engineCode")]
    public string EngineCode { get; init; } = string.Empty;
}

public sealed class TopBrandReferenceCatalogDocument
{
    [JsonPropertyName("version")]
    public int Version { get; init; }

    [JsonPropertyName("catalogs")]
    public IReadOnlyList<VehicleReferenceCatalogDocument> Catalogs { get; init; } = [];
}

public static class TopBrandReferenceCatalog
{
    private const string ResourceName = "TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin.Data.top-brands-reference.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<TopBrandReferenceCatalogDocument> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream(ResourceName)
            ?? throw new FileNotFoundException("Embedded top-brand vehicle catalog was not found.");

        var document = await JsonSerializer.DeserializeAsync<TopBrandReferenceCatalogDocument>(stream, SerializerOptions, cancellationToken);
        return document ?? new TopBrandReferenceCatalogDocument();
    }
}
