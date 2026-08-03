using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Common.Results;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Commands;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Queries;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Application.Vehicle.Aliases.Services;

public sealed class VehicleAliasApplicationService
{
    private readonly ICheckEngineClock _clock;
    private readonly ICheckEngineInputSanitizer _inputSanitizer;
    private readonly ICheckEngineTelemetry _telemetry;
    private readonly IVehicleAliasCache _vehicleAliasCache;
    private readonly IVehicleAliasNormalizationService _vehicleAliasNormalizationService;
    private readonly IVehicleAliasReadRepository _vehicleAliasReadRepository;
    private readonly IVehicleAliasWriteRepository _vehicleAliasWriteRepository;

    public VehicleAliasApplicationService(
        IVehicleAliasWriteRepository vehicleAliasWriteRepository,
        IVehicleAliasReadRepository vehicleAliasReadRepository,
        IVehicleAliasCache vehicleAliasCache,
        IVehicleAliasNormalizationService vehicleAliasNormalizationService,
        ICheckEngineInputSanitizer inputSanitizer,
        ICheckEngineClock clock,
        ICheckEngineTelemetry telemetry)
    {
        _vehicleAliasWriteRepository = vehicleAliasWriteRepository;
        _vehicleAliasReadRepository = vehicleAliasReadRepository;
        _vehicleAliasCache = vehicleAliasCache;
        _vehicleAliasNormalizationService = vehicleAliasNormalizationService;
        _inputSanitizer = inputSanitizer;
        _clock = clock;
        _telemetry = telemetry;
    }

    public async Task<CommandResult> UpsertAsync(UpsertVehicleAliasCommand command, CancellationToken cancellationToken)
    {
        var validator = new UpsertVehicleAliasCommandValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return CommandResult.Fail("ce.alias.validation_failed");

        var sanitizedAlias = _inputSanitizer.SanitizeAlias(command.AliasText);
        var normalizedAlias = _vehicleAliasNormalizationService.Normalize(sanitizedAlias, command.Locale);

        var alias = new VehicleAlias
        {
            NodeType = command.NodeType,
            NodeId = command.NodeId,
            Locale = command.Locale,
            AliasText = sanitizedAlias,
            NormalizedAlias = normalizedAlias
        };

        var startedAt = _clock.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        await _vehicleAliasWriteRepository.UpsertAsync(alias, cancellationToken);
        await _vehicleAliasCache.InvalidateAsync(command.Locale, cancellationToken);

        stopwatch.Stop();

        _telemetry.TrackEvent("checkengine.alias.upsert", new Dictionary<string, object?>
        {
            ["nodeType"] = command.NodeType,
            ["nodeId"] = command.NodeId,
            ["locale"] = command.Locale,
            ["durationMs"] = stopwatch.ElapsedMilliseconds,
            ["startedAtUtc"] = startedAt
        });

        return CommandResult.Ok();
    }

    public async Task<IReadOnlyList<VehicleAliasDto>> SearchAsync(SearchVehicleAliasesQuery query, CancellationToken cancellationToken)
    {
        var cached = await _vehicleAliasCache.GetAsync(query.Term, query.Locale, query.Take, cancellationToken);
        if (cached is not null)
            return Map(cached);

        var criteria = new VehicleAliasSearchCriteria
        {
            Term = query.Term,
            Locale = query.Locale,
            Take = query.Take
        };

        var items = await _vehicleAliasReadRepository.SearchAsync(criteria, cancellationToken);
        await _vehicleAliasCache.SetAsync(query.Term, query.Locale, query.Take, items, cancellationToken);

        _telemetry.TrackEvent("checkengine.alias.search", new Dictionary<string, object?>
        {
            ["termLength"] = query.Term.Length,
            ["locale"] = query.Locale,
            ["take"] = query.Take,
            ["cache"] = "miss"
        });

        return Map(items);
    }

    private static IReadOnlyList<VehicleAliasDto> Map(IReadOnlyList<VehicleAliasSearchItem> items)
    {
        var mapped = new List<VehicleAliasDto>(items.Count);
        foreach (var item in items)
        {
            mapped.Add(new VehicleAliasDto
            {
                NodeType = item.NodeType,
                NodeId = item.NodeId,
                Locale = item.Locale,
                AliasText = item.AliasText
            });
        }

        return mapped;
    }
}
