using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class FitmentEvaluationServiceTests
{
    [Test]
    public async Task EvaluateAsync_Should_Return_Unknown_When_No_Published_Claims()
    {
        var service = new FitmentEvaluationService(new FakeReadRepository(), new FakeCache());

        var result = await service.EvaluateAsync(new FitmentEvaluationContext
        {
            ProductId = 1,
            VehicleConfigurationId = 2
        }, CancellationToken.None);

        result.Outcome.Should().Be(FitmentStatus.Unknown);
        result.ReasonCode.Should().Be("fitment.no_published_claim");
    }

    [Test]
    public async Task EvaluateAsync_Should_Fail_Closed_For_Low_Confidence_Fits()
    {
        var claim = new FitmentClaim
        {
            Id = 1,
            ProductId = 1,
            VehicleConfigurationId = 2,
            Status = FitmentStatus.Fits,
            Confidence = 0.4m,
            IsPublished = true,
            IsActive = true
        };

        var service = new FitmentEvaluationService(new FakeReadRepository(claim), new FakeCache());

        var result = await service.EvaluateAsync(new FitmentEvaluationContext
        {
            ProductId = 1,
            VehicleConfigurationId = 2
        }, CancellationToken.None);

        result.Outcome.Should().Be(FitmentStatus.Unknown);
        result.ReasonCode.Should().Be("fitment.insufficient_confidence");
    }

    [Test]
    public async Task EvaluateAsync_Should_Return_DoesNotFit_When_Production_Year_Out_Of_Range()
    {
        var claim = new FitmentClaim
        {
            Id = 1,
            ProductId = 1,
            VehicleConfigurationId = 2,
            Status = FitmentStatus.Fits,
            Confidence = 0.95m,
            IsPublished = true,
            IsActive = true,
            Qualifier = new FitmentClaimQualifier
            {
                ProductionFromYear = 2010,
                ProductionToYear = 2015
            }
        };

        var service = new FitmentEvaluationService(new FakeReadRepository(claim), new FakeCache());

        var result = await service.EvaluateAsync(new FitmentEvaluationContext
        {
            ProductId = 1,
            VehicleConfigurationId = 2,
            ProductionYear = 2018
        }, CancellationToken.None);

        result.Outcome.Should().Be(FitmentStatus.DoesNotFit);
        result.ReasonCode.Should().Be("fitment.production_year_out_of_range");
    }

    private sealed class FakeReadRepository : IFitmentClaimReadRepository
    {
        private readonly IReadOnlyList<FitmentClaim> _claims;

        public FakeReadRepository(params FitmentClaim[] claims)
        {
            _claims = claims;
        }

        public Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.FromResult(_claims);

        public Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FitmentClaim>>([]);
    }

    private sealed class FakeCache : IFitmentCache
    {
        public Task<FitmentEvaluationResult?> GetAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.FromResult<FitmentEvaluationResult?>(null);

        public Task SetAsync(int productId, int vehicleConfigurationId, FitmentEvaluationResult result, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task InvalidateAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
