using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class HostPhase2ConventionsBySearchTests
{
    [Test]
    public void VehicleAdminController_File_Should_Exist()
    {
        var path = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Plugins", "TwinParticles.CheckEngine", "Controllers", "VehicleAdminController.cs");
        path = System.IO.Path.GetFullPath(path);

        System.IO.File.Exists(path).Should().BeTrue();

        var content = System.IO.File.ReadAllText(path);
        content.Should().Contain("class VehicleAdminController");
        content.Should().Contain("Task<IActionResult> Seed(");
        content.Should().Contain("Task<IActionResult> Makes(");
        content.Should().Contain("Task<IActionResult> Models(");
        content.Should().Contain("Task<IActionResult> Generations(");
        content.Should().Contain("Task<IActionResult> Bodies(");
        content.Should().Contain("Task<IActionResult> Engines(");
        content.Should().Contain("Task<IActionResult> Markets(");
        content.Should().Contain("Task<IActionResult> Configurations(");
        content.Should().Contain("Task<IActionResult> Aliases(");
    }
}
