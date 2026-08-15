using System;

namespace TwinParticles.CheckEngine.Domain.Ai;

public sealed class AiGenerationCandidate
{
    public int Id { get; set; }

    public AiGenerationEntityType EntityType { get; set; }

    public int EntityId { get; set; }

    public string FeatureKey { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;

    public string OutputText { get; set; } = string.Empty;

    public string PromptKey { get; set; } = string.Empty;

    public string PromptHash { get; set; } = string.Empty;

    public decimal? QualityScore { get; set; }

    public bool IsPublished { get; set; }

    public string ReviewStatus { get; set; } = "pending";

    public string? Reviewer { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset? ReviewedUtc { get; set; }
}
