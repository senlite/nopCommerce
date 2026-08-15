using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class AiEmbeddingProviderRouter : IAiEmbeddingPort
{
    private readonly OpenAiCompatibleEmbeddingPort _openAiPort;
    private readonly AzureOpenAiEmbeddingPort _azurePort;
    private readonly DeterministicTextEmbeddingPort _deterministicPort;
    private readonly CheckEngineAiOptions _options;

    public AiEmbeddingProviderRouter(
        CheckEngineAiOptions? options = null,
        OpenAiCompatibleEmbeddingPort? openAiPort = null,
        AzureOpenAiEmbeddingPort? azurePort = null,
        DeterministicTextEmbeddingPort? deterministicPort = null)
    {
        _options = options ?? CheckEngineAiOptions.Current;
        _openAiPort = openAiPort ?? new OpenAiCompatibleEmbeddingPort(_options);
        _azurePort = azurePort ?? new AzureOpenAiEmbeddingPort(_options);
        _deterministicPort = deterministicPort ?? new DeterministicTextEmbeddingPort();
    }

    public async Task<AiEmbeddingResult> EmbedAsync(AiEmbeddingRequest request, CancellationToken cancellationToken)
    {
        if (HasProviderCredentials())
        {
            var providerResult = await ResolveProvider().EmbedAsync(request, cancellationToken);
            if (providerResult.Success)
                return providerResult;
        }

        return await _deterministicPort.EmbedAsync(request, cancellationToken);
    }

    private IAiEmbeddingPort ResolveProvider()
    {
        return _options.ProviderKind switch
        {
            AiProviderKind.AzureOpenAi => _azurePort,
            AiProviderKind.Anthropic => _deterministicPort,
            _ => _openAiPort
        };
    }

    private bool HasProviderCredentials()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.BaseUrl))
            return false;

        return _options.ProviderKind switch
        {
            AiProviderKind.AzureOpenAi => !string.IsNullOrWhiteSpace(_options.AzureEmbeddingDeploymentName)
                || !string.IsNullOrWhiteSpace(_options.AzureDeploymentName),
            AiProviderKind.Anthropic => false,
            _ => true
        };
    }
}
