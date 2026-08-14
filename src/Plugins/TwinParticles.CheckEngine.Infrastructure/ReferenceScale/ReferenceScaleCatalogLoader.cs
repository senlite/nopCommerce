using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Oem.Admin;
using TwinParticles.CheckEngine.Domain.ReferenceScale;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Infrastructure.ReferenceScale;

public sealed class ReferenceScaleCatalogLoader : IReferenceScaleCatalogLoader
{
    private const int ProductBatchSize = 1_000;
    private const int OemBatchSize = 400;

    private readonly INopDataProvider _dataProvider;
    private readonly IVehicleSeedLoader _vehicleSeedLoader;
    private readonly IOemAdminRepository _oemRepository;

    public ReferenceScaleCatalogLoader(
        INopDataProvider dataProvider,
        IVehicleSeedLoader vehicleSeedLoader,
        IOemAdminRepository oemRepository)
    {
        _dataProvider = dataProvider;
        _vehicleSeedLoader = vehicleSeedLoader;
        _oemRepository = oemRepository;
    }

    public async Task<ReferenceScaleStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var status = new ReferenceScaleStatus
        {
            ProductCount = await ScalarCountAsync(
                "SELECT COUNT(*) FROM Product WHERE Sku LIKE @prefix",
                new DataParameter("prefix", ReferenceScaleManifest.ProductSkuPrefix + "%")),
            FitmentClaimCount = await ScalarCountAsync(
                "SELECT COUNT(*) FROM TP_CE_FitmentClaim WHERE SourceReference LIKE @prefix",
                new DataParameter("prefix", ReferenceScaleManifest.ProvenancePrefix + "%")),
            ConfigurationCount = await ScalarCountAsync(
                "SELECT COUNT(*) FROM TP_CE_VehicleConfiguration WHERE Fingerprint LIKE @prefix",
                new DataParameter("prefix", ReferenceScaleManifest.ConfigurationFingerprintPrefix + "%")),
            OemEntryCount = await ScalarCountAsync(
                "SELECT COUNT(*) FROM TP_CE_OemNumber WHERE NormalizedNumber LIKE @prefix",
                new DataParameter("prefix", ReferenceScaleManifest.OemNormalizedPrefix + "%")),
            ProductOemMapCount = await ScalarCountAsync(@"
SELECT COUNT(*)
FROM TP_CE_ProductOemMap map
INNER JOIN Product p ON p.Id = map.ProductId
WHERE p.Sku LIKE @prefix",
                new DataParameter("prefix", ReferenceScaleManifest.ProductSkuPrefix + "%"))
        };

        status.IsFullyLoaded =
            status.ProductCount >= ReferenceScaleManifest.TargetProducts
            && status.FitmentClaimCount >= ReferenceScaleManifest.TargetFitmentClaims
            && await ScalarCountAsync("SELECT COUNT(*) FROM TP_CE_VehicleConfiguration WHERE IsActive = 1") >= ReferenceScaleManifest.TargetConfigurations
            && status.OemEntryCount >= ReferenceScaleManifest.TargetOemEntries;

