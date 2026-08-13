using FluentAssertions;
using Nop.Plugin.Shipping.Bosta.Services;
using NUnit.Framework;

namespace Nop.Plugin.Shipping.Bosta.Tests;

[TestFixture]
public class BostaRateCalculatorTests
{
    private readonly BostaRateCalculator _calculator = new();

    [Test]
    public void First_Kilogram_Is_Included_In_The_Zone_Base_Fee()
    {
        _calculator.CalculateRate("Cairo", 0.5m).Should().Be(45m);
        _calculator.CalculateRate("Cairo", 1m).Should().Be(45m);
    }

    [Test]
    public void Additional_Weight_Is_Billed_Per_Rounded_Up_Kilogram()
    {
        // 2.3 kg over Cairo base: ceil(2.3 - 1) = 2 additional kg × 10 = 20 on top of 45.
        _calculator.CalculateRate("Cairo", 2.3m).Should().Be(65m);
    }

    [Test]
    public void Zones_Have_Distinct_Base_Fees()
    {
        _calculator.ResolveZoneBaseFee("Cairo").Should().Be(45m);
        _calculator.ResolveZoneBaseFee("Alexandria").Should().Be(55m);
        _calculator.ResolveZoneBaseFee("Upper Egypt").Should().Be(70m);
    }

    [Test]
    public void Unknown_Zone_Falls_Back_To_The_National_Rate()
    {
        _calculator.ResolveZoneBaseFee(null).Should().Be(80m);
        _calculator.ResolveZoneBaseFee("Nowhere").Should().Be(80m);
    }

    [Test]
    public void Zone_Matching_Is_Case_Insensitive()
    {
        _calculator.ResolveZoneBaseFee("cairo").Should().Be(45m);
    }
}
