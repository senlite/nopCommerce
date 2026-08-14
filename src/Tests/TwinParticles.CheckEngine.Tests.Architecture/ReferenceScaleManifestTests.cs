using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.ReferenceScale;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ReferenceScaleManifestTests
{
    private sealed class ManifestDocument
    {
        public string Version { get; set; } = string.Empty;
        public ManifestTargets Targets { get; set; } = new();
        public ManifestTagging Tagging { get; set; } = new();
    }

    private sealed class ManifestTargets
    {
        public int Products { get; set; }
        public int FitmentClaims { get; set; }
        public int Configurations { get; set; }
        public int OemEntries { get; set; }
        public int ClaimsPerProduct { get; set; }
    }

    private sealed class ManifestTagging
    {
        public string ProvenancePrefix { get; set; } = string.Empty;
        public string ProductSkuPrefix { get; set; } = string.Empty;
        public string OemNormalizedPrefix { get; set; } = string.Empty;
        public string ConfigurationFingerprintPrefix { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    [Test]
    public void Manifest_Should_Match_Domain_Constants()
    {
        var manifest = LoadManifest();

        manifest.Version.Should().Be(ReferenceScaleManifest.Version);
        manifest.Targets.Products.Should().Be(ReferenceScaleManifest.TargetProducts);
        manifest.Targets.FitmentClaims.Should().Be(ReferenceScaleManifest.TargetFitmentClaims);
        manifest.Targets.Configurations.Should().Be(ReferenceScaleManifest.TargetConfigurations);
        manifest.Targets.OemEntries.Should().Be(ReferenceScaleManifest.TargetOemEntries);
        manifest.Targets.ClaimsPerProduct.Should().Be(ReferenceScaleManifest.ClaimsPerProduct);
        manifest.Tagging.ProvenancePrefix.Should().Be(ReferenceScaleManifest.ProvenancePrefix);
        manifest.Tagging.ProductSkuPrefix.Should().Be(ReferenceScaleManifest.ProductSkuPrefix);
        manifest.Tagging.OemNormalizedPrefix.Should().Be(ReferenceScaleManifest.OemNormalizedPrefix);
        manifest.Tagging.ConfigurationFingerprintPrefix.Should().Be(ReferenceScaleManifest.ConfigurationFingerprintPrefix);
        manifest.Tagging.CreatedBy.Should().Be(ReferenceScaleManifest.CreatedBy);
    }

    [Test]
    public void ResolveTargets_Should_Scale_Counts_Proportionally()
    {
        var targets = ReferenceScaleManifest.ResolveTargets(0.001);

        targets.Products.Should().Be(250);
        targets.FitmentClaims.Should().Be(2000);
        targets.Configurations.Should().Be(40);
        targets.OemEntries.Should().Be(500);
    }

    [Test]
    public void Generator_Should_Produce_Deterministic_Tagged_Identifiers()
    {
        ReferenceScaleDataGenerator.ProductSku(1).Should().Be("REF-SCALE-00000001");
        ReferenceScaleDataGenerator.OemNormalizedNumber(42).Should().Be("REFSCALE00000042");
        ReferenceScaleDataGenerator.ConfigurationFingerprint(7).Should().Be("ref-scale:v1:cfg:00000007");
        ReferenceScaleDataGenerator.FitmentSourceReference(12, 3).Should().Be("ref-scale:v1:claim:00000012:3");
        ReferenceScaleDataGenerator.ResolveConfigurationIndex(10, 2, 40_000).Should().Be(82);
    }

    [Test]
    public void Infrastructure_Should_Expose_ReferenceScale_Loader()
    {
        typeof(TwinParticles.CheckEngine.Infrastructure.ReferenceScale.ReferenceScaleCatalogLoader).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Infrastructure.ReferenceScale.ReferenceScaleCatalogLoader)
            .Should().BeAssignableTo<IReferenceScaleCatalogLoader>();
    }

    private static ManifestDocument LoadManifest()
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Tests", "corpus", "reference-scale", "manifest.json");
            if (File.Exists(candidate))
            {
                return JsonSerializer.Deserialize<ManifestDocument>(
                    File.ReadAllText(candidate),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            }
        }

        throw new FileNotFoundException("Unable to locate reference-scale/manifest.json");
    }
}
