using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Garage;
using TwinParticles.CheckEngine.Application.Privacy;
using TwinParticles.CheckEngine.Domain.Garage;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class CheckEngineSubjectExportTests
{
    [Test]
    public async Task Subject_Export_Should_Aggregate_Garage_And_Declare_Retention_Holds()
    {
        var repository = new FakeGarageRepository();
        var audit = new RecordingAudit();
        var service = new CheckEngineSubjectDataService(new GaragePrivacyService(repository, audit), audit);

        var export = await service.ExportAsync(42, "customer:42", CancellationToken.None);

        export.CustomerId.Should().Be(42);
        export.Garage.Vehicles.Should().ContainSingle(v => v.Vin == "WBA-PLAINTEXT");
        export.RetentionHolds.Should().Contain(h => h.DataClass == "order-history");
        export.RetentionHolds.Should().Contain(h => h.DataClass == "erp-financial-sync");
        export.RetentionHolds.Should().Contain(h => h.DataClass == "audit-trail");

        audit.Actions.Should().Contain("privacy.subject_export");
        // The subject export must not leak the VIN into the audit trail.
        audit.Payloads.Should().NotContain(p => p != null && p.Contains("WBA-PLAINTEXT"));
    }

    private sealed class FakeGarageRepository : IGarageRepository
    {
        public Task<Garage?> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken)
            => Task.FromResult<Garage?>(new Garage
            {
                CustomerId = customerId,
                Vehicles =
                [
                    new GarageVehicle { Id = 1, VehicleConfigurationId = 10, Vin = "WBA-PLAINTEXT", Label = "My BMW", IsActive = true }
                ],
                Oems =
                [
                    new GarageOem { Id = 1, OemNumberId = 5, DisplayNumber = "11-51-7-586-925" }
                ]
            });

        public Task<Garage> GetOrCreateAsync(int customerId, CancellationToken cancellationToken)
            => Task.FromResult(new Garage { CustomerId = customerId });
        public Task SaveAsync(Garage garage, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> DeleteByCustomerIdAsync(int customerId, CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class RecordingAudit : ICheckEngineAuditService
    {
        public List<string> Actions { get; } = [];
        public List<string?> Payloads { get; } = [];

        public Task AppendAsync(string actor, string action, string entityType, string entityId,
            string? beforeJson, string? afterJson, CancellationToken cancellationToken = default)
        {
            Actions.Add(action);
            Payloads.Add(afterJson);
            return Task.CompletedTask;
        }
    }
}
