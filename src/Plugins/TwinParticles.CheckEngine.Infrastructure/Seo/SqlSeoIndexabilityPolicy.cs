using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Seo;

namespace TwinParticles.CheckEngine.Infrastructure.Seo;

/// <summary>
/// A landing is indexable only when the current catalog contains a published, visible product with
/// an active, published Fits claim for the vehicle. This prevents thin/empty landing pages from
/// entering search indexes (FR-438, FR-440, AC-069.1).
/// </summary>
public sealed class SqlSeoIndexabilityPolicy : ISeoIndexabilityPolicy
{
    private readonly INopDataProvider _dataProvider;

    public SqlSeoIndexabilityPolicy(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public Task<bool> IsVehicleLandingIndexableAsync(
        int vehicleConfigurationId,
        CancellationToken cancellationToken)
        => HasSellableFitAsync(productId: null, vehicleConfigurationId, cancellationToken);

    public Task<bool> IsPartForVehicleLandingIndexableAsync(
        int productId,
        int vehicleConfigurationId,
        CancellationToken cancellationToken)
        => HasSellableFitAsync(productId, vehicleConfigurationId, cancellationToken);

    private async Task<bool> HasSellableFitAsync(
        int? productId,
        int vehicleConfigurationId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _dataProvider.QueryAsync<CountRow>(@"
SELECT COUNT_BIG(1) AS Value
FROM TP_CE_FitmentClaim c
INNER JOIN Product p ON p.Id = c.ProductId
WHERE c.VehicleConfigurationId = @vehicleConfigurationId
  AND (@productId IS NULL OR c.ProductId = @productId)
  AND c.FitmentStatusId = 1
  AND c.IsPublished = 1
  AND c.IsActive = 1
  AND p.Published = 1
  AND p.Deleted = 0
  AND p.VisibleIndividually = 1",
            new DataParameter("vehicleConfigurationId", vehicleConfigurationId),
            new DataParameter("productId", productId));

        return rows.FirstOrDefault()?.Value > 0;
    }

    private sealed class CountRow
    {
        public long Value { get; set; }
    }
}
