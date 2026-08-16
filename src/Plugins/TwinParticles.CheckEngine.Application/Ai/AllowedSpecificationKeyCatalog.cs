using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.Ai;

/// <summary>
/// Loads the embedded allowed specification key catalog (FR-521).
/// </summary>
public sealed class AllowedSpecificationKeyCatalog : IAllowedSpecificationKeyCatalog
{
    private static readonly Lazy<HashSet<string>> EmbeddedKeys = new(LoadEmbeddedKeys);

    private static readonly HashSet<string> DefaultKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Material",
        "Thread",
        "Thread Size",
        "Diameter",
        "Length",
        "Width",
        "Height",
        "Weight",
        "Color",
        "Finish",
        "Voltage",
        "Amperage",
        "Capacity",
        "Volume",
        "Viscosity",
        "Position",
        "Side",
        "Drive Type",
        "Transmission",
        "Engine Code",
        "OEM Number",
        "Manufacturer",
        "Warranty",
        "Quantity",
        "Package Contents",
        "Includes",
        "Notes"
    };

    private readonly HashSet<string> _keys;

    public AllowedSpecificationKeyCatalog(IAllowedSpecificationKeyOverridesSource? overridesSource = null)
    {
        _keys = MergeKeys(EmbeddedKeys.Value, overridesSource?.GetOverrides());
    }

    public AllowedSpecificationKeyCatalog(IEnumerable<string> keys)
    {
        _keys = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> GetAllowedKeys() => _keys;

    public bool IsAllowed(string key) =>
        !string.IsNullOrWhiteSpace(key) && _keys.Contains(key.Trim());

    public static IReadOnlyCollection<string> GetEmbeddedKeys() => EmbeddedKeys.Value;

    public static HashSet<string> MergeKeys(
        IReadOnlyCollection<string> embedded,
        SpecificationKeyOverrides? overrides)
    {
        var merged = new HashSet<string>(embedded, StringComparer.OrdinalIgnoreCase);
        if (overrides is null)
            return merged;

        foreach (var key in overrides.Remove)
        {
            if (!string.IsNullOrWhiteSpace(key))
                merged.Remove(key.Trim());
        }

        foreach (var key in overrides.Add)
        {
            if (!string.IsNullOrWhiteSpace(key))
                merged.Add(key.Trim());
        }

        return merged;
    }

    private static HashSet<string> LoadEmbeddedKeys()
    {
        var assembly = typeof(AllowedSpecificationKeyCatalog).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("allowed-specification-keys.json", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
            return new HashSet<string>(DefaultKeys, StringComparer.OrdinalIgnoreCase);

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return new HashSet<string>(DefaultKeys, StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(stream);
        var payload = JsonSerializer.Deserialize<AllowedKeysFile>(
            reader.ReadToEnd(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (payload?.Keys is null || payload.Keys.Count == 0)
            return new HashSet<string>(DefaultKeys, StringComparer.OrdinalIgnoreCase);

        return new HashSet<string>(payload.Keys, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class AllowedKeysFile
    {
        public List<string>? Keys { get; set; }
    }
}
