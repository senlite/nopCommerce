namespace TwinParticles.CheckEngine.Application.Portals;

public sealed class WorkshopPortalCapabilities
{
    public bool CanViewCredit { get; init; }

    public bool CanRaiseInvoice { get; init; }

    public bool CanAssignTechnician { get; init; }

    public bool IsFrontDesk { get; init; }
}
