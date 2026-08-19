namespace TwinParticles.CheckEngine.Domain.Fleet;

public sealed class FleetMember
{
    public int Id { get; set; }

    public int FleetAccountId { get; set; }

    public int CustomerId { get; set; }

    public bool CanApprove { get; set; }
}
