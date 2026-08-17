using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Services;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class NaturalLanguageIntentParserHeuristicTests
{
    [Test]
    public async Task ParseAsync_Should_Extract_Make_Model_And_Part_Terms()
    {
        var parser = new NaturalLanguageIntentParser();
        var intent = await parser.ParseAsync("2015 BMW 320i oil filter", "en", CancellationToken.None);

        intent.Make.Should().Be("BMW");
        intent.Model.Should().Be("320i");
        intent.ModelYear.Should().Be(2015);
        intent.PartTerms.Should().Contain("oil filter");
    }

    [Test]
    public async Task ParseAsync_Should_Extract_Chassis_Code_And_Part_Phrase()
    {
        var parser = new NaturalLanguageIntentParser();
        var intent = await parser.ParseAsync("BMW F30 water pump", "en", CancellationToken.None);

        intent.Make.Should().Be("BMW");
        intent.Model.Should().Be("F30");
        intent.PartTerms.Should().Contain("water pump");
    }

    [Test]
    public async Task ParseAsync_Should_Extract_Oem_Number_Heuristically()
    {
        var parser = new NaturalLanguageIntentParser();
        var intent = await parser.ParseAsync("filter 11 42 7 534 376", "en", CancellationToken.None);

        intent.OemNumber.Should().NotBeNullOrWhiteSpace();
        intent.OemNumber.Should().Contain("11");
    }

    [Test]
    public async Task ParseAsync_Should_Keep_Arabic_Part_Phrase()
    {
        var parser = new NaturalLanguageIntentParser();
        var intent = await parser.ParseAsync("BMW فلتر زيت", "ar", CancellationToken.None);

        intent.Make.Should().Be("BMW");
        intent.PartTerms.Should().Contain("فلتر زيت");
    }

    [Test]
    public async Task ParseAsync_Should_Enrich_Vehicle_Configuration_Id_Via_Alias_Resolver()
    {
        var aliasService = new VehicleAliasApplicationService(
            new NoopWriteRepository(),
            new FakeReadRepository
            {
                Result =
                [
                    new VehicleAliasSearchItem { NodeType = "configuration", NodeId = 501, Locale = "en", AliasText = "BMW F30" }
                ]
            },
            new NoopCache(),
            new NoopNormalizer(),
            new NoopSanitizer(),
            new NoopClock(),
            new NoopTelemetry());

        var parser = new NaturalLanguageIntentParser(vehicleResolver: new SearchIntentVehicleResolver(aliasService));
        var intent = await parser.ParseAsync("BMW F30 water pump", "en", CancellationToken.None);

        intent.VehicleConfigurationId.Should().Be(501);
        intent.PartTerms.Should().Contain("water pump");
    }

    private sealed class NoopWriteRepository : IVehicleAliasWriteRepository
    {
        public Task UpsertAsync(VehicleAlias alias, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeReadRepository : IVehicleAliasReadRepository
    {
        public IReadOnlyList<VehicleAliasSearchItem> Result { get; set; } = [];

        public Task<IReadOnlyList<VehicleAliasSearchItem>> SearchAsync(VehicleAliasSearchCriteria criteria, CancellationToken cancellationToken) =>
            Task.FromResult(Result);
    }

    private sealed class NoopCache : IVehicleAliasCache
    {
        public Task<IReadOnlyList<VehicleAliasSearchItem>?> GetAsync(string term, string locale, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VehicleAliasSearchItem>?>(null);

        public Task SetAsync(string term, string locale, int take, IReadOnlyList<VehicleAliasSearchItem> items, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task InvalidateAsync(string locale, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoopNormalizer : IVehicleAliasNormalizationService
    {
        public string Normalize(string aliasText, string locale) => aliasText;
    }

    private sealed class NoopSanitizer : ICheckEngineInputSanitizer
    {
        public string SanitizeAlias(string aliasText) => aliasText;
        public string SanitizeFreeText(string text) => text;
    }

    private sealed class NoopClock : ICheckEngineClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class NoopTelemetry : ICheckEngineTelemetry
    {
        public void TrackEvent(string name, IReadOnlyDictionary<string, object?> properties) { }
    }
}
