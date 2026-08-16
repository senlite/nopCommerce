namespace TwinParticles.CheckEngine.Domain.Ai;

public sealed class AiPromptDefinition
{
    public string PromptKey { get; init; } = string.Empty;

    public string Version { get; init; } = "1";

    public string Body { get; init; } = string.Empty;

    public string? ModelHint { get; init; }

    public string? Locale { get; init; }
}
