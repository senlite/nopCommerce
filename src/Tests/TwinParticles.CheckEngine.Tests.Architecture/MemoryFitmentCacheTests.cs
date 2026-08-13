using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Infrastructure.Fitment;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// The fitment cache must key on the full evaluation context. Keying on product and vehicle alone
/// would return the first computed verdict to every context and could report a fit for a vehicle the
/// part does not fit (AC-026.1).
/// </summary>
[TestFixture]
public class MemoryFitmentCacheTests
{
    private static FitmentEvaluationContext Context(string? steeringSide = null, int? year = null) => new()
    {
        ProductId = 1,
        VehicleConfigurationId = 2,
        SteeringSide = steeringSide,
        ProductionYear = year
    };

    private static FitmentEvaluationResult Result(FitmentStatus outcome) => new()
    {
        Outcome = outcome,
        EffectiveConfidence = 0.95m
    };

    [Test]
    public async Task Different_Contexts_Should_Not_Share_A_Cached_Verdict()
    {
        var cache = new MemoryFitmentCache();

        await cache.SetAsync(Context(steeringSide: "LHD"), Result(FitmentStatus.Fits), CancellationToken.None);

        var rhd = await cache.GetAsync(Context(steeringSide: "RHD"), CancellationToken.None);
        var unknown = await cache.GetAsync(Context(), CancellationToken.None);
        var lhd = await cache.GetAsync(Context(steeringSide: "LHD"), CancellationToken.None);

        rhd.Should().BeNull("a right-hand-drive car must not read a left-hand-drive verdict");
        unknown.Should().BeNull("an unknown drive side must not read a specific verdict");
        lhd.Should().NotBeNull("the exact context is a cache hit");
        lhd!.Outcome.Should().Be(FitmentStatus.Fits);
    }

    [Test]
    public async Task Production_Year_Should_Participate_In_The_Cache_Key()
    {
        var cache = new MemoryFitmentCache();

        await cache.SetAsync(Context(year: 2013), Result(FitmentStatus.Fits), CancellationToken.None);

        (await cache.GetAsync(Context(year: 2020), CancellationToken.None))
            .Should().BeNull("a different build year can resolve differently");
        (await cache.GetAsync(Context(year: 2013), CancellationToken.None))
            .Should().NotBeNull();
    }

    [Test]
    public async Task Invalidate_Should_Clear_Every_Context_For_A_Product_And_Vehicle()
    {
        var cache = new MemoryFitmentCache();

        await cache.SetAsync(Context(steeringSide: "LHD"), Result(FitmentStatus.Fits), CancellationToken.None);
        await cache.SetAsync(Context(steeringSide: "RHD"), Result(FitmentStatus.DoesNotFit), CancellationToken.None);

        await cache.InvalidateAsync(1, 2, CancellationToken.None);

        (await cache.GetAsync(Context(steeringSide: "LHD"), CancellationToken.None)).Should().BeNull();
        (await cache.GetAsync(Context(steeringSide: "RHD"), CancellationToken.None)).Should().BeNull();
    }

    [Test]
    public async Task Case_And_Whitespace_Should_Not_Fragment_The_Cache_Key()
    {
        var cache = new MemoryFitmentCache();

        await cache.SetAsync(Context(steeringSide: "LHD"), Result(FitmentStatus.Fits), CancellationToken.None);

        (await cache.GetAsync(Context(steeringSide: " lhd "), CancellationToken.None))
            .Should().NotBeNull("qualifier comparison is case- and whitespace-insensitive");
    }
}
