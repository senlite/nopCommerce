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
using TwinParticles.CheckEngine.Infrastructure.Tenancy;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class TenantIsolationServiceTests
{
    [Test]
    public async Task Tenant_A_Should_Be_Denied_Access_To_Tenant_B()
    {
        var harness = Harness.Create();
        var denied = await harness.Isolation.AuthorizeAsync(TenantActor.ForTenant(1), 2, write: true, CancellationToken.None);
        var allowed = await harness.Isolation.AuthorizeAsync(TenantActor.ForTenant(2), 2, write: true, CancellationToken.None);

        denied.Allowed.Should().BeFalse();
        denied.ReasonCode.Should().Be(TenantErrorCodes.IsolationDenied);
        allowed.Allowed.Should().BeTrue();
        harness.Audit.Snapshot().Should().Contain(entry =>
            entry.Action == "tenant.isolation.write_denied" && entry.EntityId == "2");
    }

    [Test]
    public async Task Control_Plane_Operator_Should_Bypass_Isolation_With_Audit()
    {
        var harness = Harness.Create();
        var decision = await harness.Isolation.AuthorizeAsync(TenantActor.ControlPlaneOperator, 9, write: true, CancellationToken.None);

        decision.Allowed.Should().BeTrue();
        harness.Audit.Snapshot().Should().Contain(entry =>
            entry.Action == "tenant.isolation.elevated_write" && entry.EntityId == "9");
    }

    [Test]
    public async Task Anonymous_Actor_Should_Be_Denied()
    {
        var harness = Harness.Create();
        var decision = await harness.Isolation.AuthorizeAsync(TenantActor.Anonymous, 1, write: false, CancellationToken.None);

        decision.ReasonCode.Should().Be(TenantErrorCodes.IsolationUnauthenticated);
    }

    [Test]
    public void Connection_Router_Should_Refuse_Cross_Tenant_Routing()
    {
        var router = new TenantConnectionRouter();
        var tenantB = new Tenant { Id = 2, ConnectionName = "tenant-b" };
        var contextA = new TenantContext { TenantId = 1, ConnectionName = "tenant-a" };

        var denied = router.Resolve(tenantB, contextA);
        var allowed = router.Resolve(tenantB, new TenantContext { TenantId = 2, ConnectionName = "tenant-b" });
        var control = router.Resolve(tenantB, new TenantContext { TenantId = 0, IsControlPlane = true });

        denied.Allowed.Should().BeFalse();
        denied.ReasonCode.Should().Be(TenantErrorCodes.CrossConnectionDenied);
        allowed.Allowed.Should().BeTrue();
        allowed.ConnectionName.Should().Be("tenant-b");
        control.Allowed.Should().BeTrue();
    }

    [Test]
    public async Task Background_Jobs_Should_Not_Enumerate_Other_Tenants()
    {
        var harness = Harness.Create();
        await harness.Registry.InsertAsync(new Tenant { Slug = "a", Status = TenantStatus.Active }, CancellationToken.None);
        await harness.Registry.InsertAsync(new Tenant { Slug = "b", Status = TenantStatus.Active }, CancellationToken.None);

        var scoped = await harness.Jobs.ListTenantIdsForJobAsync(TenantActor.ForTenant(1), CancellationToken.None);
        var all = await harness.Jobs.ListTenantIdsForJobAsync(TenantActor.ControlPlaneOperator, CancellationToken.None);

        scoped.Should().Equal(1);
        all.Should().BeEquivalentTo([1, 2]);
    }

    [Test]
    public void Cache_Keys_Should_Not_Collide_Across_Tenants()
    {
        var left = TenantCacheKey.Qualify(1, "fitment:10:20");
        var right = TenantCacheKey.Qualify(2, "fitment:10:20");

        left.Should().NotBe(right);
        TenantCacheKey.SharesTenant(left, right).Should().BeFalse();
        TenantCacheKey.SharesTenant(left, TenantCacheKey.Qualify(1, "other")).Should().BeTrue();
    }

    [Test]
    public async Task Fitment_Cache_Should_Isolate_Tenant_Verdicts()
    {
        var accessor = new TenantContextAccessor();
        var cache = new TenantAwareFitmentCache(accessor);
        var context = new TwinParticles.CheckEngine.Domain.Fitment.FitmentEvaluationContext
        {
            ProductId = 10,
            VehicleConfigurationId = 20
        };

        accessor.SetCurrent(new TenantContext { TenantId = 1 });
        await cache.SetAsync(context, new TwinParticles.CheckEngine.Domain.Fitment.FitmentEvaluationResult
        {
            Outcome = TwinParticles.CheckEngine.Domain.Fitment.FitmentStatus.Fits,
            EffectiveConfidence = 0.9m
        }, CancellationToken.None);

        accessor.SetCurrent(new TenantContext { TenantId = 2 });
        var leaked = await cache.GetAsync(context, CancellationToken.None);
        leaked.Should().BeNull();

        accessor.SetCurrent(new TenantContext { TenantId = 1 });
        var own = await cache.GetAsync(context, CancellationToken.None);
        own.Should().NotBeNull();
        own!.EffectiveConfidence.Should().Be(0.9m);
        cache.GetKey(context).Should().StartWith("ce:t1:");
    }

    [Test]
    public async Task Self_Hosted_Tenant_Should_Be_Created_Once()
    {
        var harness = Harness.Create();
        var first = await harness.RegistryService.EnsureSelfHostedAsync(CancellationToken.None);
        var second = await harness.RegistryService.EnsureSelfHostedAsync(CancellationToken.None);

        first.Slug.Should().Be(SelfHostedTenant.Slug);
        first.IsSelfHosted.Should().BeTrue();
        second.Id.Should().Be(first.Id);
        (await harness.Registry.ListAsync(CancellationToken.None)).Should().HaveCount(1);
    }

    [Test]
    public async Task Resolver_Should_Bind_Hostname_And_Fallback_To_Self_Hosted()
    {
        var harness = Harness.Create();
        var hosted = await harness.RegistryService.ProvisionAsync(
            TenantActor.ControlPlaneOperator, "acme-host", "Acme", "acme.example", null, CancellationToken.None);
        hosted.Success.Should().BeTrue();

        var resolver = new TenantResolver(harness.Registry, harness.RegistryService, harness.Accessor);
        var resolved = await resolver.ResolveAsync(new TenantResolutionRequest { Hostname = "acme.example" }, CancellationToken.None);
        resolved!.Slug.Should().Be("acme-host");
        resolved.Id.Should().Be(hosted.Tenant!.Id);

        var self = await resolver.ResolveAsync(new TenantResolutionRequest { Hostname = "unknown.example" }, CancellationToken.None);
        self!.Slug.Should().Be(SelfHostedTenant.Slug);
        self.IsSelfHosted.Should().BeTrue();
    }

    [Test]
    public void Bind_Should_Publish_Tenant_Context()
    {
        var accessor = new TenantContextAccessor();
        var service = new TenantRegistryService(new InMemoryTenantRegistry(), accessor);
        service.Bind(new Tenant { Id = 7, Slug = "sync" });

        accessor.Current.TenantId.Should().Be(7);
        accessor.Current.Slug.Should().Be("sync");
    }

    private sealed class Harness
    {
        public required TenantIsolationService Isolation { get; init; }
        public required InMemoryCheckEngineAuditService Audit { get; init; }
        public required InMemoryTenantRegistry Registry { get; init; }
        public required TenantJobScope Jobs { get; init; }
        public required TenantRegistryService RegistryService { get; init; }
        public required TenantContextAccessor Accessor { get; init; }

        public static Harness Create()
        {
            var audit = new InMemoryCheckEngineAuditService();
            var registry = new InMemoryTenantRegistry();
            var accessor = new TenantContextAccessor();
            return new Harness
            {
                Isolation = new TenantIsolationService(audit),
                Audit = audit,
                Registry = registry,
                Jobs = new TenantJobScope(registry),
                RegistryService = new TenantRegistryService(registry, accessor),
                Accessor = accessor
            };
        }
    }

    internal sealed class InMemoryTenantRegistry : ITenantRegistry
    {
        private readonly ConcurrentDictionary<int, Tenant> _tenants = new();
        private int _nextId = 1;

        public Task<Tenant?> GetByIdAsync(int tenantId, CancellationToken cancellationToken)
            => Task.FromResult(_tenants.TryGetValue(tenantId, out var tenant) ? tenant : null);

        public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
            => Task.FromResult(_tenants.Values.FirstOrDefault(t => t.Slug == slug));

        public Task<Tenant?> GetByHostnameAsync(string hostname, CancellationToken cancellationToken)
            => Task.FromResult(_tenants.Values.FirstOrDefault(t =>
                t.HostnamesCsv.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                    .Contains(hostname, System.StringComparer.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Tenant>>(_tenants.Values.OrderBy(t => t.Id).ToList());

        public Task<int> InsertAsync(Tenant tenant, CancellationToken cancellationToken)
        {
            tenant.Id = Interlocked.Increment(ref _nextId) - 1;
            _tenants[tenant.Id] = tenant;
            return Task.FromResult(tenant.Id);
        }

        public Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken)
        {
            _tenants[tenant.Id] = tenant;
            return Task.CompletedTask;
        }
    }
}
