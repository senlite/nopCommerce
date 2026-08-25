using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.VinDecoders;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class BmwVinDecoderTests
{
    [Test]
    public void CanDecode_Should_Return_True_For_Supported_Bmw_Wmi()
    {
        var decoder = CreateDecoder();

        decoder.CanDecode("WBA").Should().BeTrue();
        decoder.CanDecode("5ux").Should().BeTrue();
        decoder.CanDecode("1HG").Should().BeFalse();
    }

    [Test]
    public async Task Decode_Should_Resolve_Configurations_From_Seeded_Pattern_Corpus()
    {
        var decoder = await CreateSeededDecoderAsync();

        var vin = Vin.Create("WBA8E9G51GNT12345", enforceCheckDigit: false);
        var contribution = decoder.Decode(vin);

        contribution.ReasonCode.Should().BeNull();
        contribution.Candidates.Should().NotBeEmpty();
        contribution.Candidates[0].Confidence.Value.Should().Be(0.92m);
        contribution.Candidates.Should().OnlyContain(candidate => candidate.VehicleConfigurationId > 0);
    }

    [Test]
    public async Task Decode_Should_Return_Failure_For_Unknown_Pattern_Under_Supported_Wmi()
    {
        var decoder = await CreateSeededDecoderAsync();
        var vin = Vin.Create("WBAZZZG51GNT12345", enforceCheckDigit: false);

        var contribution = decoder.Decode(vin);

        contribution.Candidates.Should().BeEmpty();
        contribution.ReasonCode.Should().Be("vin.decode_failed");
    }

    [Test]
    public void Infrastructure_Di_Should_Register_Bmw_Decoder_And_Registry()
    {
        var source = ReadInfrastructureFile("DependencyInjection", "ServiceCollectionExtensions.cs");

        source.Should().Contain("AddScoped<IManufacturerVinDecoder, BmwVinDecoder>");
        source.Should().Contain("AddScoped<IManufacturerVinDecoder, CatalogVinDecoder>");
        source.Should().Contain("AddScoped<IVinDecoderRegistry, VinDecoderRegistry>");
        source.Should().Contain("AddScoped<IVinSupportRepository, SqlVinSupportRepository>");
        source.Should().Contain("BmwVinConfigurationResolver");
        source.Should().Contain("BmwVinPatternSeedLoader");
        source.Should().Contain("AddScoped<IVehicleSeedLoader, CompositeVehicleSeedLoader>");
    }

    private static string ReadInfrastructureFile(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine.Infrastructure", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate Infrastructure/{string.Join('/', relativePath)}");
    }

    private static BmwVinDecoder CreateDecoder()
    {
        var allowList = new BmwVinWmiAllowList(["WBA", "WBS", "WBX", "5UX", "5YM"]);
        return new BmwVinDecoder(allowList, new InMemoryVinSupportRepository(), new BmwVinConfigurationResolver(new InMemoryVehicleAdminRepository()));
    }

    private static async Task<BmwVinDecoder> CreateSeededDecoderAsync()
    {
        var vehicleRepository = new InMemoryVehicleAdminRepository();
        await new BmwReferenceVehicleSeedLoader(vehicleRepository).SeedAsync(CancellationToken.None);

        var vinRepository = new InMemoryVinSupportRepository();
        await new BmwVinPatternSeedLoader(vehicleRepository, vinRepository).SeedAsync(CancellationToken.None);

        var allowList = new BmwVinWmiAllowList(["WBA", "WBS", "WBX", "5UX", "5YM"]);
        return new BmwVinDecoder(allowList, vinRepository, new BmwVinConfigurationResolver(vehicleRepository));
    }
}
