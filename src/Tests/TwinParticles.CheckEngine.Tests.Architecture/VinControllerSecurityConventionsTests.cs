using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.VinDecoders;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VinControllerSecurityConventionsTests
{
    [Test]
    public void VinController_Source_Should_Wire_Rate_Limiter()
    {
        var path = LocateVinControllerSource();
        File.Exists(path).Should().BeTrue($"VinController.cs should exist at {path}");

        var source = File.ReadAllText(path);
        source.Should().Contain(nameof(IVinDecodeRateLimiter),
            "VinController must keep IVinDecodeRateLimiter wired (NFR rate-limit / masking abuse surface)");
        source.Should().Contain("TryAcquire",
            "VinController must enforce rate limiting via TryAcquire before decode");
    }

    [Test]
    public void InMemoryVinDecodeRateLimiter_Type_Should_Exist()
    {
        typeof(InMemoryVinDecodeRateLimiter).IsClass.Should().BeTrue();
        typeof(InMemoryVinDecodeRateLimiter).Should().BeAssignableTo<IVinDecodeRateLimiter>();
    }

    private static string LocateVinControllerSource()
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", "Controllers", "VinController.cs");
            if (File.Exists(candidate))
                return candidate;
        }

        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Controllers", "VinController.cs"));
    }
}
