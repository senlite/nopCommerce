using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Dealer;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Dealer;

public sealed class SqlDealerPortalRepository : IDealerPortalRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlDealerPortalRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<DealerAccount?> GetAccountByCustomerIdAsync(int customerId, CancellationToken cancellationToken)
    {
        var sql = CheckEngineSql.SelectTop(1,
            "Id, CustomerId, DisplayName, DefaultPriceListId, IsActive",
            "FROM TP_CE_DealerAccount WHERE CustomerId = @customerId ORDER BY Id");
        var rows = await _dataProvider.QueryAsync<AccountRow>(sql, new DataParameter("customerId", customerId));
        return rows.Select(MapAccount).FirstOrDefault();
    }

    public async Task<DealerAccount?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<AccountRow>(@"
SELECT Id, CustomerId, DisplayName, DefaultPriceListId, IsActive
FROM TP_CE_DealerAccount
WHERE Id = @id",
            new DataParameter("id", accountId));

        return rows.Select(MapAccount).FirstOrDefault();
    }

    public async Task<IReadOnlyList<DealerFranchise>> GetFranchisesAsync(int dealerAccountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<FranchiseRow>(@"
SELECT Id, DealerAccountId, MakeId, FranchiseLabel
FROM TP_CE_DealerFranchise
WHERE DealerAccountId = @dealerAccountId
ORDER BY FranchiseLabel",
            new DataParameter("dealerAccountId", dealerAccountId));

        return rows.Select(MapFranchise).ToList();
    }

    public async Task<IReadOnlyList<DealerCatalogItem>> GetCatalogViewAsync(int dealerAccountId, CancellationToken cancellationToken)
    {
        var franchiseCount = await _dataProvider.QueryAsync<int>(@"
SELECT COUNT(1) FROM TP_CE_DealerFranchise WHERE DealerAccountId = @dealerAccountId",
            new DataParameter("dealerAccountId", dealerAccountId));

        var hasFranchises = franchiseCount.FirstOrDefault() > 0;
        var franchiseFilter = hasFranchises
            ? @"INNER JOIN TP_CE_DealerFranchise df ON df.DealerAccountId = da.Id
INNER JOIN TP_CE_FitmentClaim fc ON fc.ProductId = p.Id AND fc.IsPublished = 1
INNER JOIN TP_CE_VehicleConfiguration vc ON vc.Id = fc.VehicleConfigurationId
INNER JOIN TP_CE_VehicleModel vm ON vm.Id = vc.ModelId AND vm.MakeId = df.MakeId"
            : string.Empty;

        var rows = await _dataProvider.QueryAsync<CatalogItemRow>($@"
SELECT DISTINCT p.Id AS ProductId,
       p.Sku,
       p.Name,
       COALESCE(pli.UnitPrice, 0) AS DealerPrice,
       COALESCE(a.PeriodCeilingUnits - a.PeriodUsedUnits, 0) AS RemainingAllocationUnits
FROM Product p
INNER JOIN TP_CE_DealerAccount da ON da.Id = @dealerAccountId
{franchiseFilter}
LEFT JOIN TP_CE_PriceListItem pli ON pli.ProductId = p.Id AND pli.PriceListId = da.DefaultPriceListId
LEFT JOIN TP_CE_DealerAllocation a ON a.DealerAccountId = da.Id AND a.ProductId = p.Id
WHERE p.Deleted = 0 AND p.Published = 1
ORDER BY p.Name",
            new DataParameter("dealerAccountId", dealerAccountId));

        return rows.Select(MapCatalogItem).ToList();
    }

    public async Task<DealerAllocation?> GetAllocationForProductAsync(int dealerAccountId, int productId, CancellationToken cancellationToken)
    {
        var productSql = CheckEngineSql.SelectTop(1,
            "Id, DealerAccountId, ProductId, CategoryId, PeriodCeilingUnits, PeriodUsedUnits",
            "FROM TP_CE_DealerAllocation WHERE DealerAccountId = @dealerAccountId AND ProductId = @productId ORDER BY Id");
        var productRows = await _dataProvider.QueryAsync<AllocationRow>(productSql,
            new DataParameter("dealerAccountId", dealerAccountId),
            new DataParameter("productId", productId));

        var allocation = productRows.Select(MapAllocation).FirstOrDefault();
        if (allocation is not null)
            return allocation;

        var categorySql = CheckEngineSql.SelectTop(1,
            "a.Id, a.DealerAccountId, a.ProductId, a.CategoryId, a.PeriodCeilingUnits, a.PeriodUsedUnits",
            "FROM TP_CE_DealerAllocation a INNER JOIN Product_Category_Mapping pcm ON pcm.CategoryId = a.CategoryId AND pcm.ProductId = @productId WHERE a.DealerAccountId = @dealerAccountId AND a.ProductId IS NULL ORDER BY a.Id");
        var categoryRows = await _dataProvider.QueryAsync<AllocationRow>(categorySql,
            new DataParameter("dealerAccountId", dealerAccountId),
            new DataParameter("productId", productId));

        return categoryRows.Select(MapAllocation).FirstOrDefault();
    }

    public async Task<DealerQuota?> GetQuotaAsync(int dealerAccountId, CancellationToken cancellationToken)
    {
        var sql = CheckEngineSql.SelectTop(1,
            "Id, DealerAccountId, SpendCeiling, SpendUsed",
            "FROM TP_CE_DealerQuota WHERE DealerAccountId = @dealerAccountId ORDER BY Id");
        var rows = await _dataProvider.QueryAsync<QuotaRow>(sql, new DataParameter("dealerAccountId", dealerAccountId));
        return rows.Select(MapQuota).FirstOrDefault();
    }

    public Task UpdateAllocationAsync(DealerAllocation allocation, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_DealerAllocation
SET DealerAccountId = @dealerAccountId,
    ProductId = @productId,
    CategoryId = @categoryId,
    PeriodCeilingUnits = @periodCeilingUnits,
    PeriodUsedUnits = @periodUsedUnits
WHERE Id = @id",
            new DataParameter("id", allocation.Id),
            new DataParameter("dealerAccountId", allocation.DealerAccountId),
            new DataParameter("productId", allocation.ProductId ?? (object)DBNull.Value),
            new DataParameter("categoryId", allocation.CategoryId ?? (object)DBNull.Value),
            new DataParameter("periodCeilingUnits", allocation.PeriodCeilingUnits),
            new DataParameter("periodUsedUnits", allocation.PeriodUsedUnits));

    public Task UpdateQuotaAsync(DealerQuota quota, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_DealerQuota
SET DealerAccountId = @dealerAccountId,
    SpendCeiling = @spendCeiling,
    SpendUsed = @spendUsed
WHERE Id = @id",
            new DataParameter("id", quota.Id),
            new DataParameter("dealerAccountId", quota.DealerAccountId),
            new DataParameter("spendCeiling", quota.SpendCeiling),
            new DataParameter("spendUsed", quota.SpendUsed));

    public async Task<int> InsertWarrantyClaimAsync(WarrantyClaim claim, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_WarrantyClaim
(DealerAccountId, OrderId, OemNumber, VehicleConfigurationId, StatusId, EvidenceJson, ResolvedOemNumberId, CreatedUtc, UpdatedUtc)
VALUES
(@dealerAccountId, @orderId, @oemNumber, @vehicleConfigurationId, @statusId, @evidenceJson, @resolvedOemNumberId, @createdUtc, @updatedUtc);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("dealerAccountId", claim.DealerAccountId),
            new DataParameter("orderId", claim.OrderId ?? (object)DBNull.Value),
            new DataParameter("oemNumber", claim.OemNumber),
            new DataParameter("vehicleConfigurationId", claim.VehicleConfigurationId),
            new DataParameter("statusId", (int)claim.Status),
            new DataParameter("evidenceJson", claim.EvidenceJson),
            new DataParameter("resolvedOemNumberId", claim.ResolvedOemNumberId ?? (object)DBNull.Value),
            new DataParameter("createdUtc", claim.CreatedUtc.UtcDateTime),
            new DataParameter("updatedUtc", claim.UpdatedUtc.UtcDateTime));

        return id.FirstOrDefault();
    }

    public Task UpdateWarrantyClaimAsync(WarrantyClaim claim, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_WarrantyClaim
SET DealerAccountId = @dealerAccountId,
    OrderId = @orderId,
    OemNumber = @oemNumber,
    VehicleConfigurationId = @vehicleConfigurationId,
    StatusId = @statusId,
    EvidenceJson = @evidenceJson,
    ResolvedOemNumberId = @resolvedOemNumberId,
    UpdatedUtc = @updatedUtc
WHERE Id = @id",
            new DataParameter("id", claim.Id),
            new DataParameter("dealerAccountId", claim.DealerAccountId),
            new DataParameter("orderId", claim.OrderId ?? (object)DBNull.Value),
            new DataParameter("oemNumber", claim.OemNumber),
            new DataParameter("vehicleConfigurationId", claim.VehicleConfigurationId),
            new DataParameter("statusId", (int)claim.Status),
            new DataParameter("evidenceJson", claim.EvidenceJson),
            new DataParameter("resolvedOemNumberId", claim.ResolvedOemNumberId ?? (object)DBNull.Value),
            new DataParameter("updatedUtc", claim.UpdatedUtc.UtcDateTime));

    public async Task<WarrantyClaim?> GetWarrantyClaimAsync(int claimId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<WarrantyClaimRow>(@"
SELECT Id, DealerAccountId, OrderId, OemNumber, VehicleConfigurationId, StatusId, EvidenceJson, ResolvedOemNumberId, CreatedUtc, UpdatedUtc
FROM TP_CE_WarrantyClaim
WHERE Id = @id",
            new DataParameter("id", claimId));

        return rows.Select(MapWarrantyClaim).FirstOrDefault();
    }

    public async Task<IReadOnlyList<WarrantyClaim>> ListWarrantyClaimsByAccountAsync(int dealerAccountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<WarrantyClaimRow>(@"
SELECT Id, DealerAccountId, OrderId, OemNumber, VehicleConfigurationId, StatusId, EvidenceJson, ResolvedOemNumberId, CreatedUtc, UpdatedUtc
FROM TP_CE_WarrantyClaim
WHERE DealerAccountId = @accountId
ORDER BY UpdatedUtc DESC",
            new DataParameter("accountId", dealerAccountId));

        return rows.Select(MapWarrantyClaim).ToList();
    }

    public async Task<int> InsertAccountAsync(DealerAccount account, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_DealerAccount
(CustomerId, DisplayName, DefaultPriceListId, IsActive)
VALUES
(@customerId, @displayName, @defaultPriceListId, @isActive);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("customerId", account.CustomerId),
            new DataParameter("displayName", account.DisplayName),
            new DataParameter("defaultPriceListId", account.DefaultPriceListId ?? (object)DBNull.Value),
            new DataParameter("isActive", account.IsActive));

        return id.FirstOrDefault();
    }

    public async Task<int> InsertAllocationAsync(DealerAllocation allocation, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_DealerAllocation
(DealerAccountId, ProductId, CategoryId, PeriodCeilingUnits, PeriodUsedUnits)
VALUES
(@dealerAccountId, @productId, @categoryId, @periodCeilingUnits, @periodUsedUnits);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("dealerAccountId", allocation.DealerAccountId),
            new DataParameter("productId", allocation.ProductId ?? (object)DBNull.Value),
            new DataParameter("categoryId", allocation.CategoryId ?? (object)DBNull.Value),
            new DataParameter("periodCeilingUnits", allocation.PeriodCeilingUnits),
            new DataParameter("periodUsedUnits", allocation.PeriodUsedUnits));

        return id.FirstOrDefault();
    }

    public async Task<int> InsertQuotaAsync(DealerQuota quota, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_DealerQuota
(DealerAccountId, SpendCeiling, SpendUsed)
VALUES
(@dealerAccountId, @spendCeiling, @spendUsed);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("dealerAccountId", quota.DealerAccountId),
            new DataParameter("spendCeiling", quota.SpendCeiling),
            new DataParameter("spendUsed", quota.SpendUsed));

        return id.FirstOrDefault();
    }

    public async Task<int> InsertFranchiseAsync(DealerFranchise franchise, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_DealerFranchise
(DealerAccountId, MakeId, FranchiseLabel)
VALUES
(@dealerAccountId, @makeId, @franchiseLabel);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("dealerAccountId", franchise.DealerAccountId),
            new DataParameter("makeId", franchise.MakeId),
            new DataParameter("franchiseLabel", franchise.FranchiseLabel));

        return id.FirstOrDefault();
    }

    public Task UpdateAccountAsync(DealerAccount account, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_DealerAccount
SET CustomerId = @customerId,
    DisplayName = @displayName,
    DefaultPriceListId = @defaultPriceListId,
    IsActive = @isActive
WHERE Id = @id",
            new DataParameter("id", account.Id),
            new DataParameter("customerId", account.CustomerId),
            new DataParameter("displayName", account.DisplayName),
            new DataParameter("defaultPriceListId", account.DefaultPriceListId ?? (object)DBNull.Value),
            new DataParameter("isActive", account.IsActive));

    public async Task<int> InsertPriceListAsync(string name, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_PriceList (Name, IsActive) VALUES (@name, 1);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("name", name));

        return id.FirstOrDefault();
    }

    public Task InsertPriceListItemAsync(int priceListId, int productId, decimal unitPrice, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
INSERT INTO TP_CE_PriceListItem (PriceListId, ProductId, UnitPrice)
VALUES (@priceListId, @productId, @unitPrice)",
            new DataParameter("priceListId", priceListId),
            new DataParameter("productId", productId),
            new DataParameter("unitPrice", unitPrice));

    public async Task<IReadOnlyList<DealerTerritory>> GetTerritoriesAsync(int dealerAccountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<TerritoryRow>(@"
SELECT Id, DealerAccountId, MarketId, RegionCode
FROM TP_CE_DealerTerritory
WHERE DealerAccountId = @dealerAccountId
ORDER BY Id",
            new DataParameter("dealerAccountId", dealerAccountId));

        return rows.Select(row => new DealerTerritory
        {
            Id = row.Id,
            DealerAccountId = row.DealerAccountId,
            MarketId = row.MarketId,
            RegionCode = row.RegionCode
        }).ToList();
    }

    public async Task<int> InsertTerritoryAsync(DealerTerritory territory, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_DealerTerritory (DealerAccountId, MarketId, RegionCode)
VALUES (@dealerAccountId, @marketId, @regionCode);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("dealerAccountId", territory.DealerAccountId),
            new DataParameter("marketId", territory.MarketId ?? (object)DBNull.Value),
            new DataParameter("regionCode", territory.RegionCode ?? (object)DBNull.Value));

        return id.FirstOrDefault();
    }

    public async Task<bool> IsVehicleMarketAllowedAsync(int vehicleConfigurationId, IReadOnlyList<DealerTerritory> territories, CancellationToken cancellationToken)
    {
        if (territories.Count == 0)
            return true;

        var marketRows = await _dataProvider.QueryAsync<int?>(@"
SELECT MarketId FROM TP_CE_VehicleConfiguration WHERE Id = @id",
            new DataParameter("id", vehicleConfigurationId));

        var marketId = marketRows.FirstOrDefault();
        foreach (var territory in territories)
        {
            if (territory.MarketId.HasValue && territory.MarketId == marketId)
                return true;

            if (!string.IsNullOrWhiteSpace(territory.RegionCode) && marketId is null)
                return true;
        }

        return false;
    }

    private static DealerAccount MapAccount(AccountRow row)
    {
        return new DealerAccount
        {
            Id = row.Id,
            CustomerId = row.CustomerId,
            DisplayName = row.DisplayName,
            DefaultPriceListId = row.DefaultPriceListId,
            IsActive = row.IsActive
        };
    }

    private static DealerFranchise MapFranchise(FranchiseRow row)
    {
        return new DealerFranchise
        {
            Id = row.Id,
            DealerAccountId = row.DealerAccountId,
            MakeId = row.MakeId,
            FranchiseLabel = row.FranchiseLabel
        };
    }

    private static DealerCatalogItem MapCatalogItem(CatalogItemRow row)
    {
        return new DealerCatalogItem
        {
            ProductId = row.ProductId,
            Sku = row.Sku ?? string.Empty,
            Name = row.Name,
            DealerPrice = row.DealerPrice,
            RemainingAllocationUnits = row.RemainingAllocationUnits
        };
    }

    private static DealerAllocation MapAllocation(AllocationRow row)
    {
        return new DealerAllocation
        {
            Id = row.Id,
            DealerAccountId = row.DealerAccountId,
            ProductId = row.ProductId,
            CategoryId = row.CategoryId,
            PeriodCeilingUnits = row.PeriodCeilingUnits,
            PeriodUsedUnits = row.PeriodUsedUnits
        };
    }

    private static DealerQuota MapQuota(QuotaRow row)
    {
        return new DealerQuota
        {
            Id = row.Id,
            DealerAccountId = row.DealerAccountId,
            SpendCeiling = row.SpendCeiling,
            SpendUsed = row.SpendUsed
        };
    }

    private static WarrantyClaim MapWarrantyClaim(WarrantyClaimRow row)
    {
        return new WarrantyClaim
        {
            Id = row.Id,
            DealerAccountId = row.DealerAccountId,
            OrderId = row.OrderId,
            OemNumber = row.OemNumber,
            VehicleConfigurationId = row.VehicleConfigurationId,
            Status = (WarrantyClaimStatus)row.StatusId,
            EvidenceJson = row.EvidenceJson,
            ResolvedOemNumberId = row.ResolvedOemNumberId,
            CreatedUtc = ToUtc(row.CreatedUtc),
            UpdatedUtc = ToUtc(row.UpdatedUtc)
        };
    }

    private static DateTimeOffset ToUtc(DateTime value)
        => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed class AccountRow
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int? DefaultPriceListId { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class FranchiseRow
    {
        public int Id { get; set; }
        public int DealerAccountId { get; set; }
        public int MakeId { get; set; }
        public string FranchiseLabel { get; set; } = string.Empty;
    }

    private sealed class CatalogItemRow
    {
        public int ProductId { get; set; }
        public string? Sku { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DealerPrice { get; set; }
        public int RemainingAllocationUnits { get; set; }
    }

    private sealed class AllocationRow
    {
        public int Id { get; set; }
        public int DealerAccountId { get; set; }
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        public int PeriodCeilingUnits { get; set; }
        public int PeriodUsedUnits { get; set; }
    }

    private sealed class QuotaRow
    {
        public int Id { get; set; }
        public int DealerAccountId { get; set; }
        public decimal SpendCeiling { get; set; }
        public decimal SpendUsed { get; set; }
    }

    private sealed class WarrantyClaimRow
    {
        public int Id { get; set; }
        public int DealerAccountId { get; set; }
        public int? OrderId { get; set; }
        public string OemNumber { get; set; } = string.Empty;
        public int VehicleConfigurationId { get; set; }
        public int StatusId { get; set; }
        public string EvidenceJson { get; set; } = "[]";
        public int? ResolvedOemNumberId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }

    private sealed class TerritoryRow
    {
        public int Id { get; set; }
        public int DealerAccountId { get; set; }
        public int? MarketId { get; set; }
        public string? RegionCode { get; set; }
    }
}
