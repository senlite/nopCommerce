using System.IO;
using System.Linq;
using System.Text.Json;
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
public class TopBrandVinDecoderTests
{
    private static readonly string[] ExpectedMakeCodes =
    [
        "TOYOTA", "VOLKSWAGEN", "HONDA", "HYUNDAI", "FORD", "MERCEDES", "NISSAN", "KIA", "CHEVROLET"
    ];

    [Test]
    public async Task Catalogs_Should_Cover_The_Nine_Brands_Besides_Bmw()
    {
        var catalogs = await VinPatternCatalog.LoadAllAsync(CancellationToken.None);
        catalogs.Select(catalog => catalog.MakeCode)
            .Should()
            .BeEquivalentTo(ExpectedMakeCodes.Append("BMW"));
        catalogs.Where(VinPatternCatalog.IsBmw).Should().ContainSingle();
    }

    [Test]
    public async Task CanDecode_Should_Accept_Documented_Non_Bmw_Wmis_Only()
    {
        var decoder = await CreateDecoderAsync(seed: false);

        decoder.CanDecode("1FA").Should().BeTrue();
        decoder.CanDecode("1hg").Should().BeTrue();
        decoder.CanDecode("JTD").Should().BeTrue();
        decoder.CanDecode("2T3").Should().BeTrue();
        decoder.CanDecode("5J6").Should().BeTrue();
        decoder.CanDecode("3VV").Should().BeTrue();
        decoder.CanDecode("KM8").Should().BeTrue();
        decoder.CanDecode("5N1").Should().BeTrue();
        decoder.CanDecode("2GN").Should().BeTrue();
        decoder.CanDecode("5XY").Should().BeTrue();
        decoder.CanDecode("5FP").Should().BeTrue();
        decoder.CanDecode("3FT").Should().BeTrue();
        decoder.CanDecode("NMT").Should().BeTrue();
        decoder.CanDecode("5NT").Should().BeTrue();
        decoder.CanDecode("KL8").Should().BeTrue();
        decoder.CanDecode("7MU").Should().BeTrue();
        decoder.CanDecode("7FA").Should().BeTrue();
        decoder.CanDecode("3FM").Should().BeTrue();
        decoder.CanDecode("WBA").Should().BeFalse();
        decoder.CanDecode("ZZZ").Should().BeFalse();
    }

    [Test]
    public async Task Decode_Should_Fail_Closed_For_Known_Wmi_Without_Vds_Pattern()
    {
        var decoder = await CreateDecoderAsync(seed: true);
        var vin = Vin.Create("1FAZZZTD5M5100001", enforceCheckDigit: false);

        var contribution = decoder.Decode(vin);

        contribution.Candidates.Should().BeEmpty();
        contribution.ReasonCode.Should().Be("vin.decode_failed");
    }

    [Test]
    public async Task Decode_Should_Resolve_Nhtsa_Documented_Ford_Mustang()
    {
        var decoder = await CreateDecoderAsync(seed: true);
        var vin = Vin.Create("1FA6P8TD5M5100001", enforceCheckDigit: false);

        var contribution = decoder.Decode(vin);

        contribution.ReasonCode.Should().BeNull();
        contribution.Candidates.Should().NotBeEmpty();
        contribution.Candidates[0].Confidence.Value.Should().Be(0.90m);
        contribution.Candidates[0].ModelYear.Should().Be(2021);
    }

    [Test]
    public async Task Decode_Should_Resolve_Nhtsa_Documented_Honda_Accord()
    {
        var decoder = await CreateDecoderAsync(seed: true);
        var vin = Vin.Create("1HGCM82633A004352", enforceCheckDigit: false);

        var contribution = decoder.Decode(vin);

        contribution.ReasonCode.Should().BeNull();
        contribution.Candidates.Should().NotBeEmpty();
        contribution.Candidates[0].ModelYear.Should().Be(2003);
    }

    [Test]
    public async Task Decode_Should_Resolve_Nhtsa_Documented_Toyota_Rav4()
    {
        var decoder = await CreateDecoderAsync(seed: true);
        var vin = Vin.Create("2T3W1RFV0MW000001", enforceCheckDigit: false);

        var contribution = decoder.Decode(vin);

        contribution.ReasonCode.Should().BeNull();
        contribution.Candidates.Should().NotBeEmpty();
        contribution.Candidates[0].Confidence.Value.Should().Be(0.90m);
        contribution.Candidates[0].ModelYear.Should().Be(2021);
    }

