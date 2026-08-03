using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class MemoryVehicleAliasCacheTests
{
    [Test]
    public async Task GetAsync_Should_Return_Null_After_Entry_Expires_By_Ttl()
    {
        var cache = new MemoryVehicleAliasCache(System.TimeSpan.FromMilliseconds(20));
        var items = new List<VehicleAliasSearchItem>
        {
            new() { NodeType = "model", NodeId = 1, Locale = "en", AliasText = "Civic" }
        };

        await cache.SetAsync("civ", "en", 10, items, CancellationToken.None);

        await Task.Delay(60);

        var cached = await cache.GetAsync("civ", "en", 10, CancellationToken.None);

        cached.Should().BeNull();
    }

    [Test]
    public async Task InvalidateAsync_Should_Remove_All_Entries_For_Locale()
    {
        var cache = new MemoryVehicleAliasCache();

        await cache.SetAsync("civ", "en", 10, new List<VehicleAliasSearchItem> { new() { NodeType = "model", NodeId = 1, Locale = "en", AliasText = "Civic" } }, CancellationToken.None);
        await cache.SetAsync("cor", "en", 10, new List<VehicleAliasSearchItem> { new() { NodeType = "model", NodeId = 2, Locale = "en", AliasText = "Corolla" } }, CancellationToken.None);
        await cache.SetAsync("civ", "tr", 10, new List<VehicleAliasSearchItem> { new() { NodeType = "model", NodeId = 3, Locale = "tr", AliasText = "Civic TR" } }, CancellationToken.None);

        await cache.InvalidateAsync("en", CancellationToken.None);

        (await cache.GetAsync("civ", "en", 10, CancellationToken.None)).Should().BeNull();
        (await cache.GetAsync("cor", "en", 10, CancellationToken.None)).Should().BeNull();
        (await cache.GetAsync("civ", "tr", 10, CancellationToken.None)).Should().NotBeNull();
    }
}
