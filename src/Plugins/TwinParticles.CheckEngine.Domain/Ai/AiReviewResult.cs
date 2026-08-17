namespace TwinParticles.CheckEngine.Domain.Ai;

public sealed class AiReviewResult
{
    public bool Success { get; init; }

    public string? ReasonCode { get; init; }

    public static AiReviewResult Approved() => new() { Success = true };

    public static AiReviewResult Rejected() => new() { Success = true };

    public static AiReviewResult NotFound() => new() { Success = false, ReasonCode = "ai.review.not_found" };

    public static AiReviewResult Blocked(string reasonCode) => new() { Success = false, ReasonCode = reasonCode };
}
