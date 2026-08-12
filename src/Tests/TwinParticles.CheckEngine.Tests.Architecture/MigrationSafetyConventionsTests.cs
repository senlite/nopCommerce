using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using FluentMigrator;
using Nop.Data.Migrations;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class MigrationSafetyConventionsTests
{
    private static readonly Regex CreateTableRegex = new(
        @"Create\.Table\(""(?<table>[^""]+)""\)",
        RegexOptions.Compiled);

    private static readonly Regex ForeignKeyRegex = new(
        @"\.ForeignKey\(""(?<table>[^""]+)""",
        RegexOptions.Compiled);

    private static IReadOnlyList<MigrationDescriptor> DiscoverMigrations()
    {
        var migrationTypes = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.InitialBaselineMigration)
            .Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "TwinParticles.CheckEngine.Infrastructure.Migrations")
            .Where(t => typeof(IMigration).IsAssignableFrom(t))
            .ToList();

        migrationTypes.Should().NotBeEmpty("Check Engine should ship FluentMigrator schema migrations");

        return migrationTypes
            .Select(t =>
            {
                var attribute = Attribute.GetCustomAttribute(t, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
                attribute.Should().NotBeNull($"{t.Name} must declare [NopMigration]");
                return new MigrationDescriptor(t, attribute!);
            })
            .OrderBy(x => x.Attribute.Version)
            .ToList();
    }

    private static string MigrationsDirectory
    {
        get
        {
            var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            for (var dir = start; dir is not null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine.Infrastructure", "Migrations");
                if (Directory.Exists(candidate))
                    return candidate;
            }

            throw new DirectoryNotFoundException("Unable to locate Check Engine Migrations directory from test output path.");
        }
    }

    [Test]
    public void All_Migrations_Should_Be_AutoReversing_For_Rollback_Safety()
    {
        foreach (var migration in DiscoverMigrations())
            migration.Type.IsSubclassOf(typeof(AutoReversingMigration))
                .Should().BeTrue($"{migration.Type.Name} must inherit AutoReversingMigration for rollback safety (FR-925)");
    }

    [Test]
    public void All_Migrations_Should_Target_Installation_Process()
    {
        foreach (var migration in DiscoverMigrations())
            migration.Attribute.TargetMigrationProcess
                .Should().Be(MigrationProcessType.Installation, $"{migration.Type.Name} must target Installation");
    }

    [Test]
    public void All_Migration_Versions_Should_Be_Unique_And_Ordered()
    {
        var migrations = DiscoverMigrations();
        var versions = migrations.Select(x => x.Attribute.Version).ToList();

        versions.Should().OnlyHaveUniqueItems("duplicate FluentMigrator versions block forward/rollback ordering");
        versions.Should().BeInAscendingOrder();
        versions.Count.Should().BeGreaterThanOrEqualTo(23);
    }

    [Test]
    public void ForeignKeys_Should_Reference_Tables_Created_In_Same_Or_Earlier_Migration()
    {
        var createdAtVersion = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var sourceFiles = Directory.GetFiles(MigrationsDirectory, "*.cs");

        var descriptors = DiscoverMigrations()
            .Select(m =>
            {
                var file = sourceFiles.FirstOrDefault(f =>
                    File.ReadAllText(f).Contains($"class {m.Type.Name}", StringComparison.Ordinal));
                file.Should().NotBeNull($"source file for {m.Type.Name} should exist under Migrations/");
                return (Migration: m, Source: File.ReadAllText(file!));
            })
            .OrderBy(x => x.Migration.Attribute.Version)
            .ToList();

        foreach (var item in descriptors)
        {
            foreach (Match match in CreateTableRegex.Matches(item.Source))
            {
                var table = match.Groups["table"].Value;
                if (!createdAtVersion.ContainsKey(table))
                    createdAtVersion[table] = item.Migration.Attribute.Version;
            }

            foreach (Match match in ForeignKeyRegex.Matches(item.Source))
            {
                var parent = match.Groups["table"].Value;
                createdAtVersion.Should().ContainKey(parent,
                    $"{item.Migration.Type.Name} references FK parent '{parent}' before that table is created");

                createdAtVersion[parent].Should().BeLessThanOrEqualTo(item.Migration.Attribute.Version,
                    $"{item.Migration.Type.Name} FK to '{parent}' must not precede the parent table migration");
            }
        }

        createdAtVersion.Keys.Where(t => t.StartsWith("TP_", StringComparison.OrdinalIgnoreCase))
            .Should().NotBeEmpty();
    }

    [Test]
    public void Plugin_Tables_Should_Use_TP_CE_Or_TP_CheckEngine_Prefix()
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.GetFiles(MigrationsDirectory, "*.cs"))
        {
            var source = File.ReadAllText(file);
            foreach (Match match in CreateTableRegex.Matches(source))
                tables.Add(match.Groups["table"].Value);
        }

        tables.Should().NotBeEmpty();
        foreach (var table in tables)
        {
            (table.StartsWith("TP_CE_", StringComparison.Ordinal) ||
             table.StartsWith("TP_CheckEngine_", StringComparison.Ordinal))
                .Should().BeTrue($"table '{table}' must use TP_CE_ / TP_CheckEngine_ naming");
        }
    }

    [Test]
    public void Uninstall_Drop_Order_Should_List_Child_Tables_Before_Parents()
    {
        // Mirrors CheckEngine/docs/10-database-design.md uninstall drop order guidance.
        var dropOrder = new[]
        {
            "TP_CE_AuditEvent",
            "TP_CE_FitmentReviewQueueEvent",
            "TP_CE_FitmentQualifier",
            "TP_CE_FitmentClaim",
            "TP_CE_ImportRow",
            "TP_CE_ImportBatch",
            "TP_CE_ProductOemMap",
            "TP_CE_OemRelation",
            "TP_CE_OemNumber",
            "TP_CE_Manufacturer",
            "TP_CE_GarageOem",
            "TP_CE_GarageVehicle",
            "TP_CE_Garage",
            "TP_CE_VehicleAlias",
            "TP_CE_VehicleConfiguration",
            "TP_CE_VehicleMarket",
            "TP_CE_VehicleEngine",
            "TP_CE_VehicleBody",
            "TP_CE_VehicleGeneration",
            "TP_CE_VehicleModel",
            "TP_CE_VehicleMake"
        };

        var index = dropOrder
            .Select((table, i) => (table, i))
            .ToDictionary(x => x.table, x => x.i, StringComparer.OrdinalIgnoreCase);

        void AssertBefore(string child, string parent)
        {
            index[child].Should().BeLessThan(index[parent],
                $"uninstall must drop '{child}' before '{parent}'");
        }

        AssertBefore("TP_CE_FitmentReviewQueueEvent", "TP_CE_FitmentClaim");
        AssertBefore("TP_CE_FitmentQualifier", "TP_CE_FitmentClaim");
        AssertBefore("TP_CE_FitmentClaim", "TP_CE_VehicleConfiguration");
        AssertBefore("TP_CE_ImportRow", "TP_CE_ImportBatch");
        AssertBefore("TP_CE_ProductOemMap", "TP_CE_OemNumber");
        AssertBefore("TP_CE_GarageVehicle", "TP_CE_Garage");
        AssertBefore("TP_CE_GarageOem", "TP_CE_Garage");
        AssertBefore("TP_CE_VehicleConfiguration", "TP_CE_VehicleMake");
        AssertBefore("TP_CE_VehicleModel", "TP_CE_VehicleMake");
    }

    private sealed record MigrationDescriptor(Type Type, NopMigrationAttribute Attribute);
}
