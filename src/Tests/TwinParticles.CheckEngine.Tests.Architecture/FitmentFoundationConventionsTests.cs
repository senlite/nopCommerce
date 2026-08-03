using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class FitmentFoundationConventionsTests
{
    [Test]
    public void FitmentDomain_Should_Define_Core_Contracts()
    {
        typeof(TwinParticles.CheckEngine.Domain.Fitment.FitmentClaim).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Domain.Fitment.FitmentClaimQualifier).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Domain.Fitment.FitmentClaimProvenance).IsClass.Should().BeTrue();

        System.Enum.IsDefined(typeof(TwinParticles.CheckEngine.Domain.Fitment.FitmentStatus), "Fits").Should().BeTrue();
        System.Enum.IsDefined(typeof(TwinParticles.CheckEngine.Domain.Fitment.SafetyClass), "SafetyCritical").Should().BeTrue();
        System.Enum.IsDefined(typeof(TwinParticles.CheckEngine.Domain.Fitment.FitmentSourceKind), "AiInference").Should().BeTrue();
    }

    [Test]
    public void FitmentApplication_Should_Expose_Evaluation_And_Review_Services()
    {
        typeof(TwinParticles.CheckEngine.Application.Fitment.FitmentEvaluationService).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Application.Fitment.FitmentPublicationPolicyService).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Application.Fitment.FitmentReviewService).IsClass.Should().BeTrue();
    }
}
