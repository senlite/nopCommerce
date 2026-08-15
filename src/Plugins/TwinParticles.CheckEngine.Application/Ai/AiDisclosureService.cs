using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.Ai;

public sealed class AiDisclosureService
{
    private readonly IAiDisclosureAcknowledgement _acknowledgement;

    public AiDisclosureService(IAiDisclosureAcknowledgement acknowledgement)
    {
        _acknowledgement = acknowledgement;
    }

    public bool Acknowledge()
    {
        _acknowledgement.Acknowledge();
        return _acknowledgement.IsAcknowledged;
    }
}
