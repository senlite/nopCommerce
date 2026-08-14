using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class PluginMetadataContractTests
{
    [Test]
    public void Plugin_And_Assembly_Metadata_Should_Match_Release_Contract()
    {
        var root = FindRepositoryRoot();
        var pluginRoot = Path.Combine(root, "src", "Plugins", "TwinParticles.CheckEngine");
        using var pluginJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(pluginRoot, "plugin.json")));
        var document = XDocument.Load(Path.Combine(pluginRoot, "TwinParticles.CheckEngine.csproj"));
        var propertyGroup = document.Root!.Element("PropertyGroup")!;

        var version = pluginJson.RootElement.GetProperty("Version").GetString();
        var assemblyVersion = propertyGroup.Element("AssemblyVersion")!.Value;
        var fileVersion = propertyGroup.Element("FileVersion")!.Value;

        pluginJson.RootElement.GetProperty("Group").GetString().Should().Be("Misc");
        pluginJson.RootElement.GetProperty("FriendlyName").GetString().Should().Be("Check Engine");
        pluginJson.RootElement.GetProperty("SystemName").GetString().Should().Be("TwinParticles.CheckEngine");
        pluginJson.RootElement.GetProperty("FileName").GetString().Should().Be("TwinParticles.CheckEngine.dll");
        pluginJson.RootElement.GetProperty("SupportedVersions")[0].GetString().Should().Be("4.90");
        propertyGroup.Element("Version")!.Value.Should().Be(version);
        assemblyVersion.Should().Be($"{version}.0");
        fileVersion.Should().Be($"{version}.0");
    }

    [Test]
    public void Public_Packaging_Table_Should_Not_Advertise_The_Retired_System_Name()
    {
        var readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "CheckEngine", "README.md"));

        readme.Should().NotContain("`Misc.CheckEngine`");
        readme.Should().Contain("| Check Engine | `TwinParticles.CheckEngine` | `TwinParticles.CheckEngine` | Misc |");
    }

    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             dir is not null;
             dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine")))
                return dir.FullName;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root");
    }
}
