using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VinDecodePrivacyContractTests
{
    [Test]
    public void Vin_Decode_Service_Should_Log_Redacted_Telemetry_And_Audit()
    {
        var service = ReadApplicationFile("Vehicle", "Vin", "VinDecodeApplicationService.cs");
        var infrastructure = ReadInfrastructureFile("DependencyInjection", "ServiceCollectionExtensions.cs");

        service.Should().Contain("IVinPrivacyService");
        service.Should().Contain("vinLast4");
        service.Should().Contain("vinHash");
        service.Should().Contain("AutoAcceptConfidenceThreshold");
        service.Should().Contain("NeedsDisambiguation");
        service.Should().Contain("ICheckEngineAuditService");
        service.Should().Contain("action: \"vin.decode\"");
        service.Should().NotContain("[\"normalizedVin\"]");
        service.Should().NotContain("[\"rawVin\"]");

        infrastructure.Should().Contain("IVinPrivacyService, VinPrivacyService");
        infrastructure.Should().Contain("VinDecodeOptions.Current");
    }

    [Test]
    public void Unified_Search_Should_Require_Confident_Single_Match_For_Vin_Lane()
    {
        var source = ReadApplicationFile("Search", "UnifiedSearchService.cs");

        source.Should().Contain("decode.Outcome, \"SingleMatch\"");
    }

    private static string ReadApplicationFile(params string[] relativePath)
        => ReadPluginFile("TwinParticles.CheckEngine.Application", relativePath);

    private static string ReadInfrastructureFile(params string[] relativePath)
        => ReadPluginFile("TwinParticles.CheckEngine.Infrastructure", relativePath);

    private static string ReadPluginFile(string project, params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", project, .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {project}/{string.Join('/', relativePath)}");
    }
}
