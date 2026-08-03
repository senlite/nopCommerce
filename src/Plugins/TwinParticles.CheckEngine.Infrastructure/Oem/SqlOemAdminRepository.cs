using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Oem.Admin;

namespace TwinParticles.CheckEngine.Infrastructure.Oem;

public sealed class SqlOemAdminRepository : IOemAdminRepository, IOemRelationReadRepository, IOemSearchReadRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlOemAdminRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<IReadOnlyList<Manufacturer>> GetManufacturersAsync(CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<Manufacturer>("SELECT Id, Code, Name, IsOeBrand, IsActive FROM TP_CE_Manufacturer ORDER BY Name")).ToList();

    public async Task<Manufacturer?> GetManufacturerByIdAsync(int id, CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<Manufacturer>("SELECT Id, Code, Name, IsOeBrand, IsActive FROM TP_CE_Manufacturer WHERE Id=@id", new DataParameter("id", id))).FirstOrDefault();

    public Task CreateManufacturerAsync(Manufacturer entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("INSERT INTO TP_CE_Manufacturer (Code, Name, IsOeBrand, IsActive) VALUES (@code, @name, @isOeBrand, @isActive)",
            new DataParameter("code", entity.Code),
            new DataParameter("name", entity.Name),
            new DataParameter("isOeBrand", entity.IsOeBrand),
            new DataParameter("isActive", entity.IsActive));

    public Task UpdateManufacturerAsync(Manufacturer entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("UPDATE TP_CE_Manufacturer SET Code=@code, Name=@name, IsOeBrand=@isOeBrand, IsActive=@isActive WHERE Id=@id",
            new DataParameter("code", entity.Code),
            new DataParameter("name", entity.Name),
            new DataParameter("isOeBrand", entity.IsOeBrand),
            new DataParameter("isActive", entity.IsActive),
            new DataParameter("id", entity.Id));

    public Task DeleteManufacturerAsync(int id, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("DELETE FROM TP_CE_Manufacturer WHERE Id=@id", new DataParameter("id", id));

    public async Task<IReadOnlyList<OemNumber>> GetOemNumbersAsync(CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<OemNumber>("SELECT Id, ManufacturerId, DisplayNumber, NormalizedNumber, IsObsolete, IsActive FROM TP_CE_OemNumber ORDER BY DisplayNumber")).ToList();

    public async Task<OemNumber?> GetOemNumberByIdAsync(int id, CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<OemNumber>("SELECT Id, ManufacturerId, DisplayNumber, NormalizedNumber, IsObsolete, IsActive FROM TP_CE_OemNumber WHERE Id=@id", new DataParameter("id", id))).FirstOrDefault();

    public Task CreateOemNumberAsync(OemNumber entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("INSERT INTO TP_CE_OemNumber (ManufacturerId, DisplayNumber, NormalizedNumber, IsObsolete, IsActive) VALUES (@manufacturerId, @displayNumber, @normalizedNumber, @isObsolete, @isActive)",
            new DataParameter("manufacturerId", entity.ManufacturerId),
            new DataParameter("displayNumber", entity.DisplayNumber),
            new DataParameter("normalizedNumber", entity.NormalizedNumber),
            new DataParameter("isObsolete", entity.IsObsolete),
            new DataParameter("isActive", entity.IsActive));

    public Task UpdateOemNumberAsync(OemNumber entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("UPDATE TP_CE_OemNumber SET ManufacturerId=@manufacturerId, DisplayNumber=@displayNumber, NormalizedNumber=@normalizedNumber, IsObsolete=@isObsolete, IsActive=@isActive WHERE Id=@id",
            new DataParameter("manufacturerId", entity.ManufacturerId),
            new DataParameter("displayNumber", entity.DisplayNumber),
            new DataParameter("normalizedNumber", entity.NormalizedNumber),
            new DataParameter("isObsolete", entity.IsObsolete),
            new DataParameter("isActive", entity.IsActive),
            new DataParameter("id", entity.Id));

    public Task DeleteOemNumberAsync(int id, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("DELETE FROM TP_CE_OemNumber WHERE Id=@id", new DataParameter("id", id));

    public async Task<IReadOnlyList<OemRelation>> GetRelationsAsync(CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<OemRelation>("SELECT Id, FromOemNumberId, ToOemNumberId, RelationTypeId as RelationType, ValidFromUtc, ValidToUtc, IsActive FROM TP_CE_OemRelation ORDER BY Id")).ToList();

    public async Task<OemRelation?> GetRelationByIdAsync(int id, CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<OemRelation>("SELECT Id, FromOemNumberId, ToOemNumberId, RelationTypeId as RelationType, ValidFromUtc, ValidToUtc, IsActive FROM TP_CE_OemRelation WHERE Id=@id", new DataParameter("id", id))).FirstOrDefault();

    public Task CreateRelationAsync(OemRelation entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("INSERT INTO TP_CE_OemRelation (FromOemNumberId, ToOemNumberId, RelationTypeId, ValidFromUtc, ValidToUtc, IsActive) VALUES (@fromId, @toId, @relationTypeId, @validFromUtc, @validToUtc, @isActive)",
            new DataParameter("fromId", entity.FromOemNumberId),
            new DataParameter("toId", entity.ToOemNumberId),
            new DataParameter("relationTypeId", (int)entity.RelationType),
            new DataParameter("validFromUtc", entity.ValidFromUtc),
            new DataParameter("validToUtc", entity.ValidToUtc),
            new DataParameter("isActive", entity.IsActive));

    public Task UpdateRelationAsync(OemRelation entity, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("UPDATE TP_CE_OemRelation SET FromOemNumberId=@fromId, ToOemNumberId=@toId, RelationTypeId=@relationTypeId, ValidFromUtc=@validFromUtc, ValidToUtc=@validToUtc, IsActive=@isActive WHERE Id=@id",
            new DataParameter("fromId", entity.FromOemNumberId),
            new DataParameter("toId", entity.ToOemNumberId),
            new DataParameter("relationTypeId", (int)entity.RelationType),
            new DataParameter("validFromUtc", entity.ValidFromUtc),
            new DataParameter("validToUtc", entity.ValidToUtc),
            new DataParameter("isActive", entity.IsActive),
            new DataParameter("id", entity.Id));

    public Task DeleteRelationAsync(int id, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("DELETE FROM TP_CE_OemRelation WHERE Id=@id", new DataParameter("id", id));

    public async Task<IReadOnlyList<OemRelation>> GetActiveOutgoingRelationsAsync(int fromOemNumberId, CancellationToken cancellationToken)
        => (await _dataProvider.QueryAsync<OemRelation>("SELECT Id, FromOemNumberId, ToOemNumberId, RelationTypeId as RelationType, ValidFromUtc, ValidToUtc, IsActive FROM TP_CE_OemRelation WHERE FromOemNumberId=@fromId AND IsActive=1 ORDER BY Id", new DataParameter("fromId", fromOemNumberId))).ToList();

    public async Task<IReadOnlyList<OemNumber>> FindByNormalizedNumberAsync(string normalizedNumber, int? manufacturerId, CancellationToken cancellationToken)
    {
        if (manufacturerId.HasValue)
        {
            return (await _dataProvider.QueryAsync<OemNumber>(
                "SELECT Id, ManufacturerId, DisplayNumber, NormalizedNumber, IsObsolete, IsActive FROM TP_CE_OemNumber WHERE NormalizedNumber=@normalized AND ManufacturerId=@manufacturerId AND IsActive=1 ORDER BY Id",
                new DataParameter("normalized", normalizedNumber),
                new DataParameter("manufacturerId", manufacturerId.Value))).ToList();
        }

        return (await _dataProvider.QueryAsync<OemNumber>(
            "SELECT Id, ManufacturerId, DisplayNumber, NormalizedNumber, IsObsolete, IsActive FROM TP_CE_OemNumber WHERE NormalizedNumber=@normalized AND IsActive=1 ORDER BY ManufacturerId, Id",
            new DataParameter("normalized", normalizedNumber))).ToList();
    }
}
