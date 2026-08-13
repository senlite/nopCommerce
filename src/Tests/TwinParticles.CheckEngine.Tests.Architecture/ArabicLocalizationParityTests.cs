using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ArabicLocalizationParityTests
{
    [Test]
    public void Arabic_Dictionary_Should_Have_Exact_Key_Parity_With_English()
    {
        var source = ReadPluginFile("CheckEnginePlugin.cs");
        var englishStart = source.IndexOf("var englishResources", System.StringComparison.Ordinal);
        var englishEnd = source.IndexOf("// English is the safe default", englishStart, System.StringComparison.Ordinal);
        var arabicStart = source.IndexOf("private static Dictionary<string, string> ArabicResources()", System.StringComparison.Ordinal);

        englishStart.Should().BeGreaterThanOrEqualTo(0);
        englishEnd.Should().BeGreaterThan(englishStart);
        arabicStart.Should().BeGreaterThan(englishEnd);

        var english = ExtractKeys(source[englishStart..englishEnd]);
        var arabic = ExtractKeys(source[arabicStart..]);

        english.Should().NotBeEmpty();
        arabic.Should().BeEquivalentTo(english,
            "every English Check Engine resource must have an Arabic translation and vice versa");
        source[arabicStart..].Should().MatchRegex("[\\u0600-\\u06FF]",
            "the Arabic dictionary must contain Arabic script, not copied English placeholders");
    }

    [Test]
    public void Arabic_Resources_Should_Be_Applied_To_Every_Arabic_Culture()
    {
        var source = ReadPluginFile("CheckEnginePlugin.cs");

        source.Should().Contain("GetAllLanguagesAsync(showHidden: true)");
        source.Should().Contain("LanguageCulture.StartsWith(\"ar\"");
        source.Should().Contain("AddOrUpdateLocaleResourceAsync(arabicResources, language.Id)");
    }

    private static string[] ExtractKeys(string source)
        => Regex.Matches(source, "\\[\\\"(Plugins\\.TwinParticles\\.CheckEngine\\.[^\\\"]+)\\\"\\]")
            .Select(match => match.Groups[1].Value)
            .Distinct(System.StringComparer.Ordinal)
            .OrderBy(key => key, System.StringComparer.Ordinal)
            .ToArray();

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
}
