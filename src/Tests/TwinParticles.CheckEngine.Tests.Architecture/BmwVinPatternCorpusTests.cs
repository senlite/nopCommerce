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

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class BmwVinPatternCorpusTests
{
    [Test]
    public void Corpus_Should_List_All_Documented_Wmis()
    {
        var corpus = ReadCorpus();
        corpus.Wmis.Select(wmi => wmi.Wmi).Should().BeEquivalentTo(["WBA", "WBS", "WBX", "5UX", "5YM"]);
    }

    [Test]
    public void Corpus_Patterns_Should_Include_Documented_Check_Engine_Examples()
    {
        var corpus = ReadCorpus();
        corpus.Patterns.Select(pattern => pattern.Pattern).Should().Contain(["3A5", "8E9"]);
    }

    [Test]
    public async Task Every_Corpus_Pattern_Should_Resolve_To_At_Least_One_Configuration()
    {
        var vehicleRepository = new InMemoryVehicleAdminRepository();
        await new BmwReferenceVehicleSeedLoader(vehicleRepository).SeedAsync(CancellationToken.None);

        var vinRepository = new InMemoryVinSupportRepository();
        await new BmwVinPatternSeedLoader(vehicleRepository, vinRepository).SeedAsync(CancellationToken.None);

        var resolver = new BmwVinConfigurationResolver(vehicleRepository);
        var patterns = await vinRepository.GetPatternsAsync(CancellationToken.None);

        foreach (var pattern in patterns)
        {
            var resolved = await resolver.ResolveAsync(pattern, modelYear: null, CancellationToken.None);
            resolved.Should().NotBeEmpty($"pattern {pattern.Pattern} must map to seeded vehicle configurations");
        }
    }

    [Test]
    public void Vin_Model_Year_Should_Decode_Iso_Position_10()
    {
        VinModelYear.TryDecode('G', out var year).Should().BeTrue();
        year.Should().Be(2016);
        VinModelYear.DecodeFromVin("WBA8E9G51GNT12345").Should().Be(2016);
    }

    private static BmwVinPatternCatalogDocument ReadCorpus()
    {
        var path = LocateCorpusPath("bmw-vin-patterns.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<BmwVinPatternCatalogDocument>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new BmwVinPatternCatalogDocument();
    }

    private static string LocateCorpusPath(string fileName)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Tests", "corpus", "vin", fileName);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Unable to locate corpus file {fileName}");
    }
}
