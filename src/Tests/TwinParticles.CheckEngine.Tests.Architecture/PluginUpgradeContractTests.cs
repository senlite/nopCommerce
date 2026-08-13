using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// Upgrading an installed plugin must deliver everything a fresh install would, otherwise an
/// upgraded store silently renders raw resource keys or misses new schema.
/// </summary>
[TestFixture]
public class PluginUpgradeContractTests
{
    private static string ReadPluginSource()
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", "CheckEnginePlugin.cs");
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException("Unable to locate CheckEnginePlugin.cs");
    }

    private static string ExtractMethodBody(string source, string signatureFragment)
    {
        var start = source.IndexOf(signatureFragment, System.StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1, $"{signatureFragment} should exist");

        var openBrace = source.IndexOf('{', start);
        var depth = 0;

        for (var index = openBrace; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                    return source[openBrace..index];
            }
        }

        throw new InvalidDataException($"Could not find the body of {signatureFragment}");
    }

    [Test]
    public void Update_Should_Apply_Pending_Migrations()
    {
        var body = ExtractMethodBody(ReadPluginSource(), "public override async Task UpdateAsync");

        body.Should().Contain("ApplyUpMigrations",
            "migrations live in a separate assembly that nopCommerce does not scan on update");
        body.Should().Contain("MigrationProcessType.Update");
    }

    [Test]
    public void Update_Should_Refresh_Locale_Resources()
    {
        var body = ExtractMethodBody(ReadPluginSource(), "public override async Task UpdateAsync");

        body.Should().Contain("AddOrUpdateLocaleResourcesAsync",
            "strings added by a release must reach stores that upgrade rather than reinstall");
    }

    [Test]
    public void Install_And_Update_Should_Share_One_Resource_Definition()
    {
        var source = ReadPluginSource();

        Regex.Matches(source, @"AddOrUpdateLocaleResourceAsync\(new Dictionary")
            .Should().HaveCount(1, "duplicating the resource list lets install and update drift apart");
    }
}
