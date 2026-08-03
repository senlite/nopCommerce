using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Infrastructure.DependencyInjection;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.VinDecoders;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class BmwVinDecoderTests
{
    [Test]
    public void CanDecode_Should_Return_True_For_Supported_Bmw_Wmi()
    {
        var decoder = new BmwVinDecoder();

        decoder.CanDecode("WBA").Should().BeTrue();
        decoder.CanDecode("5ux").Should().BeTrue();
    }

    [Test]
    public void Decode_Should_Return_Single_Candidate_For_Known_Bmw_Vds_Prefix()
    {
        var decoder = new BmwVinDecoder();
        var vin = Vin.Create("WBA8E9G51GNT12345", enforceCheckDigit: false);

        var contribution = decoder.Decode(vin);

        contribution.ReasonCode.Should().BeNull();
        contribution.Candidates.Should().HaveCount(1);
        contribution.Candidates[0].VehicleConfigurationId.Should().Be(10041);
        contribution.Candidates[0].Confidence.Value.Should().Be(0.92m);
    }

    [Test]
    public void Decode_Should_Return_Failure_For_Unknown_Pattern_Under_Supported_Wmi()
    {
        var decoder = new BmwVinDecoder();
        var vin = Vin.Create("WBAZZZG51GNT12345", enforceCheckDigit: false);

        var contribution = decoder.Decode(vin);

        contribution.Candidates.Should().BeEmpty();
        contribution.ReasonCode.Should().Be("vin.decode_failed");
    }

    [Test]
    public void Infrastructure_Di_Should_Register_Bmw_Decoder_And_Registry()
    {
        var services = new ServiceCollection();

        services.AddCheckEngineInfrastructure();

        using var provider = services.BuildServiceProvider();
        var decoders = provider.GetServices<IManufacturerVinDecoder>();
        var registry = provider.GetService<IVinDecoderRegistry>();

        decoders.Should().ContainSingle(d => d is BmwVinDecoder);
        registry.Should().NotBeNull();
        registry!.Resolve("WBA").Should().BeOfType<BmwVinDecoder>();
    }
}
