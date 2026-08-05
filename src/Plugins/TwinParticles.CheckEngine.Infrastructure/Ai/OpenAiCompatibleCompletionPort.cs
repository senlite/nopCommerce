using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class OpenAiCompatibleCompletionPort : IAiCompletionPort
{
    private readonly CheckEngineAiOptions _options;
    private readonly HttpClient _httpClient;

    public OpenAiCompatibleCompletionPort(CheckEngineAiOptions? options = null, HttpClient? httpClient = null)
    {
        _options = options ?? CheckEngineAiOptions.Current;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.BaseUrl))
            return DisabledResult();

        try
        {
            var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/chat/completions";
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var body = new
            {
                model = _options.Model,
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                messages = new[]
                {
                    new { role = "user", content = request.Prompt }
                }
            };

            httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            var payload = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                return new AiCompletionResult
                {
                    Success = false,
                    ProviderName = "openai-compatible",
                    PromptHash = HashPrompt(request),
                    ErrorCode = "ai.provider_http_error"
                };
            }

            using var document = JsonDocument.Parse(payload);
            var text = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            var tokenUsage = 0;
            if (document.RootElement.TryGetProperty("usage", out var usage)
                && usage.TryGetProperty("total_tokens", out var totalTokens))
            {
                tokenUsage = totalTokens.GetInt32();
            }

            return new AiCompletionResult
            {
                Success = true,
                Text = text.Trim(),
                ProviderName = "openai-compatible",
                PromptHash = HashPrompt(request),
                TokenUsage = tokenUsage
            };
        }
        catch
        {
            return new AiCompletionResult
            {
                Success = false,
                ProviderName = "openai-compatible",
                PromptHash = HashPrompt(request),
                ErrorCode = "ai.provider_degraded"
            };
        }
    }

    private static AiCompletionResult DisabledResult()
    {
        return new AiCompletionResult
        {
            Success = false,
            Text = string.Empty,
            ProviderName = "null",
            PromptHash = string.Empty,
            TokenUsage = 0,
            ErrorCode = "ai.disabled"
        };
    }

    private static string HashPrompt(AiCompletionRequest request)
    {
        var raw = $"{request.PromptKey}|{request.Prompt}|{request.MaxTokens}|{request.Temperature}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
