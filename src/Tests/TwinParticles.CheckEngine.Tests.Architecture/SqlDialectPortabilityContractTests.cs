using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SqlDialectPortabilityContractTests
{
    private static readonly Regex Forbidden = new(
        @"\b(SELECT\s+TOP|ISNULL\s*\(|SCOPE_IDENTITY\s*\(|SYSUTCDATETIME\s*\(|SET\s+XACT_ABORT|@@ROWCOUNT|HASHBYTES\s*\(|IF\s+NOT\s+EXISTS|MERGE\s+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Test]
    public void Infrastructure_Sql_Should_Not_Embed_Sql_Server_Only_Dialect()
    {
        var directory = FindInfrastructureDirectory();
        var offenders = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith("CheckEngineSql.cs", StringComparison.OrdinalIgnoreCase))
            .Select(path => (path, source: StripLineComments(File.ReadAllText(path))))
            .Where(file => Forbidden.IsMatch(file.source))
            .Select(file => Path.GetRelativePath(directory, file.path))
            .ToList();

        offenders.Should().BeEmpty(
            "Check Engine raw SQL must run on MySQL as well as SQL Server; dialect helpers belong in CheckEngineSql.cs");
    }

    private static string StripLineComments(string source)
        => Regex.Replace(source, @"//.*?$", string.Empty, RegexOptions.Multiline);

    private static string FindInfrastructureDirectory()
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine.Infrastructure");
            if (Directory.Exists(candidate))
                return candidate;
        }

        throw new DirectoryNotFoundException("Unable to locate Check Engine Infrastructure directory.");
    }
}
