using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Erp;
using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ErpSyncServiceTests
{
    [Test]
    public async Task QueueSyncAsync_Should_Create_Idempotency_Key()
    {
        var queue = new InMemoryQueue();
        var service = new ErpSyncService(queue, new StubClient(), new StubConflictResolver());

        var jobId = await service.QueueSyncAsync(ErpSyncEntityType.Product, ErpSyncDirection.Bidirectional, "SKU-100", "{ }", CancellationToken.None);

        var job = await queue.GetByIdAsync(jobId, CancellationToken.None);
        job.Should().NotBeNull();
        job!.IdempotencyKey.Should().Be("ce:Product:Bidirectional:sku-100");
    }

    [Test]
    public async Task ProcessPendingAsync_Should_Retry_With_Auto_Resolution_On_Failure()
    {
        var queue = new InMemoryQueue();
        var client = new StubClient();
        var service = new ErpSyncService(queue, client, new StubConflictResolver());

        await service.QueueSyncAsync(ErpSyncEntityType.Order, ErpSyncDirection.PushToErp, "500", "force-fail", CancellationToken.None);

        var successCount = await service.ProcessPendingAsync(CancellationToken.None);

        successCount.Should().Be(1);
        var report = await service.BuildReconciliationReportAsync(CancellationToken.None);
        report.FailedJobs.Should().Be(0);
    }

    private sealed class InMemoryQueue : IErpSyncQueueRepository
    {
        private readonly System.Collections.Generic.List<ErpSyncJob> _jobs = [];

        public Task EnqueueAsync(ErpSyncJob job, CancellationToken cancellationToken)
        {
            _jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task<System.Collections.Generic.IReadOnlyList<ErpSyncJob>> GetPendingAsync(CancellationToken cancellationToken)
            => Task.FromResult<System.Collections.Generic.IReadOnlyList<ErpSyncJob>>(_jobs.FindAll(x => x.Status == "Queued"));

        public Task<ErpSyncJob?> GetByIdAsync(System.Guid jobId, CancellationToken cancellationToken)
            => Task.FromResult(_jobs.Find(x => x.JobId == jobId));

        public Task UpdateAsync(ErpSyncJob job, CancellationToken cancellationToken)
        {
            _jobs.RemoveAll(x => x.JobId == job.JobId);
            _jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task<System.Collections.Generic.IReadOnlyList<ErpSyncJob>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<System.Collections.Generic.IReadOnlyList<ErpSyncJob>>(_jobs);
    }

    private sealed class StubClient : IErpClientAdapter
    {
        public Task<bool> PushAsync(ErpSyncJob job, CancellationToken cancellationToken)
        {
            return Task.FromResult(!job.Payload.Contains("force-fail"));
        }

        public Task<string?> PullInventorySnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>("{\"items\":[]}");
        }
    }

    private sealed class StubConflictResolver : IErpConflictResolutionService
    {
        public bool CanAutoResolve(ErpSyncJob job) => true;

        public void ApplyAutoResolution(ErpSyncJob job)
        {
            job.Payload = "resolved";
            job.ConflictCode = "erp.auto_retry";
        }
    }
}