    [Test]
    public async Task Decode_Should_Resolve_Nhtsa_Documented_Volkswagen_Tiguan()
    {
        var decoder = await CreateDecoderAsync(seed: true);
        var vin = Vin.Create("3VV0B7AX0KM000001", enforceCheckDigit: false);

        var contribution = decoder.Decode(vin);

        contribution.ReasonCode.Should().BeNull();
        contribution.Candidates.Should().NotBeEmpty();
        contribution.Candidates[0].ModelYear.Should().Be(2019);
    }

    [Test]
    public async Task Decode_Should_Resolve_Nhtsa_Documented_Honda_Ridgeline()
    {
        var decoder = await CreateDecoderAsync(seed: true);
        var vin = Vin.Create("5FPYK3F53HB000001", enforceCheckDigit: false);

        var contribution = decoder.Decode(vin);

        contribution.ReasonCode.Should().BeNull();
        contribution.Candidates.Should().NotBeEmpty();
        contribution.Candidates[0].ModelYear.Should().Be(2017);
    }

    [Test]
    public async Task Decode_Should_Resolve_Nhtsa_Documented_Ford_Maverick()
    {
        var decoder = await CreateDecoderAsync(seed: true);
        var vin = Vin.Create("3FTTW8E9XNRA00001", enforceCheckDigit: false);

        var contribution = decoder.Decode(vin);

        contribution.ReasonCode.Should().BeNull();
        contribution.Candidates.Should().NotBeEmpty();
        contribution.Candidates[0].ModelYear.Should().Be(2022);
    }

    [Test]
    public async Task Decode_Should_Resolve_Nhtsa_Documented_Volkswagen_Taos()
    {
        var decoder = await CreateDecoderAsync(seed: true);
        var vin = Vin.Create("3VVLX7B23NM000001", enforceCheckDigit: false);

        var contribution = decoder.Decode(vin);

        contribution.ReasonCode.Should().BeNull();
        contribution.Candidates.Should().NotBeEmpty();
        contribution.Candidates[0].ModelYear.Should().Be(2022);
    }

    [Test]
    public async Task Decode_Should_Resolve_Nhtsa_Documented_Hyundai_Venue()
    {
        var decoder = await CreateDecoderAsync(seed: true);
        var vin = Vin.Create("KMHRC8A32LU000001", enforceCheckDigit: false);

        var contribution = decoder.Decode(vin);

        contribution.ReasonCode.Should().BeNull();
        contribution.Candidates.Should().NotBeEmpty();
        contribution.Candidates[0].ModelYear.Should().Be(2020);
    }

    [Test]
    public async Task Decode_Should_Resolve_Nhtsa_Documented_Toyota_Corolla_Cross()
    {
        var decoder = await CreateDecoderAsync(seed: true);
        var vin = Vin.Create("7MUBAABG0NV000001", enforceCheckDigit: false);

        var contribution = decoder.Decode(vin);

        contribution.ReasonCode.Should().BeNull();
        contribution.Candidates.Should().NotBeEmpty();
        contribution.Candidates[0].ModelYear.Should().Be(2022);
    }

    [Test]
    public async Task Decode_Should_Year_Disambiguate_Ford_Explorer_Generations()
    {
        var decoder = await CreateDecoderAsync(seed: true);
        var vin2018 = Vin.Create("1FM5K8D81JGA00001", enforceCheckDigit: false);
        var vin2020 = Vin.Create("1FM5K8GC0LGA00001", enforceCheckDigit: false);

        var older = decoder.Decode(vin2018);
        var newer = decoder.Decode(vin2020);

        older.ReasonCode.Should().BeNull();
        newer.ReasonCode.Should().BeNull();
        older.Candidates.Should().NotBeEmpty();
        newer.Candidates.Should().NotBeEmpty();
        older.Candidates.Select(c => c.VehicleConfigurationId)
            .Should().NotIntersectWith(newer.Candidates.Select(c => c.VehicleConfigurationId));
    }

