using System;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Infrastructure.Fitment;

public sealed class SqlFitmentReviewQueueRepository : IFitmentReviewQueueRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlFitmentReviewQueueRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public Task EnqueueAsync(int claimId, string reasonCode, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(
            @"INSERT INTO TP_CE_FitmentReviewQueueEvent (ClaimId, ReasonCode, CreatedUtc)
VALUES (@claimId, @reasonCode, @createdUtc)",
            new DataParameter("claimId", claimId),
            new DataParameter("reasonCode", reasonCode),
            new DataParameter("createdUtc", DateTime.UtcNow));
}
