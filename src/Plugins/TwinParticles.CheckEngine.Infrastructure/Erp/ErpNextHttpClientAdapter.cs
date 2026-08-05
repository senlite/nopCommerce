using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Infrastructure.Erp;

public sealed class ErpNextHttpClientAdapter : IErpClientAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ErpConnectionOptions _options;
    private readonly StubErpClientAdapter _stub = new();

    public ErpNextHttpClientAdapter(ErpConnectionOptions? options = null, HttpClient? httpClient = null)
    {
        _options = options ?? ErpConnectionOptions.Current;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<bool> PushAsync(ErpSyncJob job, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            return await _stub.PushAsync(job, cancellationToken);

        try
        {
            var entityType = job.EntityType.ToString();
            var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/api/resource/{entityType}";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            ApplyAuth(request);
            request.Content = new StringContent(job.Payload ?? string.Empty, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> PullInventorySnapshotAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            return await _stub.PullInventorySnapshotAsync(cancellationToken);

        try
        {
            var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/api/resource/Item?fields=[\"name\",\"item_code\",\"item_name\",\"stock_uom\"]";
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            ApplyAuth(request);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private void ApplyAuth(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return;

        var token = string.IsNullOrWhiteSpace(_options.ApiSecret)
            ? _options.ApiKey
            : $"{_options.ApiKey}:{_options.ApiSecret}";

        request.Headers.Authorization = new AuthenticationHeaderValue("token", token);
    }
}