    [Test]
    public async Task Registry_Should_Route_Bmw_And_Catalog_Wmis_Separately()
    {
        var vehicleRepository = new InMemoryVehicleAdminRepository();
        var vinRepository = new InMemoryVinSupportRepository();
        await SeedAsync(vehicleRepository, vinRepository);

        var catalogs = await VinPatternCatalog.LoadAllAsync(CancellationToken.None);
        var bmw = catalogs.Single(VinPatternCatalog.IsBmw);
        var others = catalogs.Where(catalog => !VinPatternCatalog.IsBmw(catalog)).ToList();
        var bmwDecoder = new BmwVinDecoder(
            new BmwVinWmiAllowList(bmw.Wmis.Select(wmi => wmi.Wmi)),
            vinRepository,
            new BmwVinConfigurationResolver(vehicleRepository));
        var catalogDecoder = new CatalogVinDecoder(
            new CatalogVinWmiAllowList(others.SelectMany(catalog => catalog.Wmis).Select(wmi => wmi.Wmi)),
            vinRepository,
            new BmwVinConfigurationResolver(vehicleRepository));
        var registry = new VinDecoderRegistry([bmwDecoder, catalogDecoder]);

        registry.Resolve("WBA").Should().BeSameAs(bmwDecoder);
        registry.Resolve("1FA").Should().BeSameAs(catalogDecoder);
        registry.Resolve("ZZZ").Should().BeNull();
    }

    [Test]
    public async Task Seed_Should_Attach_Bilingual_Configuration_Aliases()
    {
        var vehicleRepository = new InMemoryVehicleAdminRepository();
        var vinRepository = new InMemoryVinSupportRepository();
        await SeedAsync(vehicleRepository, vinRepository);

        var makes = await vehicleRepository.GetMakesAsync(CancellationToken.None);
        var toyotaId = makes.Single(make => make.Code == "TOYOTA").Id;
        var toyotaModels = (await vehicleRepository.GetModelsAsync(CancellationToken.None))
            .Where(model => model.MakeId == toyotaId)
            .Select(model => model.Id)
            .ToHashSet();
        var toyotaGenerationIds = (await vehicleRepository.GetGenerationsAsync(CancellationToken.None))
            .Where(generation => toyotaModels.Contains(generation.ModelId))
            .Select(generation => generation.Id)
            .ToHashSet();
        var toyotaConfigs = (await vehicleRepository.GetConfigurationsAsync(CancellationToken.None))
            .Where(configuration => toyotaGenerationIds.Contains(configuration.GenerationId))
            .ToList();
        toyotaConfigs.Should().NotBeEmpty();

        var aliases = await vehicleRepository.GetAliasesAsync(CancellationToken.None);
        foreach (var configuration in toyotaConfigs)
        {
            aliases.Should().Contain(alias => alias.NodeType == "configuration" && alias.NodeId == configuration.Id && alias.Locale == "en");
            aliases.Should().Contain(alias => alias.NodeType == "configuration" && alias.NodeId == configuration.Id && alias.Locale == "ar");
        }
    }

    [Test]
    public async Task Every_Non_Bmw_Pattern_Should_Resolve_To_A_Seeded_Configuration()
    {
        var vehicleRepository = new InMemoryVehicleAdminRepository();
        var vinRepository = new InMemoryVinSupportRepository();
        await SeedAsync(vehicleRepository, vinRepository);

        var resolver = new BmwVinConfigurationResolver(vehicleRepository);
        var patterns = (await vinRepository.GetPatternsAsync(CancellationToken.None))
            .Where(pattern => pattern.MakeId > 0)
            .ToList();

        var makes = await vehicleRepository.GetMakesAsync(CancellationToken.None);
        var bmwMakeId = makes.Single(make => make.Code == "BMW").Id;
        var nonBmw = patterns.Where(pattern => pattern.MakeId != bmwMakeId).ToList();
        nonBmw.Should().NotBeEmpty();

        foreach (var pattern in nonBmw)
        {
            var resolved = await resolver.ResolveAsync(pattern, modelYear: null, CancellationToken.None);
            resolved.Should().NotBeEmpty($"pattern {pattern.Pattern} ({pattern.ModelCode}/{pattern.GenerationCode}) must map to seeded vehicle configurations");
        }
    }

