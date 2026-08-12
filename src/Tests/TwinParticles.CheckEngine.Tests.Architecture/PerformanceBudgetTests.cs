using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class PerformanceBudgetTests
{
    [Test]
    public async Task FitmentEvaluation_Uncached_P95_Should_Stay_Under_50ms_NFR001()
    {
        var claim = new FitmentClaim
        {
            Id = 1,
            ProductId = 10,
            VehicleConfigurationId = 20,
            Status = FitmentStatus.Fits,
            Confidence = 0.95m,
            IsPublished = true,
            IsActive = true
        };

        var service = new FitmentEvaluationService(new StaticClaimsRepository(claim), new NoCache());

        // Warm
        for (var i = 0; i < 10; i++)
        {
            await service.EvaluateAsync(new FitmentEvaluationContext
            {
                ProductId = 10,
                VehicleConfigurationId = 20
            }, CancellationToken.None);
        }

        var samples = new long[100];
        for (var i = 0; i < samples.Length; i++)
        {
            var sw = Stopwatch.StartNew();
            await service.EvaluateAsync(new FitmentEvaluationContext
            {
                ProductId = 10,
                VehicleConfigurationId = 20
            }, CancellationToken.None);
            sw.Stop();
            samples[i] = sw.ElapsedMilliseconds;
        }

        Array.Sort(samples);
        var p95Index = (int)Math.Ceiling(samples.Length * 0.95) - 1;
        var p95 = samples[Math.Clamp(p95Index, 0, samples.Length - 1)];

        p95.Should().BeLessThan(50,
            "NFR-001: uncached FitmentEvaluationService.EvaluateAsync p95 must stay under 50ms in CI microbench");
    }

    [Test]
    public void Vin_TryCreate_1000_Iterations_Should_Stay_Under_40ms_NFR003_005()
    {
        const string vin = "1HGCM82633A004352";

        // Warm
        for (var i = 0; i < 50; i++)
            Vin.TryCreate(vin, out _, out _, enforceCheckDigit: true);

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < 1000; i++)
            Vin.TryCreate(vin, out _, out _, enforceCheckDigit: true);
        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(40,
            "NFR-003/NFR-005: Vin.TryCreate average path for 1000 iterations must stay under 40ms total");
    }

    private sealed class StaticClaimsRepository : IFitmentClaimReadRepository
    {
        private readonly IReadOnlyList<FitmentClaim> _claims;

        public StaticClaimsRepository(params FitmentClaim[] claims)
        {
            _claims = claims;
        }

        public Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.FromResult(_claims);

        public Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FitmentClaim>>([]);
    }

    private sealed class NoCache : IFitmentCache
    {
        public Task<FitmentEvaluationResult?> GetAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.FromResult<FitmentEvaluationResult?>(null);

        public Task SetAsync(int productId, int vehicleConfigurationId, FitmentEvaluationResult result, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task InvalidateAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
