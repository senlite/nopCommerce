using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.Ai;

public sealed class NullAiCompletionPort : IAiCompletionPort
{
    public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
    {
        return Task.FromResult(new AiCompletionResult
        {
            Success = false,
            Text = string.Empty,
            ProviderName = "null",
            PromptHash = string.Empty,
            TokenUsage = 0,
            ErrorCode = "ai.disabled"
        });
    }
}