    [Test]
    public async Task Composite_Seed_Should_Insert_All_Ten_Makes()
    {
        var vehicleRepository = new InMemoryVehicleAdminRepository();
        var vinRepository = new InMemoryVinSupportRepository();
        await SeedAsync(vehicleRepository, vinRepository);

        var makeCodes = (await vehicleRepository.GetMakesAsync(CancellationToken.None))
            .Select(make => make.Code)
            .ToArray();
        makeCodes.Should().BeEquivalentTo(ExpectedMakeCodes.Append("BMW"));

        var wmis = await vinRepository.GetWmisAsync(CancellationToken.None);
        wmis.Select(wmi => wmi.Wmi).Should().Contain(["WBA", "1FA", "1HG", "JTD", "5XY", "3GN", "WDD", "KMH", "5FP", "3FT", "NMT", "5NT", "KL8", "7MU", "7FA", "3FM"]);
    }

    [Test]
    public void Corpus_Files_Should_Match_Embedded_Vin_Catalogs()
    {
        var dataDir = LocatePath("src", "Plugins", "TwinParticles.CheckEngine.Infrastructure", "Vehicle", "Vin", "Data");
        var corpusDir = LocatePath("src", "Tests", "corpus", "vin");
        foreach (var source in Directory.GetFiles(dataDir, "*-vin-patterns.json"))
        {
            var name = Path.GetFileName(source);
            var corpus = Path.Combine(corpusDir, name);
            File.Exists(corpus).Should().BeTrue($"corpus is missing {name}");
            File.ReadAllText(corpus).Should().Be(File.ReadAllText(source));
        }
    }

    [Test]
    public void Every_Pattern_Should_Declare_Nhtsa_Or_Checkengine_Provenance()
    {
        var dataDir = LocatePath("src", "Plugins", "TwinParticles.CheckEngine.Infrastructure", "Vehicle", "Vin", "Data");
        foreach (var path in Directory.GetFiles(dataDir, "*-vin-patterns.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var pattern in document.RootElement.GetProperty("patterns").EnumerateArray())
            {
                var provenance = pattern.GetProperty("provenance").GetString();
                provenance.Should().NotBeNullOrWhiteSpace(path);
                (provenance!.Contains("nhtsa-vpic", System.StringComparison.Ordinal)
                 || provenance.Contains("checkengine-", System.StringComparison.Ordinal))
                    .Should().BeTrue($"{path} pattern {pattern.GetProperty("pattern").GetString()} lacks documented provenance");
            }
        }
    }

    private static async Task<CatalogVinDecoder> CreateDecoderAsync(bool seed)
    {
        var vehicleRepository = new InMemoryVehicleAdminRepository();
        var vinRepository = new InMemoryVinSupportRepository();
        if (seed)
            await SeedAsync(vehicleRepository, vinRepository);

        var catalogs = await VinPatternCatalog.LoadAllAsync(CancellationToken.None);
        var wmis = catalogs
            .Where(catalog => !VinPatternCatalog.IsBmw(catalog))
            .SelectMany(catalog => catalog.Wmis)
            .Select(wmi => wmi.Wmi);
        return new CatalogVinDecoder(
            new CatalogVinWmiAllowList(wmis),
            vinRepository,
            new BmwVinConfigurationResolver(vehicleRepository));
    }

    private static async Task SeedAsync(InMemoryVehicleAdminRepository vehicleRepository, InMemoryVinSupportRepository vinRepository)
    {
        await new CompositeVehicleSeedLoader(
            new BmwReferenceVehicleSeedLoader(vehicleRepository, new BmwVinPatternSeedLoader(vehicleRepository, vinRepository)),
            new TopBrandReferenceVehicleSeedLoader(vehicleRepository, new VinPatternSeedLoader(vehicleRepository, vinRepository)))
            .SeedAsync(CancellationToken.None);
    }

    private static string LocatePath(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, .. relativePath]);
            if (Directory.Exists(candidate) || File.Exists(candidate))
                return candidate;
        }

        throw new DirectoryNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }
}
