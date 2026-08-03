using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class OemAdminConventionsTests
{
    [Test]
    public void OemAdminService_Should_Expose_Crud_For_Manufacturer_Number_And_Relation()
    {
        var type = typeof(TwinParticles.CheckEngine.Application.Oem.OemAdminService);

        var methods = type.GetMethods().Select(x => x.Name).ToList();

        methods.Should().Contain("GetManufacturersAsync");
        methods.Should().Contain("CreateManufacturerAsync");
        methods.Should().Contain("UpdateManufacturerAsync");
        methods.Should().Contain("DeleteManufacturerAsync");

        methods.Should().Contain("GetOemNumbersAsync");
        methods.Should().Contain("CreateOemNumberAsync");
        methods.Should().Contain("UpdateOemNumberAsync");
        methods.Should().Contain("DeleteOemNumberAsync");

        methods.Should().Contain("GetRelationsAsync");
        methods.Should().Contain("CreateRelationAsync");
        methods.Should().Contain("UpdateRelationAsync");
        methods.Should().Contain("DeleteRelationAsync");
    }

    [Test]
    public void OemResolveService_Should_Expose_Resolve_Hook()
    {
        var type = typeof(TwinParticles.CheckEngine.Application.Oem.OemResolveService);

        type.GetMethod("ResolveAsync").Should().NotBeNull();
    }
}
