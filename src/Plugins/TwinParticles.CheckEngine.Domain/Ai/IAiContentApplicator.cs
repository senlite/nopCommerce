using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Ai;

public interface IAiContentApplicator
{
    Task<AiContentApplyResult> ApplyAsync(AiGenerationCandidate candidate, CancellationToken cancellationToken);
}

public sealed class AiContentApplyResult
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public static AiContentApplyResult Ok() => new() { Success = true };

    public static AiContentApplyResult Fail(string errorCode) => new() { Success = false, ErrorCode = errorCode };
}
