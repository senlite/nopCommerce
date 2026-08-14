using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using FluentMigrator;
using Nop.Data.Migrations;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// Guards NFR-aligned hot-path indexes from doc 10 / NFR-001–008 against migration drift.
/// </summary>
[TestFixture]
public class HotPathIndexCatalogTests
{
    /// <summary>
    /// Logical hot-path catalog: index name → NFR / AC rationale.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> RequiredHotPathIndexes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // NFR-006 OEM normalised lookup
            ["IX_TP_CE_OemNumber_NormalizedNumber"] = "NFR-006 manufacturer-agnostic OEM lookup",
            ["IX_TP_CE_OemNumber_ManufacturerId_NormalizedNumber"] = "NFR-006 manufacturer-scoped OEM unique lookup",

            // NFR-001 / NFR-003 / NFR-008 / AC-10.3 fitment
            ["IX_TP_CE_FitmentClaim_ProductId_VehicleConfigurationId"] = "NFR-003 1×1 fitment claim lookup",
            ["IX_TP_CE_FitmentClaim_VehicleConfigurationId_IsPublished"] = "NFR-001 vehicle search base key",
            ["IX_TP_CE_FitmentClaim_VehicleConfig_IsPublished_Covering"] = "AC-10.3 covering vehicle→products",
            ["IX_TP_CE_FitmentClaim_ProductId_IsPublished"] = "NFR-008 PDP fitment badge base key",
            ["IX_TP_CE_FitmentClaim_ProductId_IsPublished_Covering"] = "NFR-008 PDP covering badge path",
            ["IX_TP_CE_FitmentClaim_ReviewQueue"] = "Admin fitment review queue",

            // Garage context
            ["IX_TP_CE_Garage_CustomerId"] = "Garage get-or-create by customer",
            ["IX_TP_CE_GarageVehicle_GarageId"] = "Garage vehicle child load",
            ["IX_TP_CE_GarageOem_GarageId"] = "Garage OEM child load",

            // NFR-007 hierarchy child queries
            ["IX_TP_CE_VehicleModel_MakeId"] = "NFR-007 make→models",
            ["IX_TP_CE_VehicleGeneration_ModelId"] = "NFR-007 model→generations",
            ["IX_TP_CE_VehicleBody_GenerationId"] = "NFR-007 generation→bodies",
            ["IX_TP_CE_VehicleEngine_BodyId"] = "NFR-007 body→engines",
            ["IX_TP_CE_VehicleConfiguration_GenerationId"] = "NFR-007 generation→configurations",

            // OEM supersession walks
            ["IX_TP_CE_OemRelation_FromOemNumberId_IsActive"] = "OEM supersession forward walk",
            ["IX_TP_CE_OemRelation_ToOemNumberId"] = "OEM supersession reverse walk",
            ["IX_TP_CE_ProductOemMap_OemNumberId"] = "OEM→products map",

            // Import review
            ["IX_TP_CE_ImportRow_BatchId_ReviewStatus"] = "Import review queue"
        };

    private static readonly string[] RequiredIncludeTokens =
    [
        @".Include(""ProductId"")",
        @".Include(""FitmentStatusId"")",
        @".Include(""Confidence"")",
        @".Include(""ManufacturerId"")",
        @".Include(""DisplayNumber"")"
    ];

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

    private static string CombinedMigrationSource()
        => string.Join(Environment.NewLine,
            Directory.GetFiles(MigrationsDirectory, "*.cs").Select(File.ReadAllText));

    [Test]
    public void HotPath_Indexes_From_NFR_Catalog_Should_Exist_In_Migrations()
    {
        var source = CombinedMigrationSource();

        foreach (var (indexName, rationale) in RequiredHotPathIndexes)
        {
            source.Should().Contain($"\"{indexName}\"",
                because: $"hot-path index {indexName} is required for {rationale}");
        }
    }

    [Test]
    public void HotPath_Covering_Indexes_Should_Declare_Include_Columns()
    {
        var source = CombinedMigrationSource();

        foreach (var token in RequiredIncludeTokens)
            source.Should().Contain(token, because: "covering indexes must use FluentMigrator.SqlServer Include columns");
    }

    /// <summary>
    /// MySQL caps identifiers at 64 characters, so a longer name fails plugin installation there
    /// even though SQL Server (128) accepts it.
    /// </summary>
    [Test]
    public void Migration_Identifiers_Should_Fit_Within_MySql_64_Character_Limit()
    {
        const int mySqlIdentifierLimit = 64;
        var source = CombinedMigrationSource();

        var identifiers = System.Text.RegularExpressions.Regex
            .Matches(source, "\"(?<name>(IX|FK|UQ|PK)_[A-Za-z0-9_]+)\"")
            .Select(match => match.Groups["name"].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        identifiers.Should().NotBeEmpty("migrations declare named indexes and constraints");

        var tooLong = identifiers.Where(name => name.Length > mySqlIdentifierLimit).ToList();

        tooLong.Should().BeEmpty(
            because: $"database identifiers must be <= {mySqlIdentifierLimit} chars for MySQL compatibility, but found: {string.Join(", ", tooLong.Select(n => $"{n} ({n.Length})"))}");
    }

    [Test]
    public void HotPath_Index_Migrations_Should_Be_Installation_AutoReversing()
    {
        var types = new[]
        {
            typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.HotPathOemNormalizedNumberIndexMigration),
            typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.HotPathFitmentCoveringIndexesMigration),
            typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.HotPathGarageAndHierarchyIndexesMigration),
            typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.HotPathOemRelationWalkIndexesMigration)
        };

        foreach (var migrationType in types)
        {
            migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue(migrationType.Name);
            var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;
            attribute.Should().NotBeNull(migrationType.Name);
            attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation, migrationType.Name);
        }
    }
}
