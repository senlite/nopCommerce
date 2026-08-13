using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// The production guest garage lives in browser storage until account migration. These checks guard
/// the browser/server contract that makes migration survive app restarts and multi-node routing.
/// </summary>
[TestFixture]
public class GuestGarageMigrationContractTests
{
    private static string ReadPluginFile(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }

    [Test]
    public void Storefront_Should_Send_The_Guest_Payload_Not_Only_Its_Key()
    {
        var script = ReadPluginFile("Content", "checkengine-storefront.js");

        script.Should().Contain("JSON.stringify({ guestKey: guestKey, payload: payload })",
            "a key cannot retrieve browser-local data from another process or node");
        script.Should().Contain("window.localStorage.removeItem(GUEST_PAYLOAD_KEY)",
            "the browser copy is removed only after a successful merge");
        script.Should().Contain("return populateVehicleSelector().then",
            "the signed-in UI must show the migrated vehicle immediately without a manual refresh");
    }

    [Test]
    public void Unsafe_Json_Requests_Should_Carry_The_Antiforgery_Token()
    {
        var script = ReadPluginFile("Content", "checkengine-storefront.js");
        var view = ReadPluginFile(
            "Views", "Shared", "Components", "CheckEngineThemeChrome", "Default.cshtml");

        script.Should().Contain("RequestVerificationToken");
        view.Should().Contain("@Html.AntiForgeryToken()");
    }

    [Test]
    public void Anonymous_Guest_Key_Read_Write_Endpoints_Should_Not_Exist()
    {
        var controller = ReadPluginFile("Controllers", "GarageController.cs");

        controller.Should().NotContain("IActionResult> Guest(",
            "a caller-chosen guest key must not expose an anonymous read/write data oracle");
    }

    [Test]
    public void Migrate_Should_Require_Antiforgery_Token()
    {
        var controller = ReadPluginFile("Controllers", "GarageController.cs");

        controller.Should().Contain("[ValidateAntiForgeryToken]");
        controller.Should().NotContain("[IgnoreAntiforgeryToken]\n    public async Task<IActionResult> Migrate");
    }
}