        return status;
    }

    public async Task<ReferenceScaleLoadResult> LoadAsync(ReferenceScaleLoadRequest request, CancellationToken cancellationToken)
    {
        var targets = ReferenceScaleManifest.ResolveTargets(request.ScaleFactor);
        var stopwatch = Stopwatch.StartNew();

        if (request.ReplaceExisting)
            await PurgeAsync(cancellationToken);

        var existingProducts = await ScalarCountAsync(
            "SELECT COUNT(*) FROM Product WHERE Sku LIKE @prefix",
            new DataParameter("prefix", ReferenceScaleManifest.ProductSkuPrefix + "%"));

        if (!request.ReplaceExisting && existingProducts >= targets.Products)
        {
            stopwatch.Stop();
            return new ReferenceScaleLoadResult
            {
                AlreadyLoaded = true,
                Targets = targets,
                Elapsed = stopwatch.Elapsed
            };
        }

        if (request.EnsureBmwVehicleSeed)
            await _vehicleSeedLoader.SeedAsync(cancellationToken);

        var configurationsInserted = await ExpandConfigurationsAsync(targets.Configurations, cancellationToken);
        var manufacturerId = await EnsureBmwManufacturerAsync(cancellationToken);
        var oemUpserted = await UpsertOemEntriesAsync(manufacturerId, targets.OemEntries, cancellationToken);
        var productsInserted = await InsertProductsAsync(targets.Products, cancellationToken);
        var claimsInserted = await InsertFitmentClaimsAsync(targets.FitmentClaims, cancellationToken);
        var mapsInserted = await InsertProductOemMapsAsync(cancellationToken);

        stopwatch.Stop();
        return new ReferenceScaleLoadResult
        {
            Targets = targets,
            ConfigurationsInserted = configurationsInserted,
            OemEntriesUpserted = oemUpserted,
            ProductsInserted = productsInserted,
            FitmentClaimsInserted = claimsInserted,
            ProductOemMapsInserted = mapsInserted,
            Elapsed = stopwatch.Elapsed
        };
    }

    public async Task PurgeAsync(CancellationToken cancellationToken)
    {
        await _dataProvider.ExecuteNonQueryAsync(@"
DELETE q
FROM TP_CE_FitmentQualifier q
INNER JOIN TP_CE_FitmentClaim c ON c.Id = q.FitmentClaimId
WHERE c.SourceReference LIKE @prefix;",
            new DataParameter("prefix", ReferenceScaleManifest.ProvenancePrefix + "%"));

        await _dataProvider.ExecuteNonQueryAsync(
            "DELETE FROM TP_CE_FitmentClaim WHERE SourceReference LIKE @prefix;",
            new DataParameter("prefix", ReferenceScaleManifest.ProvenancePrefix + "%"));

        await _dataProvider.ExecuteNonQueryAsync(@"
DELETE map
FROM TP_CE_ProductOemMap map
INNER JOIN Product p ON p.Id = map.ProductId
WHERE p.Sku LIKE @skuPrefix;",
            new DataParameter("skuPrefix", ReferenceScaleManifest.ProductSkuPrefix + "%"));

        await _dataProvider.ExecuteNonQueryAsync(@"
DELETE idx
FROM TP_CE_SearchIndex idx
INNER JOIN Product p ON p.Id = idx.ProductId
WHERE p.Sku LIKE @skuPrefix;",
            new DataParameter("skuPrefix", ReferenceScaleManifest.ProductSkuPrefix + "%"));

        await _dataProvider.ExecuteNonQueryAsync(
            "DELETE FROM Product WHERE Sku LIKE @skuPrefix;",
            new DataParameter("skuPrefix", ReferenceScaleManifest.ProductSkuPrefix + "%"));

        await _dataProvider.ExecuteNonQueryAsync(
            "DELETE FROM TP_CE_VehicleConfiguration WHERE Fingerprint LIKE @prefix;",
            new DataParameter("prefix", ReferenceScaleManifest.ConfigurationFingerprintPrefix + "%"));

        await _dataProvider.ExecuteNonQueryAsync(
            "DELETE FROM TP_CE_OemNumber WHERE NormalizedNumber LIKE @prefix;",
            new DataParameter("prefix", ReferenceScaleManifest.OemNormalizedPrefix + "%"));
    }

    private async Task<int> ExpandConfigurationsAsync(int targetActiveConfigurations, CancellationToken cancellationToken)
    {
        var totalActive = await ScalarCountAsync(
            "SELECT COUNT(*) FROM TP_CE_VehicleConfiguration WHERE IsActive = 1");

        var syntheticNeeded = Math.Max(0, targetActiveConfigurations - totalActive);
        if (syntheticNeeded <= 0)
            return 0;

        var existingSynthetic = await ScalarCountAsync(
            "SELECT COUNT(*) FROM TP_CE_VehicleConfiguration WHERE Fingerprint LIKE @prefix",
            new DataParameter("prefix", ReferenceScaleManifest.ConfigurationFingerprintPrefix + "%"));

        var inserted = await _dataProvider.ExecuteNonQueryAsync(@"
;WITH template AS (
    SELECT TOP (1)
        GenerationId,
        BodyId,
        EngineId,
        MarketId,
        TrimName,
        ProductionFromYear,
        ProductionToYear
    FROM TP_CE_VehicleConfiguration
    WHERE IsActive = 1
    ORDER BY Id
),
tally AS (
    SELECT TOP (@count) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects o1
    CROSS JOIN sys.all_objects o2
)
INSERT INTO TP_CE_VehicleConfiguration
    (GenerationId, BodyId, EngineId, MarketId, TrimName, ProductionFromYear, ProductionToYear, Fingerprint, IsActive)
SELECT
    template.GenerationId,
    template.BodyId,
    template.EngineId,
    template.MarketId,
    CONCAT(template.TrimName, ' REF ', tally.n),
    template.ProductionFromYear,
    template.ProductionToYear,
    CONCAT(@fingerprintPrefix, RIGHT('00000000' + CAST(@existingSynthetic + tally.n AS varchar(8)), 8)),
    CAST(1 AS bit)
FROM tally
CROSS JOIN template
WHERE NOT EXISTS (
    SELECT 1
    FROM TP_CE_VehicleConfiguration existing
    WHERE existing.Fingerprint = CONCAT(@fingerprintPrefix, RIGHT('00000000' + CAST(@existingSynthetic + tally.n AS varchar(8)), 8)));",
            new DataParameter("count", syntheticNeeded),
            new DataParameter("existingSynthetic", existingSynthetic),
            new DataParameter("fingerprintPrefix", ReferenceScaleManifest.ConfigurationFingerprintPrefix));

        return inserted;
    }

    private async Task<int> EnsureBmwManufacturerAsync(CancellationToken cancellationToken)
    {
        var existing = (await _oemRepository.GetManufacturersAsync(cancellationToken))
            .FirstOrDefault(m => string.Equals(m.Code, BmwReferenceCatalog.MakeCode, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
            return existing.Id;

        await _oemRepository.CreateManufacturerAsync(new Domain.Oem.Manufacturer
        {
            Code = BmwReferenceCatalog.MakeCode,
            Name = BmwReferenceCatalog.MakeName,
            IsOeBrand = true,
            IsActive = true
        }, cancellationToken);

        existing = (await _oemRepository.GetManufacturersAsync(cancellationToken))
            .Single(m => string.Equals(m.Code, BmwReferenceCatalog.MakeCode, StringComparison.OrdinalIgnoreCase));

        return existing.Id;
    }

    private async Task<int> UpsertOemEntriesAsync(int manufacturerId, int targetCount, CancellationToken cancellationToken)
    {
        var existing = await ScalarCountAsync(
            "SELECT COUNT(*) FROM TP_CE_OemNumber WHERE NormalizedNumber LIKE @prefix",
            new DataParameter("prefix", ReferenceScaleManifest.OemNormalizedPrefix + "%"));

        var remaining = Math.Max(0, targetCount - existing);
        if (remaining == 0)
            return 0;

        var upserted = 0;
        var startSequence = existing + 1;
        for (var offset = 0; offset < remaining; offset += OemBatchSize)
        {
            var batchSize = Math.Min(OemBatchSize, remaining - offset);
            var batch = new List<OemNumber>(batchSize);
            for (var i = 0; i < batchSize; i++)
            {
                var sequence = startSequence + offset + i;
                batch.Add(new OemNumber
                {
                    ManufacturerId = manufacturerId,
                    DisplayNumber = ReferenceScaleDataGenerator.OemDisplayNumber(sequence),
                    NormalizedNumber = ReferenceScaleDataGenerator.OemNormalizedNumber(sequence),
                    IsObsolete = false,
                    IsActive = true
                });
            }

            var result = await _oemRepository.BulkUpsertOemNumbersAsync(batch, cancellationToken);
            upserted += result.Inserted + result.Updated;
        }

        return upserted;
    }

    private async Task<int> InsertProductsAsync(int targetCount, CancellationToken cancellationToken)
    {
        var existing = await ScalarCountAsync(
            "SELECT COUNT(*) FROM Product WHERE Sku LIKE @prefix",
            new DataParameter("prefix", ReferenceScaleManifest.ProductSkuPrefix + "%"));

        var remaining = Math.Max(0, targetCount - existing);
        if (remaining == 0)
            return 0;

        var inserted = 0;
        var startSequence = existing + 1;
        var now = DateTime.UtcNow;

        for (var offset = 0; offset < remaining; offset += ProductBatchSize)
        {
            var batchSize = Math.Min(ProductBatchSize, remaining - offset);
            var products = new List<Product>(batchSize);
            for (var i = 0; i < batchSize; i++)
            {
                var sequence = startSequence + offset + i;
                products.Add(new Product
                {
                    ProductTypeId = (int)ProductType.SimpleProduct,
                    VisibleIndividually = true,
                    Name = ReferenceScaleDataGenerator.ProductName(sequence),
                    Sku = ReferenceScaleDataGenerator.ProductSku(sequence),
                    Published = true,
                    Price = 10 + (sequence % 500),
                    StockQuantity = 100,
                    ManageInventoryMethodId = (int)ManageInventoryMethod.DontManageStock,
                    OrderMinimumQuantity = 1,
                    OrderMaximumQuantity = 10_000,
                    AdminComment = ReferenceScaleManifest.ProvenancePrefix,
                    CreatedOnUtc = now,
                    UpdatedOnUtc = now
                });
            }

            await _dataProvider.BulkInsertEntitiesAsync(products);
            inserted += products.Count;
        }

        return inserted;
    }

    private async Task<int> InsertFitmentClaimsAsync(int targetClaims, CancellationToken cancellationToken)
    {
        var existing = await ScalarCountAsync(
            "SELECT COUNT(*) FROM TP_CE_FitmentClaim WHERE SourceReference LIKE @prefix",
            new DataParameter("prefix", ReferenceScaleManifest.ProvenancePrefix + "%"));

        if (existing >= targetClaims)
            return 0;

        var productCount = Math.Max(1, await ScalarCountAsync(
            "SELECT COUNT(*) FROM Product WHERE Sku LIKE @prefix",
            new DataParameter("prefix", ReferenceScaleManifest.ProductSkuPrefix + "%")));

        var claimsPerProduct = Math.Max(1, (int)Math.Ceiling(targetClaims / (double)productCount));

        return await _dataProvider.ExecuteNonQueryAsync(@"
;WITH products AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) - 1 AS productIndex
    FROM Product
    WHERE Sku LIKE @skuPrefix
),
configs AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) - 1 AS configIndex
    FROM TP_CE_VehicleConfiguration
    WHERE IsActive = 1
),
slots AS (
    SELECT TOP (@claimsPerProduct) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS slot
    FROM sys.all_objects
),
configCount AS (
    SELECT COUNT(*) AS total FROM configs
)
INSERT INTO TP_CE_FitmentClaim
    (ProductId, VehicleConfigurationId, OemNumberId, FitmentStatusId, Confidence, SafetyClassId,
     SourceKindId, SourceReference, CreatedBy, ProvenanceCreatedUtc, LastVerifiedUtc,
     ValidFromUtc, ValidToUtc, IsPublished, IsActive)
