namespace TwinParticles.CheckEngine.Application.Vehicle.Admin;

public sealed class VehicleLifecycleResult
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public int MovedChildren { get; init; }

    public int MovedAliases { get; init; }

    public static VehicleLifecycleResult Ok(int movedChildren = 0, int movedAliases = 0)
        => new() { Success = true, MovedChildren = movedChildren, MovedAliases = movedAliases };

    public static VehicleLifecycleResult Fail(string errorCode)
        => new() { Success = false, ErrorCode = errorCode };
}
