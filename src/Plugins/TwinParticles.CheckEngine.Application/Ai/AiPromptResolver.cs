using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.Ai;

/// <summary>
/// Resolves versioned prompt templates and applies placeholder substitution (FR-563).
/// </summary>
public sealed class AiPromptResolver
{
    private readonly IAiPromptStore _promptStore;

    public AiPromptResolver(IAiPromptStore promptStore)
    {
        _promptStore = promptStore;
    }

    public AiPromptDefinition? ResolveDefinition(string promptKey, string? locale = null) =>
        _promptStore.Resolve(promptKey, locale);

    public string Format(string promptKey, IReadOnlyDictionary<string, string?> placeholders, string? locale = null)
    {
        var definition = _promptStore.Resolve(promptKey, locale);
        var template = definition?.Body ?? string.Empty;

        foreach (var (key, value) in placeholders)
        {
            template = template.Replace($"{{{key}}}", value ?? string.Empty, StringComparison.Ordinal);
        }

        return template;
    }
}
