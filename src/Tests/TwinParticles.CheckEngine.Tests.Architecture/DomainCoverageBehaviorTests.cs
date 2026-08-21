using System;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Licensing;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Domain.Tenancy;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class DomainCoverageBehaviorTests
{
    [Test]
    public void Confidence_Create_Should_Reject_Out_Of_Range_And_Equal_By_Value()
    {
        var act = () => Confidence.Create(1.01m);
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*vin.confidence_out_of_range*");

        var left = Confidence.Create(0.50m);
        var right = Confidence.Create(0.50m);
        left.Equals(right).Should().BeTrue();
        left.Equals((Confidence?)null).Should().BeFalse();
        left.Equals((object?)right).Should().BeTrue();
        left.Equals((object?)0.50m).Should().BeFalse();
        left.GetHashCode().Should().Be(right.GetHashCode());
        left.ToString().Should().Be("0.50");
    }

    [TestCase(null, LicenceTier.Unknown)]
    [TestCase("", LicenceTier.Unknown)]
    [TestCase("   ", LicenceTier.Unknown)]
    [TestCase("single_store", LicenceTier.SingleStore)]
    [TestCase("Multi Store", LicenceTier.MultiStore)]
    [TestCase("OEM", LicenceTier.OemRedistribution)]
    [TestCase("oem-redistribution", LicenceTier.Unknown)]
    [TestCase("not-a-tier", LicenceTier.Unknown)]
    public void ParseTier_Should_Normalize_Known_Tokens(string? raw, LicenceTier expected)
    {
        LicenceTierEntitlements.ParseTier(raw).Should().Be(expected);
    }

    [Test]
    public void PayoutReconciliationResult_Fail_Should_Record_Absolute_Variance()
    {
        var result = PayoutReconciliationResult.Fail(10.00m, 12.50m);
        result.Succeeded.Should().BeFalse();
        result.LocalNetPayout.Should().Be(10.00m);
        result.ErpTotal.Should().Be(12.50m);
        result.Variance.Should().Be(2.50m);
        result.WithinTolerance.Should().BeFalse();
    }

    [Test]
    public void TenantContext_Unresolved_Should_Use_Zero_Tenant_Id()
    {
        TenantContext.Unresolved.TenantId.Should().Be(TenantCacheKey.UnresolvedTenantId);
        TenantContext.Unresolved.IsControlPlane.Should().BeFalse();
    }

    [Test]
    public void TenantCacheKey_SharesTenant_Should_Reject_Blank_Keys()
    {
        TenantCacheKey.SharesTenant("ce:t1:fitment", "ce:t1:fitment").Should().BeTrue();
        TenantCacheKey.SharesTenant("ce:t1:fitment", "ce:t2:fitment").Should().BeFalse();
        TenantCacheKey.SharesTenant(" ", "ce:t1:x").Should().BeFalse();
        TenantCacheKey.SharesTenant("ce:t1:x", null!).Should().BeFalse();
    }
}
