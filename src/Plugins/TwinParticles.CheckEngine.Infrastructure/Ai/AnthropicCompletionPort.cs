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

public sealed class AnthropicCompletionPort : IAiCompletionPort
{
    private readonly CheckEngineAiOptions _options;
    private readonly HttpClient _httpClient;

    public AnthropicCompletionPort(CheckEngineAiOptions? options = null, HttpClient? httpClient = null)
    {
        _options = options ?? CheckEngineAiOptions.Current;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return DisabledResult();

        try
        {
            var endpoint = string.IsNullOrWhiteSpace(_options.BaseUrl)
                ? "https://api.anthropic.com/v1/messages"
                : $"{_options.BaseUrl.TrimEnd('/')}/v1/messages";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Headers.Add("x-api-key", _options.ApiKey);
            httpRequest.Headers.Add("anthropic-version", _options.AnthropicVersion);

            var body = new
            {
                model = string.IsNullOrWhiteSpace(_options.Model) ? "claude-3-5-haiku-20241022" : _options.Model,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
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
                    ProviderName = "anthropic",
                    PromptHash = HashPrompt(request),
                    ErrorCode = "ai.provider_http_error"
                };
            }

            using var document = JsonDocument.Parse(payload);
            var text = document.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            var tokenUsage = 0;
            if (document.RootElement.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("input_tokens", out var inputTokens))
                    tokenUsage += inputTokens.GetInt32();
                if (usage.TryGetProperty("output_tokens", out var outputTokens))
                    tokenUsage += outputTokens.GetInt32();
            }

            return new AiCompletionResult
            {
                Success = true,
                Text = text.Trim(),
                ProviderName = "anthropic",
                PromptHash = HashPrompt(request),
                TokenUsage = tokenUsage
            };
        }
        catch
        {
            return new AiCompletionResult
            {
                Success = false,
                ProviderName = "anthropic",
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
            ProviderName = "anthropic",
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
