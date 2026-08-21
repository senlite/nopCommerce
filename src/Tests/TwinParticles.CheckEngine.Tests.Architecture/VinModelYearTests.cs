using System;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VinModelYearTests
{
    [TestCase('A', 2010)]
    [TestCase('B', 2011)]
    [TestCase('C', 2012)]
    [TestCase('D', 2013)]
    [TestCase('E', 2014)]
    [TestCase('F', 2015)]
    [TestCase('G', 2016)]
    [TestCase('H', 2017)]
    [TestCase('J', 2018)]
    [TestCase('K', 2019)]
    [TestCase('L', 2020)]
    [TestCase('M', 2021)]
    [TestCase('N', 2022)]
    [TestCase('P', 2023)]
    [TestCase('R', 2024)]
    [TestCase('S', 2025)]
    [TestCase('T', 2026)]
    [TestCase('V', 2027)]
    [TestCase('W', 2028)]
    [TestCase('X', 2029)]
    [TestCase('Y', 2030)]
    [TestCase('1', 2001)]
    [TestCase('2', 2002)]
    [TestCase('3', 2003)]
    [TestCase('4', 2004)]
    [TestCase('5', 2005)]
    [TestCase('6', 2006)]
    [TestCase('7', 2007)]
    [TestCase('8', 2008)]
    [TestCase('9', 2009)]
    public void TryDecode_Should_Map_Iso_Year_Codes(char code, int year)
    {
        VinModelYear.TryDecode(code, out var decoded).Should().BeTrue();
        decoded.Should().Be(year);
    }

    [TestCase('I')]
    [TestCase('O')]
    [TestCase('Q')]
    [TestCase('Z')]
    [TestCase('0')]
    [TestCase('a')]
    public void TryDecode_Should_Reject_Unused_Year_Codes(char code)
    {
        VinModelYear.TryDecode(code, out var decoded).Should().BeFalse();
        decoded.Should().Be(0);
    }

    [Test]
    public void DecodeFromVin_Should_Read_Position_Ten()
    {
        VinModelYear.DecodeFromVin("WBA8E9G51GNT12345").Should().Be(2016);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("SHORT")]
    [TestCase("WBA8E9G51GNT123456")]
    public void DecodeFromVin_Should_Return_Null_When_Length_Is_Not_Seventeen(string? vin)
    {
        VinModelYear.DecodeFromVin(vin!).Should().BeNull();
    }

    [Test]
    public void DecodeFromVin_Should_Return_Null_When_Year_Code_Is_Unused()
    {
        VinModelYear.DecodeFromVin("WBA8E9G51INT12345").Should().BeNull();
    }
}
