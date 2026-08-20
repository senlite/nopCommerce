using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Tenancy;
using TwinParticles.CheckEngine.Domain.Tenancy;
using TwinParticles.CheckEngine.Infrastructure.Security;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class PublicApiWebhookTests
{
    [Test]
    public async Task Webhook_Registration_Should_Be_Tenant_Isolated_And_Signed()
    {
        var harness = Harness.Create();
        var acme = await harness.ProvisionAsync("acme-hooks");
        var beta = await harness.ProvisionAsync("beta-hooks");

        var issued = await harness.Webhooks.RegisterAsync(
            TenantActor.ForTenant(acme.Id),
            acme.Id,
            "https://hooks.acme.test/ce",
            TenantWebhookEvents.VinDecoded,
            CancellationToken.None);
        issued.Should().NotBeNull();
        issued!.PlaintextSecret.Should().StartWith(TenantWebhookService.SecretPrefix);

        var cross = await harness.Webhooks.ListAsync(TenantActor.ForTenant(beta.Id), acme.Id, CancellationToken.None);
        cross.Should().BeEmpty();

        var own = await harness.Webhooks.ListAsync(TenantActor.ForTenant(acme.Id), acme.Id, CancellationToken.None);
        own.Should().ContainSingle();

        await harness.Webhooks.DispatchAsync(acme, TenantWebhookEvents.VinDecoded, new { ok = true }, CancellationToken.None);
        await harness.Webhooks.DispatchAsync(acme, TenantWebhookEvents.OemResolved, new { ok = true }, CancellationToken.None);

        harness.Delivery.Deliveries.Should().ContainSingle();
        var delivery = harness.Delivery.Deliveries[0];
        delivery.EventType.Should().Be(TenantWebhookEvents.VinDecoded);
        delivery.Signature.Should().Be(TenantWebhookService.Sign(issued.PlaintextSecret, delivery.Body));
        delivery.Body.Should().Contain("vin.decoded");
    }

    [Test]
    public async Task Usage_Limit_Should_Throttle_Without_Deleting_Data()
    {
        var harness = Harness.Create();
        var tenant = await harness.ProvisionAsync("capped");
        await harness.Settings.UpsertAsync(new TenantSetting
        {
            TenantId = tenant.Id,
            Key = TenantUsageLimitService.SettingKeyPrefix + PublicApiScopes.VinDecode,
            Value = "1"
        }, CancellationToken.None);
        await harness.Usage.RecordAsync(new TenantUsageEvent
        {
            TenantId = tenant.Id,
            Metric = PublicApiScopes.VinDecode,
            Quantity = 1m,
            OccurredUtc = System.DateTimeOffset.UtcNow
        }, CancellationToken.None);

        (await harness.Limits.IsLimitedAsync(tenant.Id, PublicApiScopes.VinDecode, CancellationToken.None)).Should().BeTrue();
        (await harness.Limits.IsLimitedAsync(tenant.Id, PublicApiScopes.Search, CancellationToken.None)).Should().BeFalse();
        (await harness.Usage.ListDailyAsync(tenant.Id, System.DateTime.UtcNow.Date, System.DateTime.UtcNow.Date, CancellationToken.None))
            .Should().ContainSingle(row => row.Quantity == 1m);
    }

    [Test]
    public void Billing_Adapter_Should_Export_From_Usage_Port_Without_Vendor()
    {
        typeof(ITenantBillingAdapter).Assembly.GetName().Name.Should().Be("TwinParticles.CheckEngine.Domain");
        typeof(UsageLedgerBillingAdapter).Assembly.GetName().Name.Should().Be("TwinParticles.CheckEngine.Application");
    }

    private sealed class Harness
    {
        public required TenantWebhookService Webhooks { get; init; }
        public required TenantUsageLimitService Limits { get; init; }
        public required TenantRegistryService RegistryService { get; init; }
        public required RecordingDelivery Delivery { get; init; }
        public required InMemorySettingsStore Settings { get; init; }
        public required PublicApiKeyServiceTests.InMemoryUsageLedger Usage { get; init; }

        public static Harness Create()
        {
            var registry = new TenantIsolationServiceTests.InMemoryTenantRegistry();
            var accessor = new TwinParticles.CheckEngine.Infrastructure.Tenancy.TenantContextAccessor();
            var registryService = new TenantRegistryService(registry, accessor);
            var isolation = new TenantIsolationService(new InMemoryCheckEngineAuditService());
            var store = new InMemoryWebhookStore();
            var delivery = new RecordingDelivery();
            var usage = new PublicApiKeyServiceTests.InMemoryUsageLedger();
            var settings = new InMemorySettingsStore();
            return new Harness
            {
                Webhooks = new TenantWebhookService(store, new IdentityProtector(), delivery, isolation),
                Limits = new TenantUsageLimitService(settings, usage),
                RegistryService = registryService,
                Delivery = delivery,
                Settings = settings,
                Usage = usage
            };
        }

        public async Task<Tenant> ProvisionAsync(string slug)
        {
            var result = await RegistryService.ProvisionAsync(TenantActor.ControlPlaneOperator, slug, slug, null, null, CancellationToken.None);
            result.Success.Should().BeTrue();
            return result.Tenant!;
        }
    }

    private sealed class IdentityProtector : ITenantWebhookSecretProtector
    {
        public string? Protect(string? plaintext) => plaintext;
        public string? Unprotect(string? storedValue) => storedValue;
    }

    private sealed class RecordingDelivery : ITenantWebhookDeliveryPort
    {
        public List<TenantWebhookDelivery> Deliveries { get; } = [];

        public Task<bool> DeliverAsync(TenantWebhookDelivery delivery, CancellationToken cancellationToken)
        {
            Deliveries.Add(delivery);
            return Task.FromResult(true);
        }
    }

    private sealed class InMemoryWebhookStore : ITenantWebhookStore
    {
        private readonly ConcurrentDictionary<int, TenantWebhookSubscription> _rows = new();
        private int _nextId = 1;

        public Task<int> InsertAsync(TenantWebhookSubscription subscription, CancellationToken cancellationToken)
        {
            subscription.Id = Interlocked.Increment(ref _nextId) - 1;
            _rows[subscription.Id] = subscription;
            return Task.FromResult(subscription.Id);
        }

        public Task<IReadOnlyList<TenantWebhookSubscription>> ListByTenantAsync(int tenantId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<TenantWebhookSubscription>>(_rows.Values.Where(row => row.TenantId == tenantId).ToList());

        public Task SetActiveAsync(int subscriptionId, bool isActive, CancellationToken cancellationToken)
        {
            if (_rows.TryGetValue(subscriptionId, out var row))
                row.IsActive = isActive;
            return Task.CompletedTask;
        }
    }

    internal sealed class InMemorySettingsStore : ITenantSettingsStore
    {
        private readonly ConcurrentDictionary<(int TenantId, string Key), string> _values = new();

        public Task<string?> GetAsync(int tenantId, string key, CancellationToken cancellationToken)
            => Task.FromResult(_values.TryGetValue((tenantId, key), out var value) ? value : null);

        public Task UpsertAsync(TenantSetting setting, CancellationToken cancellationToken)
        {
            _values[(setting.TenantId, setting.Key)] = setting.Value;
            return Task.CompletedTask;
        }
    }
}
