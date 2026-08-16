using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Erp;
using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ErpReconciliationTests
{
    [Test]
    public async Task Reconciliation_Should_Report_No_Discrepancy_When_Totals_Match_Within_Tolerance()
    {
        var local = new ErpReconciliationTotals { OrderCount = 12, PaymentTotal = 3400.00m, InventoryUnits = 900 };
        var erp = new ErpReconciliationTotals { OrderCount = 12, PaymentTotal = 3400.005m, InventoryUnits = 900 };
        var service = new ErpSyncService(new EmptyQueue(), new StubClient(), new NoResolve(), new FakeDataSource(local, erp));

        var report = await service.BuildReconciliationReportAsync(
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, CancellationToken.None);

        report.Variances.Should().HaveCount(3);
        report.HasFinancialDiscrepancy.Should().BeFalse("cent-level rounding is within the payment tolerance");
        report.WindowFromUtc.Should().NotBeNull();
    }

    [Test]
    public async Task Reconciliation_Should_Flag_Payment_And_Count_Discrepancies()
    {
        var local = new ErpReconciliationTotals { OrderCount = 12, PaymentTotal = 3400.00m, InventoryUnits = 900 };
        var erp = new ErpReconciliationTotals { OrderCount = 11, PaymentTotal = 3200.00m, InventoryUnits = 900 };
        var service = new ErpSyncService(new EmptyQueue(), new StubClient(), new NoResolve(), new FakeDataSource(local, erp));

        var report = await service.BuildReconciliationReportAsync(
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, CancellationToken.None);

        report.HasFinancialDiscrepancy.Should().BeTrue();
        report.Variances.Single(v => v.Metric == "payment.total").AbsoluteVariance.Should().Be(200.00m);
        report.Variances.Single(v => v.Metric == "order.count").WithinTolerance.Should().BeFalse();
        report.Variances.Single(v => v.Metric == "inventory.units").WithinTolerance.Should().BeTrue();
    }

    [Test]
    public async Task Reconciliation_Should_Not_Fabricate_Match_When_Erp_Unavailable()
    {
        var local = new ErpReconciliationTotals { OrderCount = 5, PaymentTotal = 100m, InventoryUnits = 10 };
        var service = new ErpSyncService(new EmptyQueue(), new StubClient(), new NoResolve(),
            new FakeDataSource(local, ErpReconciliationTotals.Unavailable));

        var report = await service.BuildReconciliationReportAsync(
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, CancellationToken.None);

        report.Variances.Should().BeEmpty("no comparison is possible without both sides");
        report.HasFinancialDiscrepancy.Should().BeFalse();
        report.Issues.Should().Contain("reconciliation:erp_unavailable");
    }

    [Test]
    public async Task Reconciliation_Without_DataSource_Should_Still_Return_Queue_Summary()
    {
        var service = new ErpSyncService(new EmptyQueue(), new StubClient(), new NoResolve());

        var report = await service.BuildReconciliationReportAsync(CancellationToken.None);

        report.Variances.Should().BeEmpty();
        report.HasFinancialDiscrepancy.Should().BeFalse();
    }

    private sealed class FakeDataSource : IErpReconciliationDataSource
    {
        private readonly ErpReconciliationTotals _local;
        private readonly ErpReconciliationTotals _erp;

        public FakeDataSource(ErpReconciliationTotals local, ErpReconciliationTotals erp)
        {
            _local = local;
            _erp = erp;
        }

        public Task<ErpReconciliationTotals> GetLocalTotalsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
            => Task.FromResult(_local);

        public Task<ErpReconciliationTotals> GetErpTotalsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
            => Task.FromResult(_erp);
    }

    private sealed class EmptyQueue : IErpSyncQueueRepository
    {
        public Task<Guid> EnqueueAsync(ErpSyncJob job, CancellationToken cancellationToken) => Task.FromResult(job.JobId);
        public Task<IReadOnlyList<ErpSyncJob>> GetPendingAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ErpSyncJob>>([]);
        public Task<ErpSyncJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken) => Task.FromResult<ErpSyncJob?>(null);
        public Task UpdateAsync(ErpSyncJob job, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<ErpSyncJob>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ErpSyncJob>>([]);
    }

    private sealed class StubClient : IErpClientAdapter
    {
        public Task<bool> PushAsync(ErpSyncJob job, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> PullAsync(ErpSyncJob job, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<string?> PullInventorySnapshotAsync(CancellationToken cancellationToken) => Task.FromResult<string?>("{}");
    }

    private sealed class NoResolve : IErpConflictResolutionService
    {
        public bool CanAutoResolve(ErpSyncJob job) => false;
        public void ApplyAutoResolution(ErpSyncJob job) { }
    }
}
