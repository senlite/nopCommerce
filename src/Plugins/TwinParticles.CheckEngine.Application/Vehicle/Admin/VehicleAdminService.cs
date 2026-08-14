using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Application.Vehicle.Admin;

public sealed class VehicleAdminService
{
    private readonly IVehicleAdminRepository _repository;
    private readonly IVehicleSeedLoader _seedLoader;
    private readonly ICheckEngineAuditService? _auditService;
    private readonly IVehicleAliasCache? _aliasCache;

    public VehicleAdminService(
        IVehicleAdminRepository repository,
        IVehicleSeedLoader seedLoader,
        ICheckEngineAuditService? auditService = null,
        IVehicleAliasCache? aliasCache = null)
    {
        _repository = repository;
        _seedLoader = seedLoader;
        _auditService = auditService;
        _aliasCache = aliasCache;
    }

    public Task<IReadOnlyList<VehicleMake>> GetMakesAsync(CancellationToken cancellationToken) => _repository.GetMakesAsync(cancellationToken);
    public Task<VehicleMake?> GetMakeByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetMakeByIdAsync(id, cancellationToken);

    public Task CreateMakeAsync(VehicleMake entity, CancellationToken cancellationToken)
    {
        EnsureMakeInvariant(entity);
        return _repository.CreateMakeAsync(entity, cancellationToken);
    }

    public Task UpdateMakeAsync(VehicleMake entity, CancellationToken cancellationToken)
    {
        EnsureMakeInvariant(entity);
        return _repository.UpdateMakeAsync(entity, cancellationToken);
    }

    public async Task DeleteMakeAsync(int id, CancellationToken cancellationToken)
    {
        if ((await _repository.GetModelsAsync(cancellationToken)).Any(model => model.MakeId == id))
            throw new InvalidOperationException("vehicle.make.archive_required");

        await _repository.DeleteMakeAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(CancellationToken cancellationToken) => _repository.GetModelsAsync(cancellationToken);
    public Task<VehicleModel?> GetModelByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetModelByIdAsync(id, cancellationToken);

    public Task CreateModelAsync(VehicleModel entity, CancellationToken cancellationToken)
    {
        EnsureModelInvariant(entity);
        return _repository.CreateModelAsync(entity, cancellationToken);
    }

    public Task UpdateModelAsync(VehicleModel entity, CancellationToken cancellationToken)
    {
        EnsureModelInvariant(entity);
        return _repository.UpdateModelAsync(entity, cancellationToken);
    }

    public async Task DeleteModelAsync(int id, CancellationToken cancellationToken)
    {
        if ((await _repository.GetGenerationsAsync(cancellationToken)).Any(generation => generation.ModelId == id))
            throw new InvalidOperationException("vehicle.model.archive_required");

        await _repository.DeleteModelAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<VehicleGeneration>> GetGenerationsAsync(CancellationToken cancellationToken) => _repository.GetGenerationsAsync(cancellationToken);
    public Task<VehicleGeneration?> GetGenerationByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetGenerationByIdAsync(id, cancellationToken);

    public Task CreateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken)
    {
        EnsureGenerationInvariant(entity);
        return _repository.CreateGenerationAsync(entity, cancellationToken);
    }

    public Task UpdateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken)
    {
        EnsureGenerationInvariant(entity);
        return _repository.UpdateGenerationAsync(entity, cancellationToken);
    }

