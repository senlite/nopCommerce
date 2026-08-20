using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class TenantIsolationConventionsTests
{
    [Test]
    public void Horizon5_Control_Plane_Should_Ship_Schema_And_Public_Api_Routes()
    {
        ReadInfrastructureFile("Migrations", "202608201800_TenantControlPlaneSchema.cs")
            .Should().Contain("TP_CE_Tenant");
        ReadInfrastructureFile("Migrations", "202608201800_TenantControlPlaneSchema.cs")
            .Should().Contain("TP_CE_TenantApiKey");
        ReadInfrastructureFile("Migrations", "202608201800_TenantControlPlaneSchema.cs")
            .Should().Contain("TP_CE_TenantUsageDaily");
        ReadInfrastructureFile("Migrations", "202608201800_TenantControlPlaneSchema.cs")
            .Should().Contain("TP_CE_TenantWebhook");
        ReadInfrastructureFile("Tenancy", "TenantAwareFitmentCache.cs")
            .Should().Contain("TenantCacheKey.Qualify");

        var routes = ReadPluginFile("Infrastructure", "RouteProvider.cs");
        routes.Should().Contain("api/v1/vehicles");
        routes.Should().Contain("api/v1/vin/decode");
        routes.Should().Contain("api/v1/fitment/evaluate");
        routes.Should().Contain("api/v1/search");
        routes.Should().Contain("api/v1/webhooks");
        routes.Should().Contain("Admin/CheckEngine/TenantAdmin/{action}");

        ReadPluginFile("Infrastructure", "CheckEngineStartup.cs")
            .Should().Contain("TenantResolutionMiddleware");
        ReadPluginFile("Controllers", "PublicApiController.cs")
            .Should().Contain("PublicApiUnauthenticated");
        ReadPluginFile("Controllers", "TenantAdminController.cs")
            .Should().Contain("IssueApiKey");
        ReadApplicationFile("Tenancy", "TenantIsolationService.cs")
            .Should().Contain("tenant.isolation.write_denied");
        ReadApplicationFile("Tenancy", "PublicApiService.cs")
            .Should().Contain("apiVersion = \"v1\"");
    }

    [Test]
    public void Domain_Tenancy_Types_Should_Not_Carry_TenantId_On_Horizon1_Entities()
    {
        ReadDomainFile("Fitment", "FitmentClaim.cs").Should().NotContain("TenantId");
        ReadDomainFile("Vehicle", "VehicleMake.cs").Should().NotContain("TenantId");
        ReadDomainFile("Tenancy", "Tenant.cs").Should().Contain("ConnectionName");
    }

    private static string ReadPluginFile(params string[] segments) => Read("Plugins", "TwinParticles.CheckEngine", segments);
    private static string ReadApplicationFile(params string[] segments) => Read("Plugins", "TwinParticles.CheckEngine.Application", segments);
    private static string ReadInfrastructureFile(params string[] segments) => Read("Plugins", "TwinParticles.CheckEngine.Infrastructure", segments);
    private static string ReadDomainFile(params string[] segments) => Read("Plugins", "TwinParticles.CheckEngine.Domain", segments);

    private static string Read(string folder, string project, params string[] segments)
    {
        var path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            folder, project,
            Path.Combine(segments)));
        File.Exists(path).Should().BeTrue(path);
        return File.ReadAllText(path);
    }
}
