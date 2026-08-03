using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Application.Oem;

public sealed class OemSupersessionChainResult
{
    private OemSupersessionChainResult(bool success, IReadOnlyList<int> chainOemNumberIds, string? errorCode)
    {
        Success = success;
        ChainOemNumberIds = chainOemNumberIds;
        ErrorCode = errorCode;
    }

    public bool Success { get; }

    public IReadOnlyList<int> ChainOemNumberIds { get; }

    public string? ErrorCode { get; }

    public static OemSupersessionChainResult Ok(IReadOnlyList<int> chainOemNumberIds)
        => new(true, chainOemNumberIds, null);

    public static OemSupersessionChainResult Fail(string errorCode, IReadOnlyList<int> partialChainOemNumberIds)
        => new(false, partialChainOemNumberIds, errorCode);
}
