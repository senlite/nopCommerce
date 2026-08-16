using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class PluginAdminViewPackagingTests
{
    [Test]
    public void Csproj_Should_Copy_Every_Admin_View_To_Plugin_Output()
    {
        var pluginDir = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "Plugins", "TwinParticles.CheckEngine"));
        var views = Directory.GetFiles(Path.Combine(pluginDir, "Views", "Admin"), "*.cshtml")
            .Select(Path.GetFileName)
            .ToList();
        var csproj = File.ReadAllText(Path.Combine(pluginDir, "TwinParticles.CheckEngine.csproj"));

        views.Should().NotBeEmpty();
        foreach (var view in views)
        {
            csproj.Should().Contain($"Views\\Admin\\{view}", because: $"{view} must ship in the plugin output or admin pages 500");
        }
    }
}
