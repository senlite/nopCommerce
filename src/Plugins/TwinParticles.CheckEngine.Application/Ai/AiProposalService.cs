using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.Ai;

public sealed class AiProposalDto
{
    public string ProposalId { get; init; } = string.Empty;

    public string FeatureKey { get; init; } = string.Empty;

    public string PromptKey { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public bool IsPublished { get; init; }

    public string? ErrorCode { get; init; }

    public string ProviderName { get; init; } = string.Empty;
}

public sealed class AiProposalService
{
    private readonly IAiCompletionPort _aiCompletionPort;
    private readonly IAiFeatureToggle? _featureToggle;

    public AiProposalService(IAiCompletionPort aiCompletionPort, IAiFeatureToggle? featureToggle = null)
    {
        _aiCompletionPort = aiCompletionPort;
        _featureToggle = featureToggle;
    }

    public async Task<AiProposalDto> CreateProposalAsync(
        string featureKey,
        string promptKey,
        string prompt,
        CancellationToken cancellationToken)
    {
        if (_featureToggle is not null && !_featureToggle.IsEnabled(featureKey))
        {
            return new AiProposalDto
            {
                ProposalId = Guid.NewGuid().ToString("N"),
                FeatureKey = featureKey,
                PromptKey = promptKey,
                Content = string.Empty,
                IsPublished = false,
                ErrorCode = "ai.disabled"
            };
        }

        var result = await _aiCompletionPort.CompleteAsync(new AiCompletionRequest
        {
            PromptKey = promptKey,
            Prompt = prompt
        }, cancellationToken);

        return new AiProposalDto
        {
            ProposalId = Guid.NewGuid().ToString("N"),
            FeatureKey = featureKey,
            PromptKey = promptKey,
            Content = result.Success ? result.Text : string.Empty,
            IsPublished = false,
            ErrorCode = result.Success ? null : result.ErrorCode ?? "ai.completion_failed",
            ProviderName = result.ProviderName
        };
    }
}
