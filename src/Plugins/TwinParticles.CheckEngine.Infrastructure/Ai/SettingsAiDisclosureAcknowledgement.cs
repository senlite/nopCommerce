using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class SettingsAiDisclosureAcknowledgement : IAiDisclosureAcknowledgement
{
    private readonly CheckEngineAiOptions _options;

    public SettingsAiDisclosureAcknowledgement(CheckEngineAiOptions? options = null)
    {
        _options = options ?? CheckEngineAiOptions.Current;
    }

    public bool IsAcknowledged => _options.DisclosureAcknowledged;

    public void Acknowledge()
    {
        _options.DisclosureAcknowledged = true;
    }
}
