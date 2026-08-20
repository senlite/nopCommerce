using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Application.Tenancy;

public sealed class IssuedWebhook
{
    public required TenantWebhookSubscription Record { get; init; }

    public required string PlaintextSecret { get; init; }
}

public sealed class TenantWebhookService
{
    public const string SecretPrefix = "cwhsec_";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ITenantWebhookStore _store;
    private readonly ITenantWebhookSecretProtector _protector;
    private readonly ITenantWebhookDeliveryPort _delivery;
    private readonly TenantIsolationService _isolation;

    public TenantWebhookService(
        ITenantWebhookStore store,
        ITenantWebhookSecretProtector protector,
        ITenantWebhookDeliveryPort delivery,
        TenantIsolationService isolation)
    {
        _store = store;
        _protector = protector;
        _delivery = delivery;
        _isolation = isolation;
    }

    public async Task<IssuedWebhook?> RegisterAsync(
        TenantActor actor,
        int tenantId,
        string? targetUrl,
        string? eventTypesCsv,
        CancellationToken cancellationToken)
    {
        var decision = await _isolation.AuthorizeAsync(actor, tenantId, write: true, cancellationToken);
        if (!decision.Allowed)
            return null;

        if (!TryNormalizeUrl(targetUrl, out var url))
            return null;

        var plaintext = SecretPrefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var record = new TenantWebhookSubscription
        {
            TenantId = tenantId,
            TargetUrl = url,
            SigningSecretProtected = _protector.Protect(plaintext) ?? plaintext,
            EventTypesCsv = string.IsNullOrWhiteSpace(eventTypesCsv) ? TenantWebhookEvents.DefaultCsv : eventTypesCsv.Trim(),
            IsActive = true,
            CreatedUtc = DateTimeOffset.UtcNow
        };
        record.Id = await _store.InsertAsync(record, cancellationToken);
        return new IssuedWebhook { Record = record, PlaintextSecret = plaintext };
    }

    public async Task<System.Collections.Generic.IReadOnlyList<TenantWebhookSubscription>> ListAsync(
        TenantActor actor,
        int tenantId,
        CancellationToken cancellationToken)
    {
        var decision = await _isolation.AuthorizeAsync(actor, tenantId, write: false, cancellationToken);
        if (!decision.Allowed)
            return [];

        return await _store.ListByTenantAsync(tenantId, cancellationToken);
    }

    public async Task DispatchAsync(Tenant? tenant, string eventType, object payload, CancellationToken cancellationToken)
    {
        if (tenant is null || string.IsNullOrWhiteSpace(eventType))
            return;

        var subscriptions = await _store.ListByTenantAsync(tenant.Id, cancellationToken);
        var body = JsonSerializer.Serialize(new
        {
            apiVersion = "v1",
            @event = eventType,
            occurredUtc = DateTimeOffset.UtcNow,
            tenantId = tenant.Id,
            tenantSlug = tenant.Slug,
            data = payload
        }, JsonOptions);

        foreach (var subscription in subscriptions.Where(s => s.IsActive && SubscribesTo(s, eventType)))
        {
            try
            {
                var secret = _protector.Unprotect(subscription.SigningSecretProtected) ?? string.Empty;
                var signature = Sign(secret, body);
                await _delivery.DeliverAsync(new TenantWebhookDelivery
                {
                    Url = subscription.TargetUrl,
                    Body = body,
                    Signature = signature,
                    EventType = eventType
                }, cancellationToken);
            }
            catch
            {
                // Delivery failures must not fail the public API call that produced the event.
            }
        }
    }

    public static bool TryNormalizeUrl(string? value, out string url)
    {
        url = string.Empty;
        if (!Uri.TryCreate((value ?? string.Empty).Trim(), UriKind.Absolute, out var uri))
            return false;
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            return false;
        url = uri.AbsoluteUri;
        return true;
    }

    public static string Sign(string secret, string body)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret ?? string.Empty), Encoding.UTF8.GetBytes(body ?? string.Empty));
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool SubscribesTo(TenantWebhookSubscription subscription, string eventType)
        => subscription.EventTypesCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(eventType, StringComparer.OrdinalIgnoreCase);
}
