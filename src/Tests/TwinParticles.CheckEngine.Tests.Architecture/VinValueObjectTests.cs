using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VinValueObjectTests
{
    [Test]
    public void TryCreate_Should_Normalize_And_Create_Vin_When_Input_Is_Valid()
    {
        var created = Vin.TryCreate(" 1hg-cm82633a004352 ", out var vin, out var errorCode);

        created.Should().BeTrue();
        errorCode.Should().BeNull();
        vin.Should().NotBeNull();
        vin!.Value.Should().Be("1HGCM82633A004352");
    }

    [Test]
    public void TryCreate_Should_Return_InvalidLength_When_Length_Is_Not_Seventeen()
    {
        var created = Vin.TryCreate("123", out var vin, out var errorCode);

        created.Should().BeFalse();
        vin.Should().BeNull();
        errorCode.Should().Be("vin.invalid_length");
    }

    [Test]
    public void TryCreate_Should_Return_InvalidCharset_When_Disallowed_Characters_Are_Present()
    {
        var created = Vin.TryCreate("1HGCM82633A00I352", out var vin, out var errorCode);

        created.Should().BeFalse();
        vin.Should().BeNull();
        errorCode.Should().Be("vin.invalid_charset");
    }

    [Test]
    public void TryCreate_Should_Return_CheckDigitFailed_When_CheckDigit_Is_Invalid()
    {
        var created = Vin.TryCreate("1HGCM82603A004352", out var vin, out var errorCode);

        created.Should().BeFalse();
        vin.Should().BeNull();
        errorCode.Should().Be("vin.check_digit_failed");
    }

    [Test]
    public void TryCreate_Should_Allow_Invalid_CheckDigit_When_CheckDigit_Enforcement_Is_Disabled()
    {
        var created = Vin.TryCreate("1HGCM82603A004352", out var vin, out var errorCode, enforceCheckDigit: false);

        created.Should().BeTrue();
        vin.Should().NotBeNull();
        errorCode.Should().BeNull();
        vin!.Value.Should().Be("1HGCM82603A004352");
    }

    [Test]
    public void IsCheckDigitValid_Should_Return_True_For_Known_Valid_Vin()
    {
        var isValid = Vin.IsCheckDigitValid("1HGCM82633A004352");

        isValid.Should().BeTrue();
    }

    [Test]
    public void ParseSegments_Should_Return_Wmi_Vds_And_Vis()
    {
        var vin = Vin.Create("1HGCM82633A004352");

        var segments = vin.ParseSegments();

        segments.Wmi.Should().Be("1HG");
        segments.Vds.Should().Be("CM8263");
        segments.Vis.Should().Be("3A004352");
    }

    [Test]
    public void VinSegments_Should_Equal_By_Value_And_Reject_Null()
    {
        var vin = Vin.Create("1HGCM82633A004352");
        var left = VinSegments.FromVin(vin);
        var right = VinSegments.FromVin(vin);

        left.Equals(right).Should().BeTrue();
        left.Equals((VinSegments?)null).Should().BeFalse();
        left.Equals((object?)right).Should().BeTrue();
        left.Equals((object?)"not-segments").Should().BeFalse();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Test]
    public void VinSegments_FromVin_Should_Reject_Null_Vin()
    {
        var act = () => VinSegments.FromVin(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
