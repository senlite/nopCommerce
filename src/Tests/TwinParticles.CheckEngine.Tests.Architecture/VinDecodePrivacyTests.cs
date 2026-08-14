using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VinDecodePrivacyTests
{
    private const string SampleVin = "1HGCM82633A004352";

    [Test]
    public async Task DecodeAsync_Telemetry_Should_Never_Contain_Full_Vin()
    {
        var telemetry = new CapturingTelemetry();
        var privacy = new FakeVinPrivacyService();
        var decoder = new FakeDecoder(VinDecodeContribution.WithCandidates([
            new VinDecodeCandidate
            {
                VehicleConfigurationId = 42,
                Confidence = Confidence.Create(0.92m)
            }
        ]));

        var service = new VinDecodeApplicationService(
            new FakeRegistry(decoder),
            telemetry,
            privacyService: privacy);

        await service.DecodeAsync(SampleVin, CancellationToken.None);

        telemetry.Events.Should().HaveCount(1);
        var serialized = string.Join('|', telemetry.Events[0].Properties.Select(pair => $"{pair.Key}={pair.Value}"));
        serialized.Should().NotContain(SampleVin);
        telemetry.Events[0].Properties["vinLast4"].Should().Be("4352");
        telemetry.Events[0].Properties["vinHash"].Should().Be("hash:17");
    }

    [Test]
    public async Task DecodeAsync_Should_Append_Redacted_Audit_Entry()
    {
        var audit = new RecordingAuditService();
        var privacy = new FakeVinPrivacyService();
        var decoder = new FakeDecoder(VinDecodeContribution.WithCandidates([
            new VinDecodeCandidate
            {
                VehicleConfigurationId = 42,
                Confidence = Confidence.Create(0.92m)
            }
        ]));

        var service = new VinDecodeApplicationService(
            new FakeRegistry(decoder),
            new NoopTelemetry(),
            privacyService: privacy,
            auditService: audit);

        await service.DecodeAsync(SampleVin, CancellationToken.None);

        audit.Entries.Should().HaveCount(1);
        audit.Entries[0].Action.Should().Be("vin.decode");
        audit.Entries[0].EntityId.Should().Be("hash:17");
        audit.Entries[0].AfterJson.Should().Contain("vinLast4");
        audit.Entries[0].AfterJson.Should().Contain("4352");
        audit.Entries[0].AfterJson.Should().NotContain(SampleVin);
    }

    [Test]
    public void Vin_Privacy_Service_Should_Hash_Without_Embedding_Raw_Vin()
    {
        var service = new VinPrivacyService(new Nop.Core.Domain.Security.SecuritySettings
        {
            EncryptionKey = "unit-test-key-32-characters-long"
        });

        var hash = service.CreateHash(SampleVin);

        hash.Should().HaveLength(64);
        hash.Should().NotContain(SampleVin);
        service.GetLast4(SampleVin).Should().Be("4352");
    }

    private sealed class FakeRegistry : IVinDecoderRegistry
    {
        private readonly IManufacturerVinDecoder _decoder;

        public FakeRegistry(IManufacturerVinDecoder decoder) => _decoder = decoder;

        public IManufacturerVinDecoder? Resolve(string wmi) => _decoder;
    }

    private sealed class FakeDecoder : IManufacturerVinDecoder
    {
        private readonly VinDecodeContribution _contribution;

        public FakeDecoder(VinDecodeContribution contribution) => _contribution = contribution;

        public bool CanDecode(string wmi) => true;

        public VinDecodeContribution Decode(Vin vin) => _contribution;
    }

    private sealed class FakeVinPrivacyService : IVinPrivacyService
    {
        public string? GetLast4(string? normalizedVin)
            => string.IsNullOrWhiteSpace(normalizedVin) || normalizedVin.Length < 4 ? null : normalizedVin[^4..];

        public string CreateHash(string normalizedVin) => $"hash:{normalizedVin.Length}";
    }

    private sealed class CapturingTelemetry : ICheckEngineTelemetry
    {
        public List<(string EventName, IReadOnlyDictionary<string, object?> Properties)> Events { get; } = [];

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties)
            => Events.Add((eventName, properties));
    }

    private sealed class NoopTelemetry : ICheckEngineTelemetry
    {
        public void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties)
        {
        }
    }

    private sealed class RecordingAuditService : ICheckEngineAuditService
    {
        public List<(string Action, string EntityId, string? AfterJson)> Entries { get; } = [];

        public Task AppendAsync(
            string actor,
            string action,
            string entityType,
            string entityId,
            string? beforeJson,
            string? afterJson,
            CancellationToken cancellationToken = default)
        {
            Entries.Add((action, entityId, afterJson));
            return Task.CompletedTask;
        }
    }
}
