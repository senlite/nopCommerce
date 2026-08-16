using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;

public sealed class SqlVinSupportRepository : IVinSupportRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlVinSupportRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<IReadOnlyList<VinWmi>> GetWmisAsync(CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VinWmi>(
            "SELECT Id, Wmi, MakeId, ManufacturerName, RegionCode, IsActive FROM TP_CE_VinWmi ORDER BY Wmi")).ToList();

    public async Task<IReadOnlyList<VinPattern>> GetPatternsAsync(CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<VinPattern>(@"
SELECT Id, MakeId, Pattern, Priority, ModelCode, GenerationCode, EngineCode, TrimSlug, Confidence, Provenance, IsActive
FROM TP_CE_VinPattern
ORDER BY Priority DESC, Pattern")).ToList();

    public async Task UpsertWmiAsync(VinWmi entity, CancellationToken cancellationToken)
    {
        var existing = (await _dataProvider.QueryAsync<int>(
            "SELECT Id FROM TP_CE_VinWmi WHERE Wmi=@wmi",
            new DataParameter("wmi", entity.Wmi))).FirstOrDefault();

        if (existing > 0)
        {
            await _dataProvider.ExecuteNonQueryAsync(
                "UPDATE TP_CE_VinWmi SET MakeId=@makeId, ManufacturerName=@name, RegionCode=@region, IsActive=@active WHERE Id=@id",
                new DataParameter("makeId", entity.MakeId),
                new DataParameter("name", entity.ManufacturerName),
                new DataParameter("region", entity.RegionCode),
                new DataParameter("active", entity.IsActive),
                new DataParameter("id", existing));
            return;
        }

        await _dataProvider.ExecuteNonQueryAsync(
            "INSERT INTO TP_CE_VinWmi (Wmi, MakeId, ManufacturerName, RegionCode, IsActive) VALUES (@wmi,@makeId,@name,@region,@active)",
            new DataParameter("wmi", entity.Wmi),
            new DataParameter("makeId", entity.MakeId),
            new DataParameter("name", entity.ManufacturerName),
            new DataParameter("region", entity.RegionCode),
            new DataParameter("active", entity.IsActive));
    }

    public async Task UpsertPatternAsync(VinPattern entity, CancellationToken cancellationToken)
    {
        var existing = (await _dataProvider.QueryAsync<int>(
            "SELECT Id FROM TP_CE_VinPattern WHERE Pattern=@pattern AND ModelCode=@model AND GenerationCode=@generation AND COALESCE(EngineCode,'')=COALESCE(@engine,'')",
            new DataParameter("pattern", entity.Pattern),
            new DataParameter("model", entity.ModelCode),
            new DataParameter("generation", entity.GenerationCode),
            new DataParameter("engine", entity.EngineCode ?? string.Empty))).FirstOrDefault();

        if (existing > 0)
        {
            await _dataProvider.ExecuteNonQueryAsync(
                @"UPDATE TP_CE_VinPattern
                  SET MakeId=@makeId, Priority=@priority, TrimSlug=@trim, Confidence=@confidence, Provenance=@provenance, IsActive=@active
                  WHERE Id=@id",
                new DataParameter("makeId", entity.MakeId),
                new DataParameter("priority", entity.Priority),
                new DataParameter("trim", entity.TrimSlug),
                new DataParameter("confidence", entity.Confidence),
                new DataParameter("provenance", entity.Provenance),
                new DataParameter("active", entity.IsActive),
                new DataParameter("id", existing));
            return;
        }

        await _dataProvider.ExecuteNonQueryAsync(
            @"INSERT INTO TP_CE_VinPattern
              (MakeId, Pattern, Priority, ModelCode, GenerationCode, EngineCode, TrimSlug, Confidence, Provenance, IsActive)
              VALUES (@makeId,@pattern,@priority,@model,@generation,@engine,@trim,@confidence,@provenance,@active)",
            new DataParameter("makeId", entity.MakeId),
            new DataParameter("pattern", entity.Pattern),
            new DataParameter("priority", entity.Priority),
            new DataParameter("model", entity.ModelCode),
            new DataParameter("generation", entity.GenerationCode),
            new DataParameter("engine", entity.EngineCode),
            new DataParameter("trim", entity.TrimSlug),
            new DataParameter("confidence", entity.Confidence),
            new DataParameter("provenance", entity.Provenance),
            new DataParameter("active", entity.IsActive));
    }
}
