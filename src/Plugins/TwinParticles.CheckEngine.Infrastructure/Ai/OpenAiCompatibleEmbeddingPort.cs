using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class OpenAiCompatibleEmbeddingPort : IAiEmbeddingPort
{
    private readonly CheckEngineAiOptions _options;
    private readonly HttpClient _httpClient;

    public OpenAiCompatibleEmbeddingPort(CheckEngineAiOptions? options = null, HttpClient? httpClient = null)
    {
        _options = options ?? CheckEngineAiOptions.Current;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<AiEmbeddingResult> EmbedAsync(AiEmbeddingRequest request, CancellationToken cancellationToken)
    {
        var (baseUrl, apiKey) = _options.ResolveEmbeddingCredentials();
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(baseUrl))
            return DisabledResult();

        try
        {
            var endpoint = $"{baseUrl.TrimEnd('/')}/embeddings";
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var body = new
            {
                model = string.IsNullOrWhiteSpace(_options.EmbeddingModel) ? "text-embedding-3-small" : _options.EmbeddingModel,
                input = request.Text
            };

            httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new AiEmbeddingResult
                {
                    Success = false,
                    ProviderName = "openai-compatible-embeddings",
                    ErrorCode = "ai.provider_http_error"
                };
            }

            using var document = JsonDocument.Parse(payload);
            var values = document.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding");

            var vector = new float[values.GetArrayLength()];
            var index = 0;
            foreach (var value in values.EnumerateArray())
                vector[index++] = (float)value.GetDouble();

            var tokenUsage = 0;
            if (document.RootElement.TryGetProperty("usage", out var usage)
                && usage.TryGetProperty("total_tokens", out var totalTokens))
            {
                tokenUsage = totalTokens.GetInt32();
            }

            return new AiEmbeddingResult
            {
                Success = true,
                Vector = vector,
                ProviderName = "openai-compatible-embeddings",
                ModelHash = VectorMath.HashModel(_options.EmbeddingModel, vector.Length),
                TokenUsage = tokenUsage
            };
        }
        catch
        {
            return new AiEmbeddingResult
            {
                Success = false,
                ProviderName = "openai-compatible-embeddings",
                ErrorCode = "ai.provider_degraded"
            };
        }
    }

    private static AiEmbeddingResult DisabledResult()
    {
        return new AiEmbeddingResult
        {
            Success = false,
            ProviderName = "openai-compatible-embeddings",
            ErrorCode = "ai.disabled"
        };
    }
}
