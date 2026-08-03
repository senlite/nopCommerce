using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class FitmentPublicationPolicyServiceTests
{
    [Test]
    public async Task TryPublishAsync_Should_Block_Ai_Source_And_Safety_Critical_Low_Confidence()
    {
        var service = new FitmentPublicationPolicyService(new FakeWriteRepository());

        var aiClaim = new FitmentClaim
        {
            Id = 1,
            ProductId = 10,
            VehicleConfigurationId = 20,
            Confidence = 0.99m,
            SafetyClass = SafetyClass.Standard,
            Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.AiInference }
        };

        var safetyClaim = new FitmentClaim
        {
            Id = 2,
            ProductId = 10,
            VehicleConfigurationId = 20,
            Confidence = 0.90m,
            SafetyClass = SafetyClass.SafetyCritical,
            Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.CuratorManual }
        };

        (await service.TryPublishAsync(aiClaim, CancellationToken.None)).Should().BeFalse();
        (await service.TryPublishAsync(safetyClaim, CancellationToken.None)).Should().BeFalse();
    }

    private sealed class FakeWriteRepository : IFitmentClaimWriteRepository
    {
        public Task UpsertAsync(FitmentClaim claim, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetPublishedAsync(int claimId, bool isPublished, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetStatusAsync(int claimId, FitmentStatus status, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
