using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VinDecodePipelineBehaviorTests
{
    [Test]
    public async Task DecodeAsync_Should_Emit_Telemetry_For_Invalid_Vin()
    {
        var telemetry = new CapturingTelemetry();
        var service = new VinDecodeApplicationService(new FakeRegistry(null), telemetry);

        var result = await service.DecodeAsync("123", CancellationToken.None);

        result.Outcome.Should().Be("Failed");
        telemetry.Events.Should().HaveCount(1);
        telemetry.Events[0].Properties["reasonCode"].Should().Be("vin.invalid_length");
        telemetry.Events[0].Properties["candidateCount"].Should().Be(0);
    }

    [Test]
    public async Task DecodeAsync_Should_Emit_Telemetry_For_Unknown_Wmi()
    {
        var telemetry = new CapturingTelemetry();
        var service = new VinDecodeApplicationService(new FakeRegistry(null), telemetry);

        var result = await service.DecodeAsync("1HGCM82633A004352", CancellationToken.None);

        result.ReasonCode.Should().Be("vin.wmi_unknown");
        telemetry.Events.Should().HaveCount(1);
        telemetry.Events[0].Properties["wmi"].Should().Be("1HG");
        telemetry.Events[0].Properties["candidateCount"].Should().Be(0);
    }

    [Test]
    public async Task DecodeAsync_Should_Order_Candidates_By_Confidence_Descending()
    {
        var telemetry = new CapturingTelemetry();
        var decoder = new FakeDecoder(VinDecodeContribution.WithCandidates([
            new VinDecodeCandidate { VehicleConfigurationId = 100, Confidence = Confidence.Create(0.41m) },
            new VinDecodeCandidate { VehicleConfigurationId = 200, Confidence = Confidence.Create(0.87m) },
            new VinDecodeCandidate { VehicleConfigurationId = 300, Confidence = Confidence.Create(0.63m) }
        ]));

        var service = new VinDecodeApplicationService(new FakeRegistry(decoder), telemetry);

        var result = await service.DecodeAsync("1HGCM82633A004352", CancellationToken.None);

        result.Outcome.Should().Be("NeedsDisambiguation");
        result.Candidates.Should().HaveCount(3);
        result.Candidates[0].VehicleConfigurationId.Should().Be(200);
        result.Candidates[1].VehicleConfigurationId.Should().Be(300);
        result.Candidates[2].VehicleConfigurationId.Should().Be(100);

        telemetry.Events.Should().HaveCount(1);
        telemetry.Events[0].Properties["candidateCount"].Should().Be(3);
        telemetry.Events[0].Properties["outcome"].Should().Be("NeedsDisambiguation");
    }

    [Test]
    public async Task DecodeAsync_Should_Emit_Failure_Reason_From_Contribution_When_No_Candidates()
    {
        var telemetry = new CapturingTelemetry();
        var decoder = new FakeDecoder(VinDecodeContribution.Failed("vin.pattern_unknown"));

        var service = new VinDecodeApplicationService(new FakeRegistry(decoder), telemetry);

        var result = await service.DecodeAsync("1HGCM82633A004352", CancellationToken.None);

        result.Outcome.Should().Be("Failed");
        result.ReasonCode.Should().Be("vin.pattern_unknown");
        telemetry.Events[0].Properties["reasonCode"].Should().Be("vin.pattern_unknown");
    }

    [Test]
    public async Task DecodeAsync_Should_Map_Exception_To_DecodeFailed_Reason()
    {
        var telemetry = new CapturingTelemetry();
        var service = new VinDecodeApplicationService(new FakeRegistry(new ThrowingDecoder()), telemetry);

        var result = await service.DecodeAsync("1HGCM82633A004352", CancellationToken.None);

        result.Outcome.Should().Be("Failed");
        result.ReasonCode.Should().Be("vin.decode_failed");
        telemetry.Events[0].Properties["reasonCode"].Should().Be("vin.decode_failed");
    }

    private sealed class FakeRegistry : IVinDecoderRegistry
    {
        private readonly IManufacturerVinDecoder? _decoder;

        public FakeRegistry(IManufacturerVinDecoder? decoder)
        {
            _decoder = decoder;
        }

        public IManufacturerVinDecoder? Resolve(string wmi) => _decoder;
    }

    private sealed class FakeDecoder : IManufacturerVinDecoder
    {
        private readonly VinDecodeContribution _contribution;

        public FakeDecoder(VinDecodeContribution contribution)
        {
            _contribution = contribution;
        }

        public bool CanDecode(string wmi) => true;

        public VinDecodeContribution Decode(Vin vin) => _contribution;
    }

    private sealed class ThrowingDecoder : IManufacturerVinDecoder
    {
        public bool CanDecode(string wmi) => true;

        public VinDecodeContribution Decode(Vin vin)
        {
            throw new System.InvalidOperationException("decoder failed");
        }
    }

    private sealed class CapturingTelemetry : ICheckEngineTelemetry
    {
        public List<(string EventName, IReadOnlyDictionary<string, object?> Properties)> Events { get; } = [];

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties)
        {
            Events.Add((eventName, properties));
        }
    }
}
