namespace TwinParticles.CheckEngine.Domain.Vehicle.Admin;

public sealed class VehicleMergeRepositoryResult
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public int MovedChildren { get; init; }

    public int MovedAliases { get; init; }

    public static VehicleMergeRepositoryResult Ok(int movedChildren, int movedAliases)
        => new() { Success = true, MovedChildren = movedChildren, MovedAliases = movedAliases };

    public static VehicleMergeRepositoryResult Fail(string errorCode)
        => new() { Success = false, ErrorCode = errorCode };
}
