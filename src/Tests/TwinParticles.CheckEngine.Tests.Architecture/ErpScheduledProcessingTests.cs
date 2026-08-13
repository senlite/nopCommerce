using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Erp;
using TwinParticles.CheckEngine.Domain.Erp;
using TwinParticles.CheckEngine.Infrastructure.Erp;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ErpScheduledProcessingTests
{
    [Test]
    public async Task Queue_Should_Idempotently_Collapse_Duplicate_Host_Event_Delivery()
    {
        var queue = new InMemoryErpSyncQueueRepository();
        var service = CreateService(queue);

        var first = await service.QueueSyncAsync(ErpSyncEntityType.Order, ErpSyncDirection.PushToErp,
            "placed:500", "{\"orderId\":500}", CancellationToken.None);
        var duplicate = await service.QueueSyncAsync(ErpSyncEntityType.Order, ErpSyncDirection.PushToErp,
            "placed:500", "{\"orderId\":500}", CancellationToken.None);

        var jobs = await queue.GetAllAsync(CancellationToken.None);
        jobs.Should().ContainSingle("duplicate host event delivery must not create duplicate ERP work");
        duplicate.Should().Be(first, "idempotent enqueue returns the existing durable job id");
    }

    [Test]
    public async Task Scheduled_Processing_Should_Process_Claimed_Queue_Jobs()
    {
        var queue = new InMemoryErpSyncQueueRepository();
        var service = CreateService(queue);
        await service.QueueSyncAsync(
            ErpSyncEntityType.Order,
            ErpSyncDirection.PushToErp,
            "placed:501",
            "{\"orderId\":501}",
            CancellationToken.None);

        await service.ProcessPendingAsync(CancellationToken.None);

        var job = (await queue.GetAllAsync(CancellationToken.None)).Single();
        job.Status.Should().Be("Succeeded");
        job.AttemptCount.Should().Be(1);
        job.LastAttemptUtc.Should().NotBeNull();
    }

    [Test]
    public async Task Failed_Jobs_Should_Use_Bounded_Delayed_Retries()
    {
        var queue = new InMemoryErpSyncQueueRepository();
        var service = new ErpSyncService(queue, new AlwaysFailAdapter(), new NeverResolve());
        var id = await service.QueueSyncAsync(
            ErpSyncEntityType.Order,
            ErpSyncDirection.PushToErp,
            "placed:502",
            "{}",
            CancellationToken.None);

        await service.ProcessPendingAsync(CancellationToken.None);
        var job = await queue.GetByIdAsync(id, CancellationToken.None);
        job!.Status.Should().Be("Queued");
        job.ConflictCode.Should().Be("erp.retry_scheduled");
        job.NextAttemptUtc.Should().BeAfter(System.DateTime.UtcNow);

        // Make each delayed attempt eligible without waiting for wall-clock time.
        for (var attempt = 2; attempt <= 5; attempt++)
        {
            job.NextAttemptUtc = System.DateTime.UtcNow.AddSeconds(-1);
            await service.ProcessPendingAsync(CancellationToken.None);
        }

        job.Status.Should().Be("Failed");
        job.AttemptCount.Should().Be(5);
        job.NextAttemptUtc.Should().BeNull();
    }

    [Test]
    public void Plugin_Should_Wire_Order_Customer_Consumers_And_Schedule_Task_Without_Pii()
    {
        var consumer = ReadPluginFile("Consumers", "ErpCommerceEventConsumer.cs");
        var task = ReadPluginFile("Tasks", "ErpSyncQueueTask.cs");
        var plugin = ReadPluginFile("CheckEnginePlugin.cs");

        consumer.Should().Contain("IConsumer<OrderPlacedEvent>");
        consumer.Should().Contain("IConsumer<CustomerRegisteredEvent>");
        consumer.Should().Contain("ErpSyncEntityType.Order");
        consumer.Should().Contain("ErpSyncEntityType.Customer");
        consumer.Should().NotContain("customer.Email");
        task.Should().Contain("IScheduleTask");
        task.Should().Contain("ProcessPendingAsync");
        plugin.Should().Contain("EnsureScheduleTasksAsync");
        plugin.Should().Contain("GetTaskByTypeAsync(typeof(Tasks.ErpSyncQueueTask).FullName!)");
    }

    private static ErpSyncService CreateService(IErpSyncQueueRepository queue)
        => new(queue, new StubErpClientAdapter(), new DefaultErpConflictResolutionService());

    private static string ReadPluginFile(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }

    private sealed class AlwaysFailAdapter : IErpClientAdapter
    {
        public Task<bool> PushAsync(ErpSyncJob job, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<string?> PullInventorySnapshotAsync(CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }

    private sealed class NeverResolve : IErpConflictResolutionService
    {
        public bool CanAutoResolve(ErpSyncJob job) => false;
        public void ApplyAutoResolution(ErpSyncJob job) { }
    }
}
