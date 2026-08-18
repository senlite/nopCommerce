namespace TwinParticles.CheckEngine.Domain.Marketplace;

public sealed class VendorActor
{
    public int? VendorId { get; init; }

    public bool IsOperatorAdmin { get; init; }

    public bool CanBypassIsolation => IsOperatorAdmin;

    public static VendorActor OperatorAdmin { get; } = new() { IsOperatorAdmin = true };

    public static VendorActor Vendor(int vendorId) => new() { VendorId = vendorId };

    public static VendorActor Anonymous { get; } = new();
}
