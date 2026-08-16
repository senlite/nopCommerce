namespace TwinParticles.CheckEngine.Domain.Ai;

public interface IAiPromptStore
{
    AiPromptDefinition? Resolve(string promptKey, string? locale = null);
}
