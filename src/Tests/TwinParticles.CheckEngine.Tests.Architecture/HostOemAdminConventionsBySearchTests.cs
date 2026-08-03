using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class HostOemAdminConventionsBySearchTests
{
    [Test]
    public void OemAdminController_File_Should_Exist_With_Crud_Actions()
    {
        var path = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Controllers", "OemAdminController.cs");
        path = System.IO.Path.GetFullPath(path);

        System.IO.File.Exists(path).Should().BeTrue();

        var content = System.IO.File.ReadAllText(path);
        content.Should().Contain("class OemAdminController");
        content.Should().Contain("Task<IActionResult> Manufacturers(");
        content.Should().Contain("Task<IActionResult> OemNumbers(");
        content.Should().Contain("Task<IActionResult> Relations(");
        content.Should().Contain("Task<IActionResult> CreateRelation(");
    }

    [Test]
    public void OemController_File_Should_Exist_With_Resolve_Action()
    {
        var path = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Controllers", "OemController.cs");
        path = System.IO.Path.GetFullPath(path);

        System.IO.File.Exists(path).Should().BeTrue();

        var content = System.IO.File.ReadAllText(path);
        content.Should().Contain("class OemController");
        content.Should().Contain("Task<IActionResult> Resolve(");
    }

    [Test]
    public void RouteProvider_Should_Contain_OemAdmin_And_OemResolve_Routes()
    {
        var path = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Infrastructure", "RouteProvider.cs");
        path = System.IO.Path.GetFullPath(path);

        System.IO.File.Exists(path).Should().BeTrue();

        var content = System.IO.File.ReadAllText(path);
        content.Should().Contain("Plugin.TwinParticles.CheckEngine.OemAdmin");
        content.Should().Contain("Admin/CheckEngine/OemAdmin/{action}");
        content.Should().Contain("Plugin.TwinParticles.CheckEngine.OemResolve");
        content.Should().Contain("check-engine/oem/resolve");
    }
}