SELECT
    p.Id,
    c.Id,
    NULL,
    @fitsStatus,
    CAST(0.9500 AS decimal(5,4)),
    @standardSafety,
    @importedFeedSource,
    CONCAT(@provenancePrefix, ':claim:', RIGHT('00000000' + CAST(p.productIndex + 1 AS varchar(8)), 8), ':', s.slot),
    @createdBy,
    SYSUTCDATETIME(),
    SYSUTCDATETIME(),
    NULL,
    NULL,
    CAST(1 AS bit),
    CAST(1 AS bit)
FROM products p
CROSS JOIN slots s
CROSS JOIN configCount cc
INNER JOIN configs c
    ON c.configIndex = ((p.productIndex * @claimsPerProduct + s.slot) % NULLIF(cc.total, 0))
WHERE NOT EXISTS (
    SELECT 1
    FROM TP_CE_FitmentClaim existing
    WHERE existing.SourceReference = CONCAT(@provenancePrefix, ':claim:', RIGHT('00000000' + CAST(p.productIndex + 1 AS varchar(8)), 8), ':', s.slot));",
            new DataParameter("skuPrefix", ReferenceScaleManifest.ProductSkuPrefix + "%"),
            new DataParameter("claimsPerProduct", claimsPerProduct),
            new DataParameter("fitsStatus", (int)FitmentStatus.Fits),
            new DataParameter("standardSafety", (int)SafetyClass.Standard),
            new DataParameter("importedFeedSource", (int)FitmentSourceKind.ImportedFeed),
            new DataParameter("provenancePrefix", ReferenceScaleManifest.ProvenancePrefix),
            new DataParameter("createdBy", ReferenceScaleManifest.CreatedBy));
    }

    private async Task<int> InsertProductOemMapsAsync(CancellationToken cancellationToken)
    {
        return await _dataProvider.ExecuteNonQueryAsync(@"
;WITH products AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS seq
    FROM Product
    WHERE Sku LIKE @skuPrefix
),
oems AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS seq
    FROM TP_CE_OemNumber
    WHERE NormalizedNumber LIKE @oemPrefix
)
INSERT INTO TP_CE_ProductOemMap (ProductId, OemNumberId, IsPrimary, CreatedUtc)
SELECT p.Id, o.Id, CAST(1 AS bit), SYSUTCDATETIME()
FROM products p
INNER JOIN oems o ON o.seq = p.seq
WHERE NOT EXISTS (
    SELECT 1 FROM TP_CE_ProductOemMap existing
    WHERE existing.ProductId = p.Id AND existing.OemNumberId = o.Id);",
            new DataParameter("skuPrefix", ReferenceScaleManifest.ProductSkuPrefix + "%"),
            new DataParameter("oemPrefix", ReferenceScaleManifest.OemNormalizedPrefix + "%"));
    }

    private async Task<int> ScalarCountAsync(string sql)
        => (await _dataProvider.QueryAsync<ScalarRow>(sql)).Single().Value;

    private async Task<int> ScalarCountAsync(string sql, DataParameter parameter)
        => (await _dataProvider.QueryAsync<ScalarRow>(sql, parameter)).Single().Value;

    private sealed class ScalarRow
    {
        public int Value { get; set; }
    }
}