    public async Task DeleteGenerationAsync(int id, CancellationToken cancellationToken)
    {
        var hasBodies = (await _repository.GetBodiesAsync(cancellationToken)).Any(body => body.GenerationId == id);
        var hasConfigurations = (await _repository.GetConfigurationsAsync(cancellationToken)).Any(configuration => configuration.GenerationId == id);
        if (hasBodies || hasConfigurations)
            throw new InvalidOperationException("vehicle.generation.archive_required");

        await _repository.DeleteGenerationAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<VehicleBody>> GetBodiesAsync(CancellationToken cancellationToken) => _repository.GetBodiesAsync(cancellationToken);
    public Task<VehicleBody?> GetBodyByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetBodyByIdAsync(id, cancellationToken);

    public Task CreateBodyAsync(VehicleBody entity, CancellationToken cancellationToken)
    {
        EnsureBodyInvariant(entity);
        return _repository.CreateBodyAsync(entity, cancellationToken);
    }

    public Task UpdateBodyAsync(VehicleBody entity, CancellationToken cancellationToken)
    {
        EnsureBodyInvariant(entity);
        return _repository.UpdateBodyAsync(entity, cancellationToken);
    }

    public Task DeleteBodyAsync(int id, CancellationToken cancellationToken) => _repository.DeleteBodyAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleEngine>> GetEnginesAsync(CancellationToken cancellationToken) => _repository.GetEnginesAsync(cancellationToken);
    public Task<VehicleEngine?> GetEngineByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetEngineByIdAsync(id, cancellationToken);

    public Task CreateEngineAsync(VehicleEngine entity, CancellationToken cancellationToken)
    {
        EnsureEngineInvariant(entity);
        return _repository.CreateEngineAsync(entity, cancellationToken);
    }

    public Task UpdateEngineAsync(VehicleEngine entity, CancellationToken cancellationToken)
    {
        EnsureEngineInvariant(entity);
        return _repository.UpdateEngineAsync(entity, cancellationToken);
    }

    public Task DeleteEngineAsync(int id, CancellationToken cancellationToken) => _repository.DeleteEngineAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleMarket>> GetMarketsAsync(CancellationToken cancellationToken) => _repository.GetMarketsAsync(cancellationToken);
    public Task<VehicleMarket?> GetMarketByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetMarketByIdAsync(id, cancellationToken);

    public Task CreateMarketAsync(VehicleMarket entity, CancellationToken cancellationToken)
    {
        EnsureMarketInvariant(entity);
        return _repository.CreateMarketAsync(entity, cancellationToken);
    }

    public Task UpdateMarketAsync(VehicleMarket entity, CancellationToken cancellationToken)
    {
        EnsureMarketInvariant(entity);
        return _repository.UpdateMarketAsync(entity, cancellationToken);
    }

    public Task DeleteMarketAsync(int id, CancellationToken cancellationToken) => _repository.DeleteMarketAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleConfiguration>> GetConfigurationsAsync(CancellationToken cancellationToken) => _repository.GetConfigurationsAsync(cancellationToken);
    public Task<VehicleConfiguration?> GetConfigurationByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetConfigurationByIdAsync(id, cancellationToken);

    public Task CreateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken)
    {
        EnsureConfigurationInvariant(entity);
        return _repository.CreateConfigurationAsync(entity, cancellationToken);
    }

