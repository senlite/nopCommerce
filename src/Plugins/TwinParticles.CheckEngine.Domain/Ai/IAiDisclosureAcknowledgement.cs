namespace TwinParticles.CheckEngine.Domain.Ai;

public interface IAiDisclosureAcknowledgement
{
    bool IsAcknowledged { get; }

    void Acknowledge();
}
