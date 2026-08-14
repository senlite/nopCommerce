using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Erp;
using TwinParticles.CheckEngine.Infrastructure.Erp;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ErpNextHttpClientAdapterTests
{
    [Test]
    public async Task PushAsync_With_Empty_BaseUrl_Should_Use_Stub_Success_Path()
    {
        var previous = ErpConnectionOptions.Current;
        try
        {
            ErpConnectionOptions.Current = new ErpConnectionOptions
            {
                BaseUrl = string.Empty,
                ApiKey = string.Empty,
                ApiSecret = string.Empty
            };

            var adapter = new ErpNextHttpClientAdapter(ErpConnectionOptions.Current);

            var success = await adapter.PushAsync(new ErpSyncJob
            {
                JobId = Guid.NewGuid(),
                EntityType = ErpSyncEntityType.Product,
                Direction = ErpSyncDirection.PushToErp,
                Payload = "{\"item_code\":\"SKU-1\"}"
            }, CancellationToken.None);

            success.Should().BeTrue();
        }
        finally
        {
            ErpConnectionOptions.Current = previous;
        }
    }

    [Test]
    public async Task PushAsync_With_Empty_BaseUrl_Should_Honor_Stub_Force_Fail()
    {
        var adapter = new ErpNextHttpClientAdapter(new ErpConnectionOptions { BaseUrl = string.Empty });

        var success = await adapter.PushAsync(new ErpSyncJob
        {
            JobId = Guid.NewGuid(),
            EntityType = ErpSyncEntityType.Order,
            Direction = ErpSyncDirection.PushToErp,
            Payload = "force-fail"
        }, CancellationToken.None);

        success.Should().BeFalse();
    }

    [Test]
    public async Task PullInventorySnapshotAsync_With_Empty_BaseUrl_Should_Return_Stub_Snapshot()
    {
        var adapter = new ErpNextHttpClientAdapter(new ErpConnectionOptions { BaseUrl = "   " });

        var snapshot = await adapter.PullInventorySnapshotAsync(CancellationToken.None);

        snapshot.Should().NotBeNullOrWhiteSpace();
        snapshot.Should().Contain("warehouse");
    }
}
