using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class AiEmbeddingProviderRouter : IAiEmbeddingPort
{
    private readonly OpenAiCompatibleEmbeddingPort _openAiPort;
    private readonly DeterministicTextEmbeddingPort _deterministicPort;
    private readonly CheckEngineAiOptions _options;

    public AiEmbeddingProviderRouter(
        CheckEngineAiOptions? options = null,
        OpenAiCompatibleEmbeddingPort? openAiPort = null,
        DeterministicTextEmbeddingPort? deterministicPort = null)
    {
        _options = options ?? CheckEngineAiOptions.Current;
        _openAiPort = openAiPort ?? new OpenAiCompatibleEmbeddingPort(_options);
        _deterministicPort = deterministicPort ?? new DeterministicTextEmbeddingPort();
    }

    public async Task<AiEmbeddingResult> EmbedAsync(AiEmbeddingRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey) && !string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            var providerResult = await _openAiPort.EmbedAsync(request, cancellationToken);
            if (providerResult.Success)
                return providerResult;
        }

        return await _deterministicPort.EmbedAsync(request, cancellationToken);
    }
}
