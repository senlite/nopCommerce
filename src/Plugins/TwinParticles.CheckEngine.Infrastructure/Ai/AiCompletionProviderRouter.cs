using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class AiCompletionProviderRouter : IAiCompletionPort
{
    private readonly OpenAiCompatibleCompletionPort _openAiPort;
    private readonly AzureOpenAiCompletionPort _azurePort;
    private readonly AnthropicCompletionPort _anthropicPort;
    private readonly CheckEngineAiOptions _options;

    public AiCompletionProviderRouter(
        CheckEngineAiOptions? options = null,
        OpenAiCompatibleCompletionPort? openAiPort = null,
        AzureOpenAiCompletionPort? azurePort = null,
        AnthropicCompletionPort? anthropicPort = null)
    {
        _options = options ?? CheckEngineAiOptions.Current;
        _openAiPort = openAiPort ?? new OpenAiCompatibleCompletionPort(_options);
        _azurePort = azurePort ?? new AzureOpenAiCompletionPort(_options);
        _anthropicPort = anthropicPort ?? new AnthropicCompletionPort(_options);
    }

    public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
    {
        return _options.ProviderKind switch
        {
            AiProviderKind.AzureOpenAi => _azurePort.CompleteAsync(request, ct),
            AiProviderKind.Anthropic => _anthropicPort.CompleteAsync(request, ct),
            _ => _openAiPort.CompleteAsync(request, ct)
        };
    }
}
