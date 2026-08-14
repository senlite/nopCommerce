using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    // Keep row batches under the SQL Server ~2,100 parameter ceiling (4 params per row).
    private const int BulkUpsertBatchSize = 400;

    public async Task<OemBulkUpsertResult> BulkUpsertOemNumbersAsync(IReadOnlyList<OemNumber> numbers, CancellationToken cancellationToken)
    {
        if (numbers is null || numbers.Count == 0)
            return OemBulkUpsertResult.Empty;

        var inserted = 0;
        var updated = 0;

        for (var offset = 0; offset < numbers.Count; offset += BulkUpsertBatchSize)
        {
            var batch = numbers.Skip(offset).Take(BulkUpsertBatchSize).ToList();
            var parameters = new List<DataParameter>(batch.Count * 4);
            var values = new StringBuilder();

            for (var i = 0; i < batch.Count; i++)
            {
                var entity = batch[i];
                if (i > 0)
                    values.Append(',');

                values.Append($"(@m{i},@d{i},@n{i},@o{i})");
                parameters.Add(new DataParameter($"m{i}", entity.ManufacturerId));
                parameters.Add(new DataParameter($"d{i}", entity.DisplayNumber));
                parameters.Add(new DataParameter($"n{i}", entity.NormalizedNumber));
                parameters.Add(new DataParameter($"o{i}", entity.IsObsolete));
            }

            // MERGE keyed on (ManufacturerId, NormalizedNumber) per FR-236. Matched rows keep their Id so
            // relations and product maps that reference them survive; unmatched rows are inserted active.
            var sql = $@"
SET NOCOUNT ON;
DECLARE @actions TABLE(act nvarchar(10));
MERGE INTO TP_CE_OemNumber AS T
USING (VALUES {values}) AS S (ManufacturerId, DisplayNumber, NormalizedNumber, IsObsolete)
    ON T.ManufacturerId = S.ManufacturerId AND T.NormalizedNumber = S.NormalizedNumber
WHEN MATCHED THEN
    UPDATE SET DisplayNumber = S.DisplayNumber, IsObsolete = S.IsObsolete, IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (ManufacturerId, DisplayNumber, NormalizedNumber, IsObsolete, IsActive)
    VALUES (S.ManufacturerId, S.DisplayNumber, S.NormalizedNumber, S.IsObsolete, 1)
OUTPUT $action INTO @actions;
SELECT
    SUM(CASE WHEN act = 'INSERT' THEN 1 ELSE 0 END) AS Inserted,
    SUM(CASE WHEN act = 'UPDATE' THEN 1 ELSE 0 END) AS Updated
FROM @actions;";

            var row = (await _dataProvider.QueryAsync<BulkUpsertCountRow>(sql, parameters.ToArray())).First();
            inserted += row.Inserted;
            updated += row.Updated;
        }

        return new OemBulkUpsertResult { Inserted = inserted, Updated = updated };
    }

    private sealed class BulkUpsertCountRow
    {
        public int Inserted { get; set; }
        public int Updated { get; set; }
    }

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

    public async Task<IReadOnlyList<OemNumber>> FindByNormalizedPrefixAsync(string normalizedPrefix, int take, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedPrefix))
            return [];

        var limit = take <= 0 ? 5 : System.Math.Min(take, 20);

        // Prefix match on the indexed normalized column. The LIKE pattern is parameterized; the caller
        // supplies an already-normalized token so we only append the wildcard.
        return (await _dataProvider.QueryAsync<OemNumber>(
            $"SELECT TOP ({limit}) Id, ManufacturerId, DisplayNumber, NormalizedNumber, IsObsolete, IsActive FROM TP_CE_OemNumber WHERE NormalizedNumber LIKE @prefix AND IsActive=1 ORDER BY NormalizedNumber, ManufacturerId, Id",
            new DataParameter("prefix", normalizedPrefix + "%"))).ToList();
    }
}
