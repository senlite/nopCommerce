using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Vehicle.Admin;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.VinDecoders;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class TopBrandVinDecodeApplicationTests
{
    [Test]
    public async Task Application_Decode_Should_Resolve_Check_Digit_Valid_Golden_Vins()
    {
        var service = await CreateSeededServiceAsync();
        foreach (var vector in ReadGoldenVectors().Where(vector => vector.ExpectReason is null))
        {
            Vin.IsCheckDigitValid(vector.Vin).Should().BeTrue(vector.Vin);

            var result = await service.DecodeAsync(vector.Vin, CancellationToken.None);

            result.ReasonCode.Should().BeNull(vector.Vin);
            result.CheckDigitValid.Should().BeTrue();
            result.Wmi.Should().Be(vector.Wmi);
            result.Outcome.Should().BeOneOf("SingleMatch", "NeedsDisambiguation");
            result.Candidates.Should().NotBeEmpty(vector.Vin);
            result.Candidates.Should().OnlyContain(candidate => candidate.VehicleConfigurationId > 0);
            result.Candidates.Should().OnlyContain(candidate => candidate.ModelYear == vector.ModelYear);
        }
    }

    [Test]
    public async Task Application_Decode_Should_Fail_Closed_For_Known_Honda_Wmi_Without_Vds()
    {
        var service = await CreateSeededServiceAsync();
        var result = await service.DecodeAsync("1HGBH41JXMN109186", CancellationToken.None);

        result.Outcome.Should().Be("Failed");
        result.ReasonCode.Should().Be("vin.decode_failed");
        result.Wmi.Should().Be("1HG");
        result.CheckDigitValid.Should().BeTrue();
        result.Candidates.Should().BeEmpty();
    }

    [Test]
    public async Task Application_Decode_Should_Still_Fail_Check_Digit_Before_Wmi_Lookup()
    {
        var service = await CreateSeededServiceAsync();
        var result = await service.DecodeAsync("1FAZZZTD5M5100001", CancellationToken.None);

        result.Outcome.Should().Be("Failed");
        result.ReasonCode.Should().Be("vin.check_digit_failed");
        result.Candidates.Should().BeEmpty();
    }

    [Test]
    public async Task Honda_Accord_Disambiguation_Labels_Should_Include_Distinct_Markets()
    {
        var harness = await CreateSeededHarnessAsync();
        var result = await harness.Decode.DecodeAsync("1HGCM82633A004352", CancellationToken.None);

        result.Outcome.Should().Be("NeedsDisambiguation");
        result.Candidates.Should().HaveCount(2);

        var labels = await harness.Admin.GetConfigurationDisplayLabelsAsync(
            result.Candidates.Select(candidate => candidate.VehicleConfigurationId),
            CancellationToken.None);

        labels.Should().HaveCount(2);
        labels.Values.Should().OnlyHaveUniqueItems();
        labels.Values.Should().Contain(label => label.Contains("Europe", StringComparison.Ordinal));
        labels.Values.Should().Contain(label => label.Contains("Gulf", StringComparison.Ordinal));
        labels.Values.Should().OnlyContain(label =>
            label.Contains("Honda Accord CM LX", StringComparison.Ordinal)
            && label.Contains("2003-2007", StringComparison.Ordinal));
    }

    [Test]
    public void Golden_Corpus_Should_Declare_Provenance_For_Resolvable_Rows()
    {
        foreach (var vector in ReadGoldenVectors().Where(vector => vector.ExpectReason is null))
        {
            vector.Provenance.Should().Contain("nhtsa-vpic:");
            vector.Vin[..3].Should().Be(vector.Wmi);
            vector.Vin.Substring(3, 3).Should().Be(vector.VdsPrefix);
        }
    }

    private static async Task<VinDecodeApplicationService> CreateSeededServiceAsync()
        => (await CreateSeededHarnessAsync()).Decode;

    private static async Task<SeededHarness> CreateSeededHarnessAsync()
    {
        var vehicleRepository = new InMemoryVehicleAdminRepository();
        var vinRepository = new InMemoryVinSupportRepository();
        var seedLoader = new CompositeVehicleSeedLoader(
            new BmwReferenceVehicleSeedLoader(vehicleRepository, new BmwVinPatternSeedLoader(vehicleRepository, vinRepository)),
            new TopBrandReferenceVehicleSeedLoader(vehicleRepository, new VinPatternSeedLoader(vehicleRepository, vinRepository)));
        await seedLoader.SeedAsync(CancellationToken.None);

        var catalogs = await VinPatternCatalog.LoadAllAsync(CancellationToken.None);
        var bmw = catalogs.Single(VinPatternCatalog.IsBmw);
        var others = catalogs.Where(catalog => !VinPatternCatalog.IsBmw(catalog));
        var resolver = new BmwVinConfigurationResolver(vehicleRepository);
        var registry = new VinDecoderRegistry([
            new BmwVinDecoder(new BmwVinWmiAllowList(bmw.Wmis.Select(wmi => wmi.Wmi)), vinRepository, resolver),
            new CatalogVinDecoder(
                new CatalogVinWmiAllowList(others.SelectMany(catalog => catalog.Wmis).Select(wmi => wmi.Wmi)),
                vinRepository,
                resolver)
        ]);

        return new SeededHarness(
            new VinDecodeApplicationService(registry, new NoopTelemetry()),
            new VehicleAdminService(vehicleRepository, seedLoader));
    }

    private static IReadOnlyList<GoldenVinVector> ReadGoldenVectors()
    {
        var path = LocatePath("src", "Tests", "corpus", "vin", "top-brands-golden-vins.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("vectors").EnumerateArray().Select(element => new GoldenVinVector
        {
            Vin = element.GetProperty("vin").GetString()!,
            Wmi = element.GetProperty("wmi").GetString()!,
            VdsPrefix = element.GetProperty("vdsPrefix").GetString()!,
            ModelYear = element.TryGetProperty("modelYear", out var year) ? year.GetInt32() : null,
            Provenance = element.TryGetProperty("provenance", out var provenance) ? provenance.GetString() : null,
            ExpectReason = element.TryGetProperty("expectReason", out var reason) ? reason.GetString() : null
        }).ToList();
    }

    private static string LocatePath(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, .. relativePath]);
            if (File.Exists(candidate) || Directory.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }

    private sealed record SeededHarness(VinDecodeApplicationService Decode, VehicleAdminService Admin);

    private sealed class GoldenVinVector
    {
        public string Vin { get; init; } = string.Empty;
        public string Wmi { get; init; } = string.Empty;
        public string VdsPrefix { get; init; } = string.Empty;
        public int? ModelYear { get; init; }
        public string? Provenance { get; init; }
        public string? ExpectReason { get; init; }
    }

    private sealed class NoopTelemetry : ICheckEngineTelemetry
    {
        public void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties)
        {
        }
    }
}
