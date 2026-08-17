using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AllowedSpecificationKeyCatalogTests
{
    [Test]
    public void Overrides_Should_Add_And_Remove_Keys()
    {
        var catalog = new AllowedSpecificationKeyCatalog(new FakeOverridesSource(new SpecificationKeyOverrides
        {
            Add = ["Torque Spec"],
            Remove = ["Notes"]
        }));

        catalog.IsAllowed("Material").Should().BeTrue();
        catalog.IsAllowed("Notes").Should().BeFalse();
        catalog.IsAllowed("Torque Spec").Should().BeTrue();
    }

    [Test]
    public void MergeKeys_Should_Be_Case_Insensitive()
    {
        var merged = AllowedSpecificationKeyCatalog.MergeKeys(
            ["Material", "Thread"],
            new SpecificationKeyOverrides
            {
                Add = ["torque spec"],
                Remove = ["material"]
            });

        merged.Should().Contain("Torque spec");
        merged.Should().NotContain("Material");
        merged.Should().Contain("Thread");
    }

    [Test]
    public void ParseOverrides_Should_Read_Add_And_Remove_Arrays()
    {
        var overrides = SpecificationKeyOverridesJson.Parse("""{"add":["Bolt Pattern"],"remove":["Notes"]}""");

        overrides.Add.Should().ContainSingle().Which.Should().Be("Bolt Pattern");
        overrides.Remove.Should().ContainSingle().Which.Should().Be("Notes");
    }

    private sealed class FakeOverridesSource : IAllowedSpecificationKeyOverridesSource
    {
        private readonly SpecificationKeyOverrides _overrides;

        public FakeOverridesSource(SpecificationKeyOverrides overrides) => _overrides = overrides;

        public SpecificationKeyOverrides GetOverrides() => _overrides;
    }
}
