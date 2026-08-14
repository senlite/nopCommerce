using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// The publish threshold is operator-tunable (FR-313), but no configuration may weaken the
/// safety-critical gate (FR-316, AC-028.1).
/// </summary>
[TestFixture]
public class FitmentPublicationThresholdTests
{
    private static FitmentClaim Claim(decimal confidence, SafetyClass safetyClass) => new()
    {
        Id = 1,
        ProductId = 10,
        VehicleConfigurationId = 20,
        Confidence = confidence,
        SafetyClass = safetyClass,
        IsActive = true,
        Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.CuratorManual }
    };

    [Test]
    public async Task Operator_Should_Be_Able_To_Raise_The_Standard_Threshold()
    {
        var strict = new FitmentPublicationOptions { MinPublishConfidence = 0.97m };
        var service = new FitmentPublicationPolicyService(new FakeWriteRepository(), strict);

        (await service.TryPublishAsync(Claim(0.90m, SafetyClass.Standard), CancellationToken.None))
            .Should().BeFalse("the operator raised the bar above this claim's confidence");
        (await service.TryPublishAsync(Claim(0.98m, SafetyClass.Standard), CancellationToken.None))
            .Should().BeTrue();
    }

    [Test]
    public async Task Operator_Should_Be_Able_To_Lower_The_Standard_Threshold()
    {
        var relaxed = new FitmentPublicationOptions { MinPublishConfidence = 0.70m };
        var service = new FitmentPublicationPolicyService(new FakeWriteRepository(), relaxed);

        (await service.TryPublishAsync(Claim(0.75m, SafetyClass.Standard), CancellationToken.None))
            .Should().BeTrue();
    }

    [Test]
    public async Task Configuration_Should_Not_Be_Able_To_Weaken_The_Safety_Critical_Gate()
    {
        // An operator attempts to publish brake parts at a permissive threshold.
        var permissive = new FitmentPublicationOptions
        {
            MinPublishConfidence = 0.10m,
            SafetyCriticalMinPublishConfidence = 0.10m
        };
        var service = new FitmentPublicationPolicyService(new FakeWriteRepository(), permissive);

        (await service.TryPublishAsync(Claim(0.94m, SafetyClass.SafetyCritical), CancellationToken.None))
            .Should().BeFalse("no setting may publish a safety-critical claim below the floor");
        (await service.TryPublishAsync(Claim(0.96m, SafetyClass.SafetyCritical), CancellationToken.None))
            .Should().BeTrue();
    }

    [Test]
    public async Task Operator_Should_Be_Able_To_Raise_The_Safety_Critical_Threshold()
    {
        var strict = new FitmentPublicationOptions { SafetyCriticalMinPublishConfidence = 0.99m };
        var service = new FitmentPublicationPolicyService(new FakeWriteRepository(), strict);

        (await service.TryPublishAsync(Claim(0.96m, SafetyClass.SafetyCritical), CancellationToken.None))
            .Should().BeFalse("raising the safety-critical bar is always permitted");
    }

    [Test]
    public async Task Ai_Claims_Should_Never_Publish_Regardless_Of_Threshold()
    {
        var permissive = new FitmentPublicationOptions { MinPublishConfidence = 0.01m };
        var service = new FitmentPublicationPolicyService(new FakeWriteRepository(), permissive);

        var claim = Claim(1.0m, SafetyClass.Standard);
        claim.Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.AiInference };

        (await service.TryPublishAsync(claim, CancellationToken.None)).Should().BeFalse();
    }

    [Test]
    public void Default_Options_Should_Match_The_Documented_Thresholds()
    {
        var defaults = new FitmentPublicationOptions();

        defaults.ResolveThreshold(SafetyClass.Standard).Should().Be(0.85m);
        defaults.ResolveThreshold(SafetyClass.Elevated).Should().Be(0.85m);
        defaults.ResolveThreshold(SafetyClass.SafetyCritical)
            .Should().Be(FitmentPublicationOptions.SafetyCriticalConfidenceFloor);
    }

    private sealed class FakeWriteRepository : IFitmentClaimWriteRepository
    {
        public Task UpsertAsync(FitmentClaim claim, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetPublishedAsync(int claimId, bool isPublished, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetStatusAsync(int claimId, FitmentStatus status, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
