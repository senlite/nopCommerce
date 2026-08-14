using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class LicenceReadOnlyContractTests
{
    [Test]
    public void Startup_Should_Register_Licence_Write_Filter()
    {
        var startup = ReadPluginFile(
            "TwinParticles.CheckEngine", "Infrastructure", "CheckEngineStartup.cs");

        startup.Should().Contain("CheckEngineLicenceWriteFilter");
        startup.Should().Contain("options.Filters.AddService<CheckEngineLicenceWriteFilter>()");
    }

    [Test]
    public void Licence_Write_Filter_Should_Block_Admin_Mutations_While_Allowing_Diagnostics()
    {
        var filter = ReadPluginFile(
            "TwinParticles.CheckEngine", "Infrastructure", "Filters", "CheckEngineLicenceWriteFilter.cs");

        filter.Should().Contain("licence.read_only");
        filter.Should().Contain("Status403Forbidden");
        filter.Should().Contain("DiagnosticsAdminController");
        filter.Should().Contain("UninstallAdminController");
        filter.Should().Contain("ADR-009");
        filter.Should().Contain("Missing area = storefront");
        filter.Should().NotContain("BasePublicController");
    }

    [Test]
    public void Heartbeat_Should_Revalidate_The_Stored_Activation_Key()
    {
        var service = ReadPluginFile(
            "TwinParticles.CheckEngine.Application", "Licensing", "DefaultLicenceService.cs");

        service.Should().Contain("GetActivationKeyAsync");
        service.Should().Contain("_licenceKeyValidator.Validate(storedKey)");
    }

    [Test]
    public void Diagnostics_Admin_Should_Expose_Licence_Activation_And_Heartbeat()
    {
        var controller = ReadPluginFile(
            "TwinParticles.CheckEngine", "Controllers", "DiagnosticsAdminController.cs");

        controller.Should().Contain("IActionResult> Activate(");
        controller.Should().Contain("IActionResult> Heartbeat(");
        controller.Should().Contain("ActivateAsync");
        controller.Should().Contain("HeartbeatAsync");
    }

    private static string ReadPluginFile(string project, params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", project, .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {project}/{string.Join('/', relativePath)}");
    }
}
