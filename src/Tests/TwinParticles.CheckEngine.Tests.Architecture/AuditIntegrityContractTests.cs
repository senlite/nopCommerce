using System.IO;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Infrastructure.Migrations;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AuditIntegrityContractTests
{
    [Test]
    public void Sql_Audit_Should_Chain_Entries_Verify_Integrity_And_Anchor_Retention()
    {
        var source = ReadPluginFile("..", "TwinParticles.CheckEngine.Infrastructure", "Security", "SqlCheckEngineAuditService.cs");

        source.Should().Contain("sp_getapplock");
        source.Should().Contain("HASHBYTES('SHA2_256'");
        source.Should().Contain("PreviousHash");
        source.Should().Contain("EntryHash");
        source.Should().Contain("LAG(EntryHash,1,@anchor)");
        source.Should().Contain("TP_CE_AuditChainAnchor");
        source.Should().Contain("LastPrunedHash");
        source.Should().Contain("DELETE FROM TP_CE_AuditEvent WHERE Id<=@pruneId");
    }

    [Test]
    public void Audit_Integrity_Migration_Should_Have_Explicit_Rollback()
    {
        typeof(AuditIntegrityChainMigration).BaseType.Should().Be(typeof(FluentMigrator.Migration));
        typeof(AuditIntegrityChainMigration).BaseType.Should().NotBe(typeof(FluentMigrator.AutoReversingMigration));
    }

    [Test]
    public void Plugin_Should_Register_Daily_Audit_Retention_Task()
    {
        var task = ReadPluginFile("Tasks", "AuditRetentionTask.cs");
        var plugin = ReadPluginFile("CheckEnginePlugin.cs");

        task.Should().Contain("DateTime.UtcNow.AddYears(-7)");
        task.Should().Contain("PruneAsync");
        plugin.Should().Contain("typeof(Tasks.AuditRetentionTask).FullName!");
        plugin.Should().Contain("24 * 60 * 60");
    }

    private static string ReadPluginFile(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.GetFullPath(Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", .. relativePath]));
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }
}
