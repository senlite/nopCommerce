using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class JsonHtmlBoardConventionsTests
{
    [Test]
    public void Remaining_Admin_Json_Gets_Should_Redirect_Browsers_To_Html_Boards()
    {
        AiAdmin().Should().Contain("WantsJsonResponse");
        AiAdmin().Should().Contain("PageOrJsonApi(nameof(Dashboard))");
        AiAdmin().Should().Contain("PageOrJsonApi(nameof(ReviewBoard))");

        SearchAdmin().Should().Contain("WantsJsonResponse");
        SearchAdmin().Should().Contain("RedirectToAction(nameof(Index))");

        Diagnostics().Should().Contain("Views/Admin/DiagnosticsAdmin.cshtml");
        Diagnostics().Should().Contain("WantsJsonResponse");

        ReferenceData().Should().Contain("Views/Admin/ReferenceDataAdmin.cshtml");
        ReferenceData().Should().Contain("WantsJsonResponse");

        ImageAdmin().Should().Contain("Views/Admin/ImageAdmin.cshtml");

        Uninstall().Should().Contain("WantsJsonResponse");
        Uninstall().Should().Contain("RedirectToAction(\"Dashboard\", \"CheckEngine\")");
    }

    [Test]
    public void Admin_Js_Should_Boot_New_Html_Boards()
    {
        var js = ReadPluginFile("Content", "checkengine-admin.js");
        js.Should().Contain("diagnostics-admin");
        js.Should().Contain("reference-admin");
        js.Should().Contain("image-admin");
        js.Should().Contain("DiagnosticsAdmin/Package");
        js.Should().Contain("ReferenceDataAdmin/Status");
        js.Should().Contain("ImageAdmin/Replace");
    }

    private static string AiAdmin() => ReadPluginFile("Controllers", "AiAdminController.cs");
    private static string SearchAdmin() => ReadPluginFile("Controllers", "SearchAdminController.cs");
    private static string Diagnostics() => ReadPluginFile("Controllers", "DiagnosticsAdminController.cs");
    private static string ReferenceData() => ReadPluginFile("Controllers", "ReferenceDataAdminController.cs");
    private static string ImageAdmin() => ReadPluginFile("Controllers", "ImageAdminController.cs");
    private static string Uninstall() => ReadPluginFile("Controllers", "UninstallAdminController.cs");

    private static string ReadPluginFile(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate TwinParticles.CheckEngine/{string.Join('/', relativePath)}");
    }
}
