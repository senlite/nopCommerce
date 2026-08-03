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
public class VinDecodeApplicationServiceTests
{
    [Test]
    public async Task DecodeAsync_Should_Return_Failed_For_Invalid_Vin()
    {
        var service = new VinDecodeApplicationService(new FakeRegistry(null), new FakeTelemetry());

        var result = await service.DecodeAsync("123", CancellationToken.None);

        result.Outcome.Should().Be("Failed");
        result.ReasonCode.Should().Be("vin.invalid_length");
    }

    [Test]
    public async Task DecodeAsync_Should_Return_WmiUnknown_When_No_Decoder_Registered()
    {
        var service = new VinDecodeApplicationService(new FakeRegistry(null), new FakeTelemetry());

        var result = await service.DecodeAsync("1HGCM82633A004352", CancellationToken.None);

        result.Outcome.Should().Be("Failed");
        result.ReasonCode.Should().Be("vin.wmi_unknown");
        result.Wmi.Should().Be("1HG");
    }

    [Test]
    public async Task DecodeAsync_Should_Return_SingleMatch_For_One_Candidate()
    {
        var decoder = new FakeDecoder(VinDecodeContribution.WithCandidates([
            new VinDecodeCandidate
            {
                VehicleConfigurationId = 200,
                Confidence = Confidence.Create(0.91m),
                ModelYear = 2018
            }
        ]));

        var service = new VinDecodeApplicationService(new FakeRegistry(decoder), new FakeTelemetry());

        var result = await service.DecodeAsync("1HGCM82633A004352", CancellationToken.None);

        result.Outcome.Should().Be("SingleMatch");
        result.Candidates.Should().HaveCount(1);
        result.Candidates[0].VehicleConfigurationId.Should().Be(200);
    }

    [Test]
    public async Task DecodeAsync_Should_Return_NeedsDisambiguation_For_Multiple_Candidates()
    {
        var decoder = new FakeDecoder(VinDecodeContribution.WithCandidates([
            new VinDecodeCandidate { VehicleConfigurationId = 1, Confidence = Confidence.Create(0.70m) },
            new VinDecodeCandidate { VehicleConfigurationId = 2, Confidence = Confidence.Create(0.82m) }
        ]));

        var service = new VinDecodeApplicationService(new FakeRegistry(decoder), new FakeTelemetry());

        var result = await service.DecodeAsync("1HGCM82633A004352", CancellationToken.None);

        result.Outcome.Should().Be("NeedsDisambiguation");
        result.Candidates.Should().HaveCount(2);
        result.Candidates[0].VehicleConfigurationId.Should().Be(2);
    }

    [Test]
    public async Task DecodeAsync_Should_Return_Graceful_Failure_When_Decoder_Throws()
    {
        var service = new VinDecodeApplicationService(new FakeRegistry(new ThrowingDecoder()), new FakeTelemetry());

        var result = await service.DecodeAsync("1HGCM82633A004352", CancellationToken.None);

        result.Outcome.Should().Be("Failed");
        result.ReasonCode.Should().Be("vin.decode_failed");
        result.Candidates.Should().BeEmpty();
    }

    private sealed class FakeRegistry : IVinDecoderRegistry
    {
        private readonly IManufacturerVinDecoder? _decoder;

        public FakeRegistry(IManufacturerVinDecoder? decoder)
        {
            _decoder = decoder;
        }

        public IManufacturerVinDecoder? Resolve(string wmi)
        {
            return _decoder;
        }
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
            throw new InvalidOperationException("decoder failure");
        }
    }

    private sealed class FakeTelemetry : ICheckEngineTelemetry
    {
        public void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties)
        {
        }
    }
}
