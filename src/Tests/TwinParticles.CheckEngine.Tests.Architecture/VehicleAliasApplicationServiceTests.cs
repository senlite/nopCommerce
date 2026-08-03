using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Commands;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Queries;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Services;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VehicleAliasApplicationServiceTests
{
    [Test]
    public async Task UpsertAsync_Should_Return_ValidationError_When_Command_Is_Invalid()
    {
        var writeRepository = new FakeWriteRepository();
        var readRepository = new FakeReadRepository();
        var cache = new FakeCache();
        var normalization = new FakeNormalizationService();
        var sanitizer = new FakeSanitizer();
        var clock = new FakeClock();
        var telemetry = new FakeTelemetry();

        var service = CreateService(writeRepository, readRepository, cache, normalization, sanitizer, clock, telemetry);

        var command = new UpsertVehicleAliasCommand
        {
            NodeType = string.Empty,
            NodeId = 0,
            Locale = string.Empty,
            AliasText = string.Empty
        };

        var result = await service.UpsertAsync(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ce.alias.validation_failed");
        writeRepository.UpsertCalls.Should().Be(0);
        cache.InvalidateCalls.Should().Be(0);
        telemetry.Events.Count.Should().Be(0);
    }

    [Test]
    public async Task UpsertAsync_Should_Persist_Sanitized_And_Normalized_Alias_When_Command_Is_Valid()
    {
        var writeRepository = new FakeWriteRepository();
        var readRepository = new FakeReadRepository();
        var cache = new FakeCache();
        var normalization = new FakeNormalizationService();
        var sanitizer = new FakeSanitizer();
        var clock = new FakeClock();
        var telemetry = new FakeTelemetry();

        var service = CreateService(writeRepository, readRepository, cache, normalization, sanitizer, clock, telemetry);

        var command = new UpsertVehicleAliasCommand
        {
            NodeType = "model",
            NodeId = 101,
            Locale = "en",
            AliasText = "  Civic  "
        };

        var result = await service.UpsertAsync(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        writeRepository.UpsertCalls.Should().Be(1);
        writeRepository.LastAlias.Should().NotBeNull();
        writeRepository.LastAlias!.AliasText.Should().Be("san:Civic");
        writeRepository.LastAlias.NormalizedAlias.Should().Be("norm:en:san:Civic");

        cache.InvalidateCalls.Should().Be(1);
        cache.LastInvalidatedLocale.Should().Be("en");

        telemetry.Events.Count.Should().Be(1);
        telemetry.Events[0].EventName.Should().Be("checkengine.alias.upsert");
    }

    [Test]
    public async Task SearchAsync_Should_Return_Cached_Result_When_Available()
    {
        var writeRepository = new FakeWriteRepository();
        var readRepository = new FakeReadRepository();
        var cache = new FakeCache
        {
            Cached = new List<VehicleAliasSearchItem>
            {
                new()
                {
                    NodeType = "model",
                    NodeId = 77,
                    Locale = "en",
                    AliasText = "Corolla"
                }
            }
        };

        var normalization = new FakeNormalizationService();
        var sanitizer = new FakeSanitizer();
        var clock = new FakeClock();
        var telemetry = new FakeTelemetry();

        var service = CreateService(writeRepository, readRepository, cache, normalization, sanitizer, clock, telemetry);

        var result = await service.SearchAsync(new SearchVehicleAliasesQuery
        {
            Term = "cor",
            Locale = "en",
            Take = 5
        }, CancellationToken.None);

        result.Count.Should().Be(1);
        result[0].AliasText.Should().Be("Corolla");
        readRepository.SearchCalls.Should().Be(0);
        cache.SetCalls.Should().Be(0);
    }

    [Test]
    public async Task SearchAsync_Should_Read_And_Cache_Result_On_Miss()
    {
        var writeRepository = new FakeWriteRepository();
        var readRepository = new FakeReadRepository
        {
            Result = new List<VehicleAliasSearchItem>
            {
                new()
                {
                    NodeType = "model",
                    NodeId = 88,
                    Locale = "en",
                    AliasText = "Camry"
                }
            }
        };

        var cache = new FakeCache();
        var normalization = new FakeNormalizationService();
        var sanitizer = new FakeSanitizer();
        var clock = new FakeClock();
        var telemetry = new FakeTelemetry();

        var service = CreateService(writeRepository, readRepository, cache, normalization, sanitizer, clock, telemetry);

        var result = await service.SearchAsync(new SearchVehicleAliasesQuery
        {
            Term = "cam",
            Locale = "en",
            Take = 10
        }, CancellationToken.None);

        result.Count.Should().Be(1);
        result[0].AliasText.Should().Be("Camry");
        readRepository.SearchCalls.Should().Be(1);
        cache.SetCalls.Should().Be(1);
        telemetry.Events.Count.Should().Be(1);
        telemetry.Events[0].EventName.Should().Be("checkengine.alias.search");
    }

    private static VehicleAliasApplicationService CreateService(
        IVehicleAliasWriteRepository writeRepository,
        IVehicleAliasReadRepository readRepository,
        IVehicleAliasCache cache,
        IVehicleAliasNormalizationService normalization,
        ICheckEngineInputSanitizer sanitizer,
        ICheckEngineClock clock,
        ICheckEngineTelemetry telemetry)
    {
        return new VehicleAliasApplicationService(
            writeRepository,
            readRepository,
            cache,
            normalization,
            sanitizer,
            clock,
            telemetry);
    }

    private sealed class FakeWriteRepository : IVehicleAliasWriteRepository
    {
        public int UpsertCalls { get; private set; }

        public VehicleAlias? LastAlias { get; private set; }

        public Task UpsertAsync(VehicleAlias alias, CancellationToken cancellationToken)
        {
            UpsertCalls++;
            LastAlias = alias;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReadRepository : IVehicleAliasReadRepository
    {
        public int SearchCalls { get; private set; }

        public IReadOnlyList<VehicleAliasSearchItem> Result { get; set; } = new List<VehicleAliasSearchItem>();

        public Task<IReadOnlyList<VehicleAliasSearchItem>> SearchAsync(VehicleAliasSearchCriteria criteria, CancellationToken cancellationToken)
        {
            SearchCalls++;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeCache : IVehicleAliasCache
    {
        public int SetCalls { get; private set; }

        public int InvalidateCalls { get; private set; }

        public string? LastInvalidatedLocale { get; private set; }

        public IReadOnlyList<VehicleAliasSearchItem>? Cached { get; set; }

        public Task<IReadOnlyList<VehicleAliasSearchItem>?> GetAsync(string term, string locale, int take, CancellationToken cancellationToken)
        {
            return Task.FromResult(Cached);
        }

        public Task SetAsync(string term, string locale, int take, IReadOnlyList<VehicleAliasSearchItem> items, CancellationToken cancellationToken)
        {
            SetCalls++;
            Cached = items;
            return Task.CompletedTask;
        }

        public Task InvalidateAsync(string locale, CancellationToken cancellationToken)
        {
            InvalidateCalls++;
            LastInvalidatedLocale = locale;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeNormalizationService : IVehicleAliasNormalizationService
    {
        public string Normalize(string text, string locale)
        {
            return $"norm:{locale}:{text}";
        }
    }

    private sealed class FakeSanitizer : ICheckEngineInputSanitizer
    {
        public string SanitizeAlias(string value)
        {
            return $"san:{value.Trim()}";
        }
    }

    private sealed class FakeClock : ICheckEngineClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 2, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeTelemetry : ICheckEngineTelemetry
    {
        public List<TelemetryEvent> Events { get; } = new();

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties)
        {
            Events.Add(new TelemetryEvent(eventName, properties));
        }
    }

    private sealed record TelemetryEvent(string EventName, IReadOnlyDictionary<string, object?> Properties);
}
