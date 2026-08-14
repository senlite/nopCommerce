using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

public sealed class SqlVehicleAdminRepository : IVehicleAdminRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlVehicleAdminRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<IReadOnlyList<VehicleMake>> GetMakesAsync(CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleMake>("SELECT Id, Code, Name, IsActive FROM TP_CE_VehicleMake ORDER BY Name")).ToList();

    public async Task<VehicleMake?> GetMakeByIdAsync(int id, CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleMake>("SELECT Id, Code, Name, IsActive FROM TP_CE_VehicleMake WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id))).FirstOrDefault();

    public Task CreateMakeAsync(VehicleMake entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("INSERT INTO TP_CE_VehicleMake (Code, Name, IsActive) VALUES (@code,@name,@active)", new LinqToDB.Data.DataParameter("code", entity.Code), new LinqToDB.Data.DataParameter("name", entity.Name), new LinqToDB.Data.DataParameter("active", entity.IsActive));

    public Task UpdateMakeAsync(VehicleMake entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("UPDATE TP_CE_VehicleMake SET Code=@code, Name=@name, IsActive=@active WHERE Id=@id", new LinqToDB.Data.DataParameter("code", entity.Code), new LinqToDB.Data.DataParameter("name", entity.Name), new LinqToDB.Data.DataParameter("active", entity.IsActive), new LinqToDB.Data.DataParameter("id", entity.Id));

    public Task DeleteMakeAsync(int id, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("DELETE FROM TP_CE_VehicleMake WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id));

    public async Task<VehicleMergeRepositoryResult> MergeMakeAsync(
        int sourceMakeId,
        int targetMakeId,
        CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<MergeRow>(@"
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1
    FROM TP_CE_VehicleModel sourceModel
    INNER JOIN TP_CE_VehicleModel targetModel
        ON targetModel.MakeId = @targetMakeId
       AND targetModel.Code = sourceModel.Code
    WHERE sourceModel.MakeId = @sourceMakeId
)
BEGIN
    ROLLBACK TRANSACTION;
    SELECT CAST(0 AS bit) AS Success,
           CAST('vehicle.make.merge.model_code_conflict' AS nvarchar(128)) AS ErrorCode,
           0 AS MovedChildren,
           0 AS MovedAliases;
    RETURN;
END;

DECLARE @movedChildren int;
DECLARE @movedAliases int;

UPDATE TP_CE_VehicleModel SET MakeId=@targetMakeId WHERE MakeId=@sourceMakeId;
SET @movedChildren = @@ROWCOUNT;

UPDATE TP_CE_VehicleAlias SET NodeId=@targetMakeId WHERE NodeType='make' AND NodeId=@sourceMakeId;
SET @movedAliases = @@ROWCOUNT;

UPDATE TP_CE_VehicleMake SET IsActive=0 WHERE Id=@sourceMakeId;

COMMIT TRANSACTION;
SELECT CAST(1 AS bit) AS Success,
       CAST(NULL AS nvarchar(128)) AS ErrorCode,
       @movedChildren AS MovedChildren,
       @movedAliases AS MovedAliases;",
            new DataParameter("sourceMakeId", sourceMakeId),
            new DataParameter("targetMakeId", targetMakeId));
        return MapMerge(rows.Single());
    }

    public async Task<IReadOnlyList<VehicleModel>> GetModelsAsync(CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleModel>("SELECT Id, MakeId, Code, Name, IsActive FROM TP_CE_VehicleModel ORDER BY Name")).ToList();

    public async Task<VehicleModel?> GetModelByIdAsync(int id, CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleModel>("SELECT Id, MakeId, Code, Name, IsActive FROM TP_CE_VehicleModel WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id))).FirstOrDefault();

    public Task CreateModelAsync(VehicleModel entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("INSERT INTO TP_CE_VehicleModel (MakeId, Code, Name, IsActive) VALUES (@makeId,@code,@name,@active)", new LinqToDB.Data.DataParameter("makeId", entity.MakeId), new LinqToDB.Data.DataParameter("code", entity.Code), new LinqToDB.Data.DataParameter("name", entity.Name), new LinqToDB.Data.DataParameter("active", entity.IsActive));

    public Task UpdateModelAsync(VehicleModel entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("UPDATE TP_CE_VehicleModel SET MakeId=@makeId, Code=@code, Name=@name, IsActive=@active WHERE Id=@id", new LinqToDB.Data.DataParameter("makeId", entity.MakeId), new LinqToDB.Data.DataParameter("code", entity.Code), new LinqToDB.Data.DataParameter("name", entity.Name), new LinqToDB.Data.DataParameter("active", entity.IsActive), new LinqToDB.Data.DataParameter("id", entity.Id));

    public Task DeleteModelAsync(int id, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("DELETE FROM TP_CE_VehicleModel WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id));

    public async Task<VehicleMergeRepositoryResult> MergeModelAsync(
        int sourceModelId,
        int targetModelId,
        CancellationToken cancellationToken)
    {
        // Only Generation.ModelId and model-scoped aliases move. Every generation/configuration id
        // stays stable, so fitment claims, garage rows, SEO landings, and historical records retain
        // their existing foreign keys.
        var rows = await _dataProvider.QueryAsync<MergeRow>(@"
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1
    FROM TP_CE_VehicleGeneration sourceGeneration
    INNER JOIN TP_CE_VehicleGeneration targetGeneration
        ON targetGeneration.ModelId = @targetModelId
       AND targetGeneration.Code = sourceGeneration.Code
    WHERE sourceGeneration.ModelId = @sourceModelId
)
BEGIN
    ROLLBACK TRANSACTION;
    SELECT CAST(0 AS bit) AS Success,
           CAST('vehicle.model.merge.generation_code_conflict' AS nvarchar(128)) AS ErrorCode,
           0 AS MovedChildren,
           0 AS MovedAliases;
    RETURN;
END;

DECLARE @movedChildren int;
DECLARE @movedAliases int;

UPDATE TP_CE_VehicleGeneration SET ModelId=@targetModelId WHERE ModelId=@sourceModelId;
SET @movedChildren = @@ROWCOUNT;

UPDATE TP_CE_VehicleAlias SET NodeId=@targetModelId WHERE NodeType='model' AND NodeId=@sourceModelId;
SET @movedAliases = @@ROWCOUNT;

UPDATE TP_CE_VehicleModel SET IsActive=0 WHERE Id=@sourceModelId;

COMMIT TRANSACTION;
SELECT CAST(1 AS bit) AS Success,
       CAST(NULL AS nvarchar(128)) AS ErrorCode,
       @movedChildren AS MovedChildren,
       @movedAliases AS MovedAliases;",
            new DataParameter("sourceModelId", sourceModelId),
            new DataParameter("targetModelId", targetModelId));
        return MapMerge(rows.Single());
    }

    public async Task<IReadOnlyList<VehicleGeneration>> GetGenerationsAsync(CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleGeneration>("SELECT Id, ModelId, Code, Name, StartYear, EndYear, IsActive FROM TP_CE_VehicleGeneration ORDER BY Name")).ToList();

    public async Task<VehicleGeneration?> GetGenerationByIdAsync(int id, CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleGeneration>("SELECT Id, ModelId, Code, Name, StartYear, EndYear, IsActive FROM TP_CE_VehicleGeneration WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id))).FirstOrDefault();

    public Task CreateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("INSERT INTO TP_CE_VehicleGeneration (ModelId, Code, Name, StartYear, EndYear, IsActive) VALUES (@modelId,@code,@name,@startYear,@endYear,@active)", new LinqToDB.Data.DataParameter("modelId", entity.ModelId), new LinqToDB.Data.DataParameter("code", entity.Code), new LinqToDB.Data.DataParameter("name", entity.Name), new LinqToDB.Data.DataParameter("startYear", entity.StartYear), new LinqToDB.Data.DataParameter("endYear", entity.EndYear), new LinqToDB.Data.DataParameter("active", entity.IsActive));

    public Task UpdateGenerationAsync(VehicleGeneration entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("UPDATE TP_CE_VehicleGeneration SET ModelId=@modelId, Code=@code, Name=@name, StartYear=@startYear, EndYear=@endYear, IsActive=@active WHERE Id=@id", new LinqToDB.Data.DataParameter("modelId", entity.ModelId), new LinqToDB.Data.DataParameter("code", entity.Code), new LinqToDB.Data.DataParameter("name", entity.Name), new LinqToDB.Data.DataParameter("startYear", entity.StartYear), new LinqToDB.Data.DataParameter("endYear", entity.EndYear), new LinqToDB.Data.DataParameter("active", entity.IsActive), new LinqToDB.Data.DataParameter("id", entity.Id));

    public Task DeleteGenerationAsync(int id, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("DELETE FROM TP_CE_VehicleGeneration WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id));

    public async Task<VehicleMergeRepositoryResult> MergeGenerationAsync(
        int sourceGenerationId,
        int targetGenerationId,
        CancellationToken cancellationToken)
    {
        // Bodies and configurations reparent onto the survivor generation. Configuration ids stay
        // stable, so every fitment claim, garage row and SEO landing that references a configuration
        // is reassigned to the survivor without touching its foreign keys (AC-012.1 / FR-112).
        var rows = await _dataProvider.QueryAsync<MergeRow>(@"
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1
    FROM TP_CE_VehicleBody sourceBody
    INNER JOIN TP_CE_VehicleBody targetBody
        ON targetBody.GenerationId = @targetGenerationId
       AND targetBody.Code = sourceBody.Code
    WHERE sourceBody.GenerationId = @sourceGenerationId
)
BEGIN
    ROLLBACK TRANSACTION;
    SELECT CAST(0 AS bit) AS Success,
           CAST('vehicle.generation.merge.body_code_conflict' AS nvarchar(128)) AS ErrorCode,
           0 AS MovedChildren,
           0 AS MovedAliases;
    RETURN;
END;

DECLARE @movedBodies int;
DECLARE @movedConfigurations int;
DECLARE @movedAliases int;

UPDATE TP_CE_VehicleBody SET GenerationId=@targetGenerationId WHERE GenerationId=@sourceGenerationId;
SET @movedBodies = @@ROWCOUNT;

UPDATE TP_CE_VehicleConfiguration SET GenerationId=@targetGenerationId WHERE GenerationId=@sourceGenerationId;
SET @movedConfigurations = @@ROWCOUNT;

UPDATE TP_CE_VehicleAlias SET NodeId=@targetGenerationId WHERE NodeType='generation' AND NodeId=@sourceGenerationId;
SET @movedAliases = @@ROWCOUNT;

UPDATE TP_CE_VehicleGeneration SET IsActive=0 WHERE Id=@sourceGenerationId;

COMMIT TRANSACTION;
SELECT CAST(1 AS bit) AS Success,
       CAST(NULL AS nvarchar(128)) AS ErrorCode,
       (@movedBodies + @movedConfigurations) AS MovedChildren,
       @movedAliases AS MovedAliases;",
            new DataParameter("sourceGenerationId", sourceGenerationId),
            new DataParameter("targetGenerationId", targetGenerationId));
        return MapMerge(rows.Single());
    }

    public async Task<IReadOnlyList<VehicleBody>> GetBodiesAsync(CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleBody>("SELECT Id, GenerationId, Code, Name, Doors, IsActive FROM TP_CE_VehicleBody ORDER BY Name")).ToList();

    public async Task<VehicleBody?> GetBodyByIdAsync(int id, CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleBody>("SELECT Id, GenerationId, Code, Name, Doors, IsActive FROM TP_CE_VehicleBody WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id))).FirstOrDefault();

    public Task CreateBodyAsync(VehicleBody entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("INSERT INTO TP_CE_VehicleBody (GenerationId, Code, Name, Doors, IsActive) VALUES (@generationId,@code,@name,@doors,@active)", new LinqToDB.Data.DataParameter("generationId", entity.GenerationId), new LinqToDB.Data.DataParameter("code", entity.Code), new LinqToDB.Data.DataParameter("name", entity.Name), new LinqToDB.Data.DataParameter("doors", entity.Doors), new LinqToDB.Data.DataParameter("active", entity.IsActive));

    public Task UpdateBodyAsync(VehicleBody entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("UPDATE TP_CE_VehicleBody SET GenerationId=@generationId, Code=@code, Name=@name, Doors=@doors, IsActive=@active WHERE Id=@id", new LinqToDB.Data.DataParameter("generationId", entity.GenerationId), new LinqToDB.Data.DataParameter("code", entity.Code), new LinqToDB.Data.DataParameter("name", entity.Name), new LinqToDB.Data.DataParameter("doors", entity.Doors), new LinqToDB.Data.DataParameter("active", entity.IsActive), new LinqToDB.Data.DataParameter("id", entity.Id));

    public Task DeleteBodyAsync(int id, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("DELETE FROM TP_CE_VehicleBody WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id));

    public async Task<IReadOnlyList<VehicleEngine>> GetEnginesAsync(CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleEngine>("SELECT Id, BodyId, Code, Name, FuelType, DisplacementCc, PowerHp, IsActive FROM TP_CE_VehicleEngine ORDER BY Name")).ToList();

    public async Task<VehicleEngine?> GetEngineByIdAsync(int id, CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleEngine>("SELECT Id, BodyId, Code, Name, FuelType, DisplacementCc, PowerHp, IsActive FROM TP_CE_VehicleEngine WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id))).FirstOrDefault();

    public Task CreateEngineAsync(VehicleEngine entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("INSERT INTO TP_CE_VehicleEngine (BodyId, Code, Name, FuelType, DisplacementCc, PowerHp, IsActive) VALUES (@bodyId,@code,@name,@fuelType,@displacementCc,@powerHp,@active)", new LinqToDB.Data.DataParameter("bodyId", entity.BodyId), new LinqToDB.Data.DataParameter("code", entity.Code), new LinqToDB.Data.DataParameter("name", entity.Name), new LinqToDB.Data.DataParameter("fuelType", entity.FuelType), new LinqToDB.Data.DataParameter("displacementCc", entity.DisplacementCc), new LinqToDB.Data.DataParameter("powerHp", entity.PowerHp), new LinqToDB.Data.DataParameter("active", entity.IsActive));

    public Task UpdateEngineAsync(VehicleEngine entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("UPDATE TP_CE_VehicleEngine SET BodyId=@bodyId, Code=@code, Name=@name, FuelType=@fuelType, DisplacementCc=@displacementCc, PowerHp=@powerHp, IsActive=@active WHERE Id=@id", new LinqToDB.Data.DataParameter("bodyId", entity.BodyId), new LinqToDB.Data.DataParameter("code", entity.Code), new LinqToDB.Data.DataParameter("name", entity.Name), new LinqToDB.Data.DataParameter("fuelType", entity.FuelType), new LinqToDB.Data.DataParameter("displacementCc", entity.DisplacementCc), new LinqToDB.Data.DataParameter("powerHp", entity.PowerHp), new LinqToDB.Data.DataParameter("active", entity.IsActive), new LinqToDB.Data.DataParameter("id", entity.Id));

    public Task DeleteEngineAsync(int id, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("DELETE FROM TP_CE_VehicleEngine WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id));

    public async Task<IReadOnlyList<VehicleMarket>> GetMarketsAsync(CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleMarket>("SELECT Id, Code, Name, IsActive FROM TP_CE_VehicleMarket ORDER BY Name")).ToList();

    public async Task<VehicleMarket?> GetMarketByIdAsync(int id, CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleMarket>("SELECT Id, Code, Name, IsActive FROM TP_CE_VehicleMarket WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id))).FirstOrDefault();

    public Task CreateMarketAsync(VehicleMarket entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("INSERT INTO TP_CE_VehicleMarket (Code, Name, IsActive) VALUES (@code,@name,@active)", new LinqToDB.Data.DataParameter("code", entity.Code), new LinqToDB.Data.DataParameter("name", entity.Name), new LinqToDB.Data.DataParameter("active", entity.IsActive));

    public Task UpdateMarketAsync(VehicleMarket entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("UPDATE TP_CE_VehicleMarket SET Code=@code, Name=@name, IsActive=@active WHERE Id=@id", new LinqToDB.Data.DataParameter("code", entity.Code), new LinqToDB.Data.DataParameter("name", entity.Name), new LinqToDB.Data.DataParameter("active", entity.IsActive), new LinqToDB.Data.DataParameter("id", entity.Id));

    public Task DeleteMarketAsync(int id, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("DELETE FROM TP_CE_VehicleMarket WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id));

    public async Task<IReadOnlyList<VehicleConfiguration>> GetConfigurationsAsync(CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleConfiguration>("SELECT Id, GenerationId, BodyId, EngineId, MarketId, TrimName, ProductionFromYear, ProductionToYear, Fingerprint, IsActive FROM TP_CE_VehicleConfiguration ORDER BY Id")).ToList();

    public async Task<VehicleConfiguration?> GetConfigurationByIdAsync(int id, CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleConfiguration>("SELECT Id, GenerationId, BodyId, EngineId, MarketId, TrimName, ProductionFromYear, ProductionToYear, Fingerprint, IsActive FROM TP_CE_VehicleConfiguration WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id))).FirstOrDefault();

    public Task CreateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("INSERT INTO TP_CE_VehicleConfiguration (GenerationId, BodyId, EngineId, MarketId, TrimName, ProductionFromYear, ProductionToYear, Fingerprint, IsActive) VALUES (@generationId,@bodyId,@engineId,@marketId,@trimName,@fromYear,@toYear,@fingerprint,@active)", new LinqToDB.Data.DataParameter("generationId", entity.GenerationId), new LinqToDB.Data.DataParameter("bodyId", entity.BodyId), new LinqToDB.Data.DataParameter("engineId", entity.EngineId), new LinqToDB.Data.DataParameter("marketId", entity.MarketId), new LinqToDB.Data.DataParameter("trimName", entity.TrimName), new LinqToDB.Data.DataParameter("fromYear", entity.ProductionFromYear), new LinqToDB.Data.DataParameter("toYear", entity.ProductionToYear), new LinqToDB.Data.DataParameter("fingerprint", entity.Fingerprint), new LinqToDB.Data.DataParameter("active", entity.IsActive));

    public Task UpdateConfigurationAsync(VehicleConfiguration entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("UPDATE TP_CE_VehicleConfiguration SET GenerationId=@generationId, BodyId=@bodyId, EngineId=@engineId, MarketId=@marketId, TrimName=@trimName, ProductionFromYear=@fromYear, ProductionToYear=@toYear, Fingerprint=@fingerprint, IsActive=@active WHERE Id=@id", new LinqToDB.Data.DataParameter("generationId", entity.GenerationId), new LinqToDB.Data.DataParameter("bodyId", entity.BodyId), new LinqToDB.Data.DataParameter("engineId", entity.EngineId), new LinqToDB.Data.DataParameter("marketId", entity.MarketId), new LinqToDB.Data.DataParameter("trimName", entity.TrimName), new LinqToDB.Data.DataParameter("fromYear", entity.ProductionFromYear), new LinqToDB.Data.DataParameter("toYear", entity.ProductionToYear), new LinqToDB.Data.DataParameter("fingerprint", entity.Fingerprint), new LinqToDB.Data.DataParameter("active", entity.IsActive), new LinqToDB.Data.DataParameter("id", entity.Id));

    public Task DeleteConfigurationAsync(int id, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("DELETE FROM TP_CE_VehicleConfiguration WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id));

    public async Task<IReadOnlyList<VehicleAlias>> GetAliasesAsync(CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleAlias>("SELECT Id, NodeType, NodeId, Locale, AliasText, NormalizedAlias FROM TP_CE_VehicleAlias ORDER BY Id")).ToList();

    public async Task<VehicleAlias?> GetAliasByIdAsync(int id, CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VehicleAlias>("SELECT Id, NodeType, NodeId, Locale, AliasText, NormalizedAlias FROM TP_CE_VehicleAlias WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id))).FirstOrDefault();

    public Task CreateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("INSERT INTO TP_CE_VehicleAlias (NodeType, NodeId, Locale, AliasText, NormalizedAlias) VALUES (@nodeType,@nodeId,@locale,@aliasText,@normalizedAlias)", new LinqToDB.Data.DataParameter("nodeType", entity.NodeType), new LinqToDB.Data.DataParameter("nodeId", entity.NodeId), new LinqToDB.Data.DataParameter("locale", entity.Locale), new LinqToDB.Data.DataParameter("aliasText", entity.AliasText), new LinqToDB.Data.DataParameter("normalizedAlias", entity.NormalizedAlias));

    public Task UpdateAliasAsync(VehicleAlias entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("UPDATE TP_CE_VehicleAlias SET NodeType=@nodeType, NodeId=@nodeId, Locale=@locale, AliasText=@aliasText, NormalizedAlias=@normalizedAlias WHERE Id=@id", new LinqToDB.Data.DataParameter("nodeType", entity.NodeType), new LinqToDB.Data.DataParameter("nodeId", entity.NodeId), new LinqToDB.Data.DataParameter("locale", entity.Locale), new LinqToDB.Data.DataParameter("aliasText", entity.AliasText), new LinqToDB.Data.DataParameter("normalizedAlias", entity.NormalizedAlias), new LinqToDB.Data.DataParameter("id", entity.Id));

    public Task DeleteAliasAsync(int id, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("DELETE FROM TP_CE_VehicleAlias WHERE Id=@id", new LinqToDB.Data.DataParameter("id", id));

    private static VehicleMergeRepositoryResult MapMerge(MergeRow row)
        => row.Success
            ? VehicleMergeRepositoryResult.Ok(row.MovedChildren, row.MovedAliases)
            : VehicleMergeRepositoryResult.Fail(row.ErrorCode ?? "vehicle.merge.failed");

    private sealed class MergeRow
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public int MovedChildren { get; set; }
        public int MovedAliases { get; set; }
    }
}
