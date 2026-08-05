using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Images;

namespace TwinParticles.CheckEngine.Infrastructure.Images;

public sealed class SqlProductImageRepository : IProductImageRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlProductImageRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<ProductImageRecord?> GetPrimaryAsync(int productId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ProductImageRow>(
            @"SELECT ProductId, PictureId, SourceUrl, IsPlaceholder, QuarantineStatus, CreatedUtc
FROM TP_CE_ProductImageMeta
WHERE ProductId = @productId",
            new DataParameter("productId", productId));

        return rows.Select(Map).FirstOrDefault();
    }

    public async Task UpsertPrimaryAsync(ProductImageRecord record, CancellationToken cancellationToken)
    {
        const string updateSql = @"UPDATE TP_CE_ProductImageMeta
SET PictureId = @pictureId,
    SourceUrl = @sourceUrl,
    IsPlaceholder = @isPlaceholder,
    QuarantineStatus = @quarantineStatus
WHERE ProductId = @productId";

        var updated = await _dataProvider.ExecuteNonQueryAsync(updateSql,
            new DataParameter("pictureId", record.PictureId),
            new DataParameter("sourceUrl", record.SourceUrl),
            new DataParameter("isPlaceholder", record.IsPlaceholder),
            new DataParameter("quarantineStatus", (int)record.QuarantineStatus),
            new DataParameter("productId", record.ProductId));

        if (updated > 0)
            return;

        if (record.CreatedUtc == default)
            record.CreatedUtc = DateTime.UtcNow;

        await _dataProvider.ExecuteNonQueryAsync(
            @"INSERT INTO TP_CE_ProductImageMeta
(ProductId, PictureId, SourceUrl, IsPlaceholder, QuarantineStatus, CreatedUtc)
VALUES
(@productId, @pictureId, @sourceUrl, @isPlaceholder, @quarantineStatus, @createdUtc)",
            new DataParameter("productId", record.ProductId),
            new DataParameter("pictureId", record.PictureId),
            new DataParameter("sourceUrl", record.SourceUrl),
            new DataParameter("isPlaceholder", record.IsPlaceholder),
            new DataParameter("quarantineStatus", (int)record.QuarantineStatus),
            new DataParameter("createdUtc", record.CreatedUtc));
    }

    private static ProductImageRecord Map(ProductImageRow row)
        => new()
        {
            ProductId = row.ProductId,
            PictureId = row.PictureId,
            SourceUrl = row.SourceUrl,
            IsPlaceholder = row.IsPlaceholder,
            QuarantineStatus = (QuarantineStatus)row.QuarantineStatus,
            CreatedUtc = row.CreatedUtc
        };

    private sealed class ProductImageRow
    {
        public int ProductId { get; set; }
        public int PictureId { get; set; }
        public string? SourceUrl { get; set; }
        public bool IsPlaceholder { get; set; }
        public int QuarantineStatus { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