    public Task UpdateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken)
    {
        EnsureConfigurationInvariant(entity);
        return _repository.UpdateConfigurationAsync(entity, cancellationToken);
    }

    public Task DeleteConfigurationAsync(int id, CancellationToken cancellationToken) => _repository.DeleteConfigurationAsync(id, cancellationToken);

    public Task<IReadOnlyList<VehicleAlias>> GetAliasesAsync(CancellationToken cancellationToken) => _repository.GetAliasesAsync(cancellationToken);
    public Task<VehicleAlias?> GetAliasByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetAliasByIdAsync(id, cancellationToken);

    public Task CreateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken)
    {
        EnsureAliasInvariant(entity);
        return _repository.CreateAliasAsync(entity, cancellationToken);
    }

    public Task UpdateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken)
    {
        EnsureAliasInvariant(entity);
        return _repository.UpdateAliasAsync(entity, cancellationToken);
    }

    public Task DeleteAliasAsync(int id, CancellationToken cancellationToken) => _repository.DeleteAliasAsync(id, cancellationToken);

    public Task<VehicleSeedLoadResult> SeedAsync(CancellationToken cancellationToken) => _seedLoader.SeedAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, string>> GetConfigurationDisplayLabelsAsync(
        IEnumerable<int> configurationIds,
        CancellationToken cancellationToken)
    {
        var ids = configurationIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<int, string>();

        var labels = new Dictionary<int, string>();
        foreach (var configurationId in ids)
        {
            var label = await GetConfigurationDisplayLabelAsync(configurationId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(label))
                labels[configurationId] = label;
        }

        return labels;
    }

    public async Task<string?> GetConfigurationDisplayLabelAsync(int configurationId, CancellationToken cancellationToken)
    {
        var configuration = await _repository.GetConfigurationByIdAsync(configurationId, cancellationToken);
        if (configuration is null || !configuration.IsActive)
            return null;

        var generation = await _repository.GetGenerationByIdAsync(configuration.GenerationId, cancellationToken);
        if (generation is null || !generation.IsActive)
            return null;

        var model = await _repository.GetModelByIdAsync(generation.ModelId, cancellationToken);
        if (model is null || !model.IsActive)
            return null;

        var make = await _repository.GetMakeByIdAsync(model.MakeId, cancellationToken);
        if (make is null || !make.IsActive)
            return null;

        return string.Join(
            " ",
            new[] { make.Name, model.Name, generation.Code, configuration.TrimName }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    public async Task<VehicleLifecycleResult> ArchiveMakeAsync(
        int makeId,
        string actor,
        CancellationToken cancellationToken)
    {
        var make = await _repository.GetMakeByIdAsync(makeId, cancellationToken);
        if (make is null)
            return VehicleLifecycleResult.Fail("vehicle.make.not_found");

        if (!make.IsActive)
            return VehicleLifecycleResult.Ok();

        var before = JsonSerializer.Serialize(new { make.Id, make.Code, make.IsActive });
        make.IsActive = false;
        await _repository.UpdateMakeAsync(make, cancellationToken);
        await AppendAuditAsync(actor, "vehicle.make.archive", "VehicleMake", make.Id, before,
            JsonSerializer.Serialize(new { make.Id, make.Code, make.IsActive }), cancellationToken);
        await InvalidateAliasLocalesAsync("make", make.Id, cancellationToken);
        return VehicleLifecycleResult.Ok();
    }

    public async Task<VehicleLifecycleResult> ArchiveModelAsync(
        int modelId,
        string actor,
        CancellationToken cancellationToken)
    {
        var model = await _repository.GetModelByIdAsync(modelId, cancellationToken);
        if (model is null)
            return VehicleLifecycleResult.Fail("vehicle.model.not_found");

        if (!model.IsActive)
            return VehicleLifecycleResult.Ok();

        var before = JsonSerializer.Serialize(new { model.Id, model.MakeId, model.Code, model.IsActive });
        model.IsActive = false;
        await _repository.UpdateModelAsync(model, cancellationToken);
        await AppendAuditAsync(actor, "vehicle.model.archive", "VehicleModel", model.Id, before,
            JsonSerializer.Serialize(new { model.Id, model.MakeId, model.Code, model.IsActive }), cancellationToken);
        await InvalidateAliasLocalesAsync("model", model.Id, cancellationToken);
        return VehicleLifecycleResult.Ok();
    }

    public async Task<VehicleLifecycleResult> MergeMakeAsync(
        int sourceMakeId,
        int targetMakeId,
        string actor,
        CancellationToken cancellationToken)
    {
        if (sourceMakeId <= 0 || targetMakeId <= 0 || sourceMakeId == targetMakeId)
            return VehicleLifecycleResult.Fail("vehicle.make.merge.invalid");

        var source = await _repository.GetMakeByIdAsync(sourceMakeId, cancellationToken);
        var target = await _repository.GetMakeByIdAsync(targetMakeId, cancellationToken);
        if (source is null || target is null)
            return VehicleLifecycleResult.Fail("vehicle.make.merge.not_found");
        if (!target.IsActive)
            return VehicleLifecycleResult.Fail("vehicle.make.merge.target_inactive");

        var result = await _repository.MergeMakeAsync(sourceMakeId, targetMakeId, cancellationToken);
        if (!result.Success)
            return VehicleLifecycleResult.Fail(result.ErrorCode ?? "vehicle.make.merge.failed");

        await AppendAuditAsync(actor, "vehicle.make.merge", "VehicleMake", sourceMakeId,
            JsonSerializer.Serialize(new { SourceMakeId = sourceMakeId, SourceCode = source.Code }),
            JsonSerializer.Serialize(new
            {
                SourceMakeId = sourceMakeId,
                TargetMakeId = targetMakeId,
                TargetCode = target.Code,
                result.MovedChildren,
                result.MovedAliases
            }),
            cancellationToken);
        await InvalidateAliasLocalesAsync("make", sourceMakeId, cancellationToken);
        await InvalidateAliasLocalesAsync("make", targetMakeId, cancellationToken);
        return VehicleLifecycleResult.Ok(result.MovedChildren, result.MovedAliases);
    }

    public async Task<VehicleLifecycleResult> MergeModelAsync(
        int sourceModelId,
        int targetModelId,
        string actor,
        CancellationToken cancellationToken)
    {
        if (sourceModelId <= 0 || targetModelId <= 0 || sourceModelId == targetModelId)
            return VehicleLifecycleResult.Fail("vehicle.model.merge.invalid");

        var source = await _repository.GetModelByIdAsync(sourceModelId, cancellationToken);
        var target = await _repository.GetModelByIdAsync(targetModelId, cancellationToken);
        if (source is null || target is null)
            return VehicleLifecycleResult.Fail("vehicle.model.merge.not_found");
        if (source.MakeId != target.MakeId)
            return VehicleLifecycleResult.Fail("vehicle.model.merge.cross_make");
        if (!target.IsActive)
            return VehicleLifecycleResult.Fail("vehicle.model.merge.target_inactive");

        var result = await _repository.MergeModelAsync(sourceModelId, targetModelId, cancellationToken);
        if (!result.Success)
            return VehicleLifecycleResult.Fail(result.ErrorCode ?? "vehicle.model.merge.failed");

        await AppendAuditAsync(actor, "vehicle.model.merge", "VehicleModel", sourceModelId,
            JsonSerializer.Serialize(new { SourceModelId = sourceModelId, SourceCode = source.Code }),
            JsonSerializer.Serialize(new
            {
                SourceModelId = sourceModelId,
                TargetModelId = targetModelId,
                TargetCode = target.Code,
                result.MovedChildren,
                result.MovedAliases
            }),
            cancellationToken);
        await InvalidateAliasLocalesAsync("model", sourceModelId, cancellationToken);
        await InvalidateAliasLocalesAsync("model", targetModelId, cancellationToken);
        return VehicleLifecycleResult.Ok(result.MovedChildren, result.MovedAliases);
    }

    public async Task<VehicleLifecycleResult> ArchiveGenerationAsync(
        int generationId,
        string actor,
        CancellationToken cancellationToken)
    {
        var generation = await _repository.GetGenerationByIdAsync(generationId, cancellationToken);
        if (generation is null)
            return VehicleLifecycleResult.Fail("vehicle.generation.not_found");

        if (!generation.IsActive)
            return VehicleLifecycleResult.Ok();

        var before = JsonSerializer.Serialize(new { generation.Id, generation.ModelId, generation.Code, generation.IsActive });
        generation.IsActive = false;
        await _repository.UpdateGenerationAsync(generation, cancellationToken);
        await AppendAuditAsync(actor, "vehicle.generation.archive", "VehicleGeneration", generation.Id, before,
            JsonSerializer.Serialize(new { generation.Id, generation.ModelId, generation.Code, generation.IsActive }), cancellationToken);
        await InvalidateAliasLocalesAsync("generation", generation.Id, cancellationToken);
        return VehicleLifecycleResult.Ok();
    }

    public async Task<VehicleLifecycleResult> MergeGenerationAsync(
        int sourceGenerationId,
        int targetGenerationId,
        string actor,
        CancellationToken cancellationToken)
    {
        if (sourceGenerationId <= 0 || targetGenerationId <= 0 || sourceGenerationId == targetGenerationId)
            return VehicleLifecycleResult.Fail("vehicle.generation.merge.invalid");

        var source = await _repository.GetGenerationByIdAsync(sourceGenerationId, cancellationToken);
        var target = await _repository.GetGenerationByIdAsync(targetGenerationId, cancellationToken);
        if (source is null || target is null)
            return VehicleLifecycleResult.Fail("vehicle.generation.merge.not_found");
        if (source.ModelId != target.ModelId)
            return VehicleLifecycleResult.Fail("vehicle.generation.merge.cross_model");
        if (!target.IsActive)
            return VehicleLifecycleResult.Fail("vehicle.generation.merge.target_inactive");

        var result = await _repository.MergeGenerationAsync(sourceGenerationId, targetGenerationId, cancellationToken);
        if (!result.Success)
            return VehicleLifecycleResult.Fail(result.ErrorCode ?? "vehicle.generation.merge.failed");

        await AppendAuditAsync(actor, "vehicle.generation.merge", "VehicleGeneration", sourceGenerationId,
            JsonSerializer.Serialize(new { SourceGenerationId = sourceGenerationId, SourceCode = source.Code }),
            JsonSerializer.Serialize(new
            {
                SourceGenerationId = sourceGenerationId,
                TargetGenerationId = targetGenerationId,
                TargetCode = target.Code,
                result.MovedChildren,
                result.MovedAliases
            }),
            cancellationToken);
        await InvalidateAliasLocalesAsync("generation", sourceGenerationId, cancellationToken);
        await InvalidateAliasLocalesAsync("generation", targetGenerationId, cancellationToken);
        return VehicleLifecycleResult.Ok(result.MovedChildren, result.MovedAliases);
    }

    private async Task AppendAuditAsync(
        string actor,
        string action,
        string entityType,
        int entityId,
        string? beforeJson,
        string? afterJson,
        CancellationToken cancellationToken)
    {
        if (_auditService is null)
            return;

        await _auditService.AppendAsync(
            string.IsNullOrWhiteSpace(actor) ? "system" : actor,
            action,
            entityType,
            entityId.ToString(),
            beforeJson,
            afterJson,
            cancellationToken);
    }

    private async Task InvalidateAliasLocalesAsync(
        string nodeType,
        int nodeId,
        CancellationToken cancellationToken)
    {
        if (_aliasCache is null)
            return;

        var locales = (await _repository.GetAliasesAsync(cancellationToken))
            .Where(alias => alias.NodeType == nodeType && alias.NodeId == nodeId)
            .Select(alias => alias.Locale)
            .Distinct();

        foreach (var locale in locales)
            await _aliasCache.InvalidateAsync(locale, cancellationToken);
    }

    private static void EnsureMakeInvariant(VehicleMake entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("vehicle.make.code_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("vehicle.make.name_required", nameof(entity));
    }

    private static void EnsureModelInvariant(VehicleModel entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (entity.MakeId <= 0)
            throw new ArgumentException("vehicle.model.make_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("vehicle.model.code_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("vehicle.model.name_required", nameof(entity));
    }

    private static void EnsureGenerationInvariant(VehicleGeneration entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (entity.ModelId <= 0)
            throw new ArgumentException("vehicle.generation.model_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("vehicle.generation.code_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("vehicle.generation.name_required", nameof(entity));
        if (entity.StartYear <= 0)
            throw new ArgumentException("vehicle.generation.start_year_required", nameof(entity));
        if (entity.EndYear.HasValue && entity.EndYear.Value < entity.StartYear)
            throw new ArgumentException("vehicle.generation.invalid_year_range", nameof(entity));
    }

    private static void EnsureBodyInvariant(VehicleBody entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (entity.GenerationId <= 0)
            throw new ArgumentException("vehicle.body.generation_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("vehicle.body.code_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("vehicle.body.name_required", nameof(entity));
    }

    private static void EnsureEngineInvariant(VehicleEngine entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (entity.BodyId <= 0)
            throw new ArgumentException("vehicle.engine.body_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("vehicle.engine.code_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("vehicle.engine.name_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.FuelType))
            throw new ArgumentException("vehicle.engine.fuel_required", nameof(entity));
        if (entity.DisplacementCc <= 0)
            throw new ArgumentException("vehicle.engine.displacement_required", nameof(entity));
        if (entity.PowerHp <= 0)
            throw new ArgumentException("vehicle.engine.power_required", nameof(entity));
    }

    private static void EnsureMarketInvariant(VehicleMarket entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("vehicle.market.code_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("vehicle.market.name_required", nameof(entity));
    }

    private static void EnsureConfigurationInvariant(VehicleConfiguration entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (entity.GenerationId <= 0)
            throw new ArgumentException("vehicle.configuration.generation_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.TrimName))
            throw new ArgumentException("vehicle.configuration.trim_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Fingerprint))
            throw new ArgumentException("vehicle.configuration.fingerprint_required", nameof(entity));
        if (entity.ProductionFromYear.HasValue && entity.ProductionFromYear.Value <= 0)
            throw new ArgumentException("vehicle.configuration.production_from_invalid", nameof(entity));
        if (entity.ProductionToYear.HasValue && entity.ProductionToYear.Value <= 0)
            throw new ArgumentException("vehicle.configuration.production_to_invalid", nameof(entity));
        if (entity.ProductionFromYear.HasValue && entity.ProductionToYear.HasValue && entity.ProductionToYear.Value < entity.ProductionFromYear.Value)
            throw new ArgumentException("vehicle.configuration.invalid_year_range", nameof(entity));
    }

    private static void EnsureAliasInvariant(VehicleAlias entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.NodeType))
            throw new ArgumentException("vehicle.alias.node_type_required", nameof(entity));
        if (entity.NodeId <= 0)
            throw new ArgumentException("vehicle.alias.node_id_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Locale))
            throw new ArgumentException("vehicle.alias.locale_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.AliasText))
            throw new ArgumentException("vehicle.alias.text_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.NormalizedAlias))
            throw new ArgumentException("vehicle.alias.normalized_required", nameof(entity));
    }
}
