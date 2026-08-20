using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Infrastructure.Tenancy;

public sealed class HttpTenantWebhookDeliveryPort : ITenantWebhookDeliveryPort
{
    private readonly HttpClient _httpClient;

    public HttpTenantWebhookDeliveryPort(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<bool> DeliverAsync(TenantWebhookDelivery delivery, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, delivery.Url);
            request.Headers.TryAddWithoutValidation("X-CE-Signature", delivery.Signature);
            request.Headers.TryAddWithoutValidation("X-CE-Event", delivery.EventType);
            request.Content = new StringContent(delivery.Body ?? string.Empty, Encoding.UTF8, "application/json");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
