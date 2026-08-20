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
public class PublicApiKeyServiceTests
{
    [Test]
    public async Task Issued_Key_Should_Authenticate_And_Bind_Tenant()
    {
        var harness = Harness.Create();
        var tenant = await harness.ProvisionAsync("acme");

        var issued = await harness.Keys.IssueAsync(TenantActor.ControlPlaneOperator, tenant.Id, PublicApiScopes.DefaultCsv, CancellationToken.None);
        issued.Should().NotBeNull();
        issued!.Plaintext.Should().StartWith(PublicApiKeyService.KeyPrefix);

        var auth = await harness.Keys.AuthenticateAsync(issued.Plaintext, CancellationToken.None);
        auth.Success.Should().BeTrue();
        auth.Tenant!.Id.Should().Be(tenant.Id);
        harness.Accessor.Current.TenantId.Should().Be(tenant.Id);
        PublicApiKeyService.HasScope(auth.ApiKey!, PublicApiScopes.FitmentEvaluate).Should().BeTrue();
    }

    [Test]
    public async Task Missing_Or_Unknown_Key_Should_Fail_Closed()
    {
        var harness = Harness.Create();
        (await harness.Keys.AuthenticateAsync(null, CancellationToken.None)).ReasonCode.Should().Be(TenantErrorCodes.PublicApiUnauthenticated);
        (await harness.Keys.AuthenticateAsync("cek_live_deadbeef", CancellationToken.None)).ReasonCode.Should().Be(TenantErrorCodes.PublicApiKeyInvalid);
    }

    [Test]
    public async Task Suspended_Tenant_Key_Should_Be_Rejected()
    {
        var harness = Harness.Create();
        var tenant = await harness.ProvisionAsync("paused");
        var issued = await harness.Keys.IssueAsync(TenantActor.ControlPlaneOperator, tenant.Id, null, CancellationToken.None);
        await harness.RegistryService.SetStatusAsync(TenantActor.ControlPlaneOperator, tenant.Id, TenantStatus.Suspended, CancellationToken.None);

        var auth = await harness.Keys.AuthenticateAsync(issued!.Plaintext, CancellationToken.None);
        auth.Success.Should().BeFalse();
        auth.ReasonCode.Should().Be(TenantErrorCodes.TenantSuspended);
    }

    [Test]
    public async Task Tenant_Cannot_Issue_Keys_For_Another_Tenant()
    {
        var harness = Harness.Create();
        var tenant = await harness.ProvisionAsync("locked");
        var issued = await harness.Keys.IssueAsync(TenantActor.ForTenant(999), tenant.Id, null, CancellationToken.None);
        issued.Should().BeNull();
    }

    [Test]
    public async Task Usage_Ledger_Should_Stay_Tenant_Scoped()
    {
        var harness = Harness.Create();
        await harness.Usage.RecordAsync(new TenantUsageEvent { TenantId = 1, Metric = "vin.decode", Quantity = 2m, OccurredUtc = System.DateTimeOffset.UtcNow }, CancellationToken.None);
        await harness.Usage.RecordAsync(new TenantUsageEvent { TenantId = 2, Metric = "vin.decode", Quantity = 9m, OccurredUtc = System.DateTimeOffset.UtcNow }, CancellationToken.None);

        var rows = await harness.Usage.ListDailyAsync(1, System.DateTime.UtcNow.Date.AddDays(-1), System.DateTime.UtcNow.Date, CancellationToken.None);
        rows.Should().ContainSingle();
        rows[0].Quantity.Should().Be(2m);
        rows[0].TenantId.Should().Be(1);
    }

    private sealed class Harness
    {
        public required PublicApiKeyService Keys { get; init; }
        public required TenantRegistryService RegistryService { get; init; }
        public required TwinParticles.CheckEngine.Infrastructure.Tenancy.TenantContextAccessor Accessor { get; init; }
        public required InMemoryUsageLedger Usage { get; init; }

        public static Harness Create()
        {
            var registry = new TenantIsolationServiceTests.InMemoryTenantRegistry();
            var accessor = new TwinParticles.CheckEngine.Infrastructure.Tenancy.TenantContextAccessor();
            var registryService = new TenantRegistryService(registry, accessor);
            var isolation = new TenantIsolationService(new InMemoryCheckEngineAuditService());
            var keys = new PublicApiKeyService(new InMemoryApiKeyStore(), registry, registryService, isolation);
            return new Harness
            {
                Keys = keys,
                RegistryService = registryService,
                Accessor = accessor,
                Usage = new InMemoryUsageLedger()
            };
        }

        public async Task<Tenant> ProvisionAsync(string slug)
        {
            var result = await RegistryService.ProvisionAsync(TenantActor.ControlPlaneOperator, slug, slug, null, null, CancellationToken.None);
            result.Success.Should().BeTrue();
            return result.Tenant!;
        }
    }

    private sealed class InMemoryApiKeyStore : ITenantApiKeyStore
    {
        private readonly ConcurrentDictionary<int, TenantApiKey> _keys = new();
        private int _nextId = 1;

        public Task<int> InsertAsync(TenantApiKey key, CancellationToken cancellationToken)
        {
            key.Id = Interlocked.Increment(ref _nextId) - 1;
            _keys[key.Id] = key;
            return Task.FromResult(key.Id);
        }

        public Task<TenantApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken)
            => Task.FromResult(_keys.Values.FirstOrDefault(k => k.KeyHash == keyHash));

        public Task<IReadOnlyList<TenantApiKey>> ListByTenantAsync(int tenantId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<TenantApiKey>>(_keys.Values.Where(k => k.TenantId == tenantId).ToList());

        public Task SetActiveAsync(int keyId, bool isActive, CancellationToken cancellationToken)
        {
            if (_keys.TryGetValue(keyId, out var key))
                key.IsActive = isActive;
            return Task.CompletedTask;
        }

        public Task TouchLastUsedAsync(int keyId, System.DateTimeOffset utc, CancellationToken cancellationToken)
        {
            if (_keys.TryGetValue(keyId, out var key))
                key.LastUsedUtc = utc;
            return Task.CompletedTask;
        }
    }

    internal sealed class InMemoryUsageLedger : ITenantUsageLedger
    {
        private readonly ConcurrentBag<TenantUsageEvent> _events = [];

        public Task RecordAsync(TenantUsageEvent usageEvent, CancellationToken cancellationToken)
        {
            _events.Add(usageEvent);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TenantUsageDaily>> ListDailyAsync(int tenantId, System.DateTime fromUtc, System.DateTime toUtc, CancellationToken cancellationToken)
        {
            var rows = _events
                .Where(e => e.TenantId == tenantId)
                .GroupBy(e => (e.Metric, e.OccurredUtc.UtcDateTime.Date))
                .Select(g => new TenantUsageDaily
                {
                    TenantId = tenantId,
                    Metric = g.Key.Metric,
                    DayUtc = g.Key.Date,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();
            return Task.FromResult<IReadOnlyList<TenantUsageDaily>>(rows);
        }
    }
}
