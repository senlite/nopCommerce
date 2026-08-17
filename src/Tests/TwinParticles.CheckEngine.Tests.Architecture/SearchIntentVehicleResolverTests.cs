using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Services;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SearchIntentVehicleResolverTests
{
    [Test]
    public async Task ResolveConfigurationIdAsync_Should_Prefer_Configuration_Alias()
    {
        var aliasService = new VehicleAliasApplicationService(
            new NoopWriteRepository(),
            new FakeReadRepository
            {
                Result =
                [
                    new VehicleAliasSearchItem { NodeType = "model", NodeId = 10, Locale = "en", AliasText = "320i" },
                    new VehicleAliasSearchItem { NodeType = "configuration", NodeId = 222, Locale = "en", AliasText = "320i 2016" }
                ]
            },
            new NoopCache(),
            new NoopNormalizer(),
            new NoopSanitizer(),
            new NoopClock(),
            new NoopTelemetry());

        var resolver = new SearchIntentVehicleResolver(aliasService);
        var intent = new SearchIntent
        {
            Make = "BMW",
            Model = "320i",
            ModelYear = 2016,
            Locale = "en"
        };

        var configurationId = await resolver.ResolveConfigurationIdAsync(intent, CancellationToken.None);

        configurationId.Should().Be(222);
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
