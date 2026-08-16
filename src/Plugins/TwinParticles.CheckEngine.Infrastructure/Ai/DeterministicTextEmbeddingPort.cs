using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

/// <summary>
/// Offline-safe embedding port for tests and degraded mode. Uses deterministic bag-of-words vectors.
/// </summary>
public sealed class DeterministicTextEmbeddingPort : IAiEmbeddingPort
{
    public Task<AiEmbeddingResult> EmbedAsync(AiEmbeddingRequest request, CancellationToken cancellationToken)
    {
        var vector = VectorMath.EmbedText(request.Text);
        return Task.FromResult(new AiEmbeddingResult
        {
            Success = true,
            Vector = vector,
            ProviderName = "deterministic",
            ModelHash = VectorMath.HashModel("deterministic", VectorMath.DefaultDimensions),
            TokenUsage = System.Math.Max(1, request.Text.Length / 4)
        });
    }
}
