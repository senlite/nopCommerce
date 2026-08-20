using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Tenancy;

public static class TenantWebhookEvents
{
    public const string VehiclesListed = "vehicles.listed";
    public const string VinDecoded = "vin.decoded";
    public const string OemResolved = "oem.resolved";
    public const string FitmentEvaluated = "fitment.evaluated";
    public const string SearchCompleted = "search.completed";

    public const string DefaultCsv =
        VehiclesListed + "," + VinDecoded + "," + OemResolved + "," + FitmentEvaluated + "," + SearchCompleted;
}

public sealed class TenantWebhookSubscription
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public string TargetUrl { get; set; } = string.Empty;

    public string SigningSecretProtected { get; set; } = string.Empty;

    public string EventTypesCsv { get; set; } = TenantWebhookEvents.DefaultCsv;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedUtc { get; set; }
}

public interface ITenantWebhookStore
{
    Task<int> InsertAsync(TenantWebhookSubscription subscription, CancellationToken cancellationToken);

    Task<IReadOnlyList<TenantWebhookSubscription>> ListByTenantAsync(int tenantId, CancellationToken cancellationToken);

    Task SetActiveAsync(int subscriptionId, bool isActive, CancellationToken cancellationToken);
}

public interface ITenantWebhookSecretProtector
{
    string? Protect(string? plaintext);

    string? Unprotect(string? storedValue);
}

public sealed class TenantWebhookDelivery
{
    public required string Url { get; init; }

    public required string Body { get; init; }

    public required string Signature { get; init; }

    public required string EventType { get; init; }
}

public interface ITenantWebhookDeliveryPort
{
    Task<bool> DeliverAsync(TenantWebhookDelivery delivery, CancellationToken cancellationToken);
}
