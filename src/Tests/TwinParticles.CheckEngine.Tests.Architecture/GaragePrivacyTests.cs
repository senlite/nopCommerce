using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Nop.Services.Security;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Garage;
using TwinParticles.CheckEngine.Domain.Garage;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Infrastructure.Garage;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class GaragePrivacyTests
{
    [Test]
    public void Vin_Protector_Should_Encrypt_At_Rest_And_Round_Trip()
    {
        var protector = new NopGarageVinProtector(new FakeEncryptionService());

        var stored = protector.Protect("wba3a5c50df123456");

        stored.Should().StartWith("enc:v1:");
        stored.Should().NotContain("WBA3A5C50DF123456");
        protector.Unprotect(stored).Should().Be("WBA3A5C50DF123456");
    }

    [Test]
    public void Vin_Protector_Should_Read_Legacy_Plaintext_For_Opportunistic_Migration()
    {
        var protector = new NopGarageVinProtector(new FakeEncryptionService());

        protector.Unprotect("wba3a5c50df123456").Should().Be("WBA3A5C50DF123456");
    }

    [Test]
    public async Task Export_Should_Include_Garage_Data_But_Audit_No_Vin()
    {
        var repository = new InMemoryGarageRepository();
        var garage = await repository.GetOrCreateAsync(77, CancellationToken.None);
        garage.Vehicles.Add(new GarageVehicle
        {
            VehicleConfigurationId = 501,
            Vin = "WBA3A5C50DF123456",
            Label = "My BMW",
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        });
        garage.Oems.Add(new GarageOem
        {
            OemNumberId = 9,
            DisplayNumber = "11-51-7-586-925",
            CreatedUtc = DateTime.UtcNow
        });
        await repository.SaveAsync(garage, CancellationToken.None);
        var audit = new RecordingAudit();
        var service = new GaragePrivacyService(repository, audit);

        var export = await service.ExportAsync(77, "customer:77", CancellationToken.None);

        export.Vehicles.Should().ContainSingle(vehicle =>
            vehicle.Vin == "WBA3A5C50DF123456" &&
            vehicle.VehicleConfigurationId == 501);
        export.Oems.Should().ContainSingle(oem => oem.DisplayNumber == "11-51-7-586-925");
        audit.Events.Should().ContainSingle(evt => evt.Action == "garage.privacy.export");
        audit.Events[0].BeforeJson.Should().BeNull();
        audit.Events[0].AfterJson.Should().NotContain("WBA3A5C50DF123456",
            "audit metadata must never copy full VIN personal data");
    }

    [Test]
    public async Task Erase_Should_Delete_Garage_And_Be_Idempotent()
    {
        var repository = new InMemoryGarageRepository();
        await repository.GetOrCreateAsync(88, CancellationToken.None);
        var audit = new RecordingAudit();
        var service = new GaragePrivacyService(repository, audit);

        (await service.EraseAsync(88, "customer:88", CancellationToken.None)).Should().BeTrue();
        (await repository.GetByCustomerIdAsync(88, CancellationToken.None)).Should().BeNull();
        (await service.EraseAsync(88, "customer:88", CancellationToken.None)).Should().BeTrue();
        audit.Events.Should().HaveCount(2);
        audit.Events.Should().OnlyContain(evt => evt.Action == "garage.privacy.erase");
    }

    private sealed class FakeEncryptionService : IEncryptionService
    {
        public string CreateSaltKey(int size) => "salt";

        public string CreatePasswordHash(string password, string saltKey, string passwordFormat)
            => password;

        public string EncryptText(string plainText, string encryptionPrivateKey = "")
            => Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));

        public string DecryptText(string cipherText, string encryptionPrivateKey = "")
            => Encoding.UTF8.GetString(Convert.FromBase64String(cipherText));
    }

    private sealed class RecordingAudit : ICheckEngineAuditService
    {
        public List<AuditEvent> Events { get; } = [];

        public Task AppendAsync(
            string actor,
            string action,
            string entityType,
            string entityId,
            string? beforeJson,
            string? afterJson,
            CancellationToken cancellationToken = default)
        {
            Events.Add(new AuditEvent(actor, action, beforeJson, afterJson));
            return Task.CompletedTask;
        }
    }

    private sealed record AuditEvent(string Actor, string Action, string? BeforeJson, string? AfterJson);
}
