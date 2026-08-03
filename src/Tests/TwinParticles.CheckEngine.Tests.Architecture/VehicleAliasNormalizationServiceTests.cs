using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VehicleAliasNormalizationServiceTests
{
    [Test]
    public void Normalize_Should_Use_Invariant_Lowercase_For_NonTurkish_Locales()
    {
        var service = new VehicleAliasNormalizationService();

        var result = service.Normalize("  CIVIC I  ", "en");

        result.Should().Be("civic i");
    }

    [Test]
    public void Normalize_Should_Use_Turkish_Lowercase_For_Turkish_Locales()
    {
        var service = new VehicleAliasNormalizationService();

        var result = service.Normalize("IĞDIR", "tr");

        result.Should().Be("ığdır");
    }
}
