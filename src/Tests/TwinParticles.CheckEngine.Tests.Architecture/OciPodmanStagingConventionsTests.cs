using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class OciPodmanStagingConventionsTests
{
    [Test]
    public void Repository_Should_Ship_Isolated_Podman_Pack()
    {
        File.Exists(Locate("podman", "compose.yml")).Should().BeTrue();
        File.Exists(Locate("podman", "env.example")).Should().BeTrue();
        File.Exists(Locate("podman", "bootstrap.sh")).Should().BeTrue();
        File.Exists(Locate("podman", "deploy.sh")).Should().BeTrue();
        File.Exists(Locate("podman", "smoke.sh")).Should().BeTrue();
        File.Exists(Locate("podman", "Caddyfile.example")).Should().BeTrue();
        File.Exists(Locate("podman", "install-caddy-checkengine.sh")).Should().BeTrue();
        File.Exists(Locate("podman", "STAGING.md")).Should().BeTrue();
    }

    [Test]
    public void Compose_Should_Use_CheckEngine_Names_And_Not_Share_Other_Stacks()
    {
        var compose = File.ReadAllText(Locate("podman", "compose.yml"));
        compose.Should().Contain($"container_name: {CheckEngineOciStagingPolicy.WebContainer}");
        compose.Should().Contain($"container_name: {CheckEngineOciStagingPolicy.DbContainer}");
        compose.Should().Contain($"name: {CheckEngineOciStagingPolicy.NetworkName}");
        compose.Should().Contain(CheckEngineOciStagingPolicy.PostgresImage);
        compose.Should().NotContain("abuzahra_");
        compose.Should().NotContain("twinparticles_");
        compose.Should().NotContain("senlite-marley");
        compose.Should().NotContain("mssql");
        compose.Should().NotContain("nopcommerce_mssql");
    }

    [Test]
    public void Compose_Should_Bind_Loopback_Only_On_Staging_Port()
    {
        var compose = File.ReadAllText(Locate("podman", "compose.yml"));
        compose.Should().Contain($"127.0.0.1:${{WEB_PORT:-{CheckEngineOciStagingPolicy.LoopbackPort}}}:80");
        compose.Should().NotContain("\"80:80\"");
        compose.Should().NotContain("'80:80'");
        compose.Should().NotMatchRegex(@"(?m)^\s+-\s+80:80");
        compose.Should().NotContain("0.0.0.0:80");
    }

    [Test]
    public void Compose_Should_Build_Production_Dockerfile_Not_E2E_Sdk_Run()
    {
        var compose = File.ReadAllText(Locate("podman", "compose.yml"));
        compose.Should().Contain("dockerfile: Dockerfile");
        compose.Should().NotContain("e2e/Dockerfile.web");
        var dockerfile = File.ReadAllText(Locate("Dockerfile"));
        dockerfile.Should().Contain("dotnet publish");
        dockerfile.Should().Contain("Nop.Web.dll");
        dockerfile.Should().NotContain("dotnet run");
    }

    [Test]
    public void Env_And_Caddy_Should_Target_Public_CheckEngine_Host()
    {
        var env = File.ReadAllText(Locate("podman", "env.example"));
        var caddy = File.ReadAllText(Locate("podman", "Caddyfile.example"));
        env.Should().Contain($"PUBLIC_HOST={CheckEngineOciStagingPolicy.PublicHost}");
        env.Should().Contain($"WEB_PORT={CheckEngineOciStagingPolicy.LoopbackPort}");
        env.Should().Contain($"REPO_ROOT={CheckEngineOciStagingPolicy.RepoRootOnOci}");
        caddy.Should().Contain(CheckEngineOciStagingPolicy.PublicHost);
        caddy.Should().Contain($"127.0.0.1:{CheckEngineOciStagingPolicy.LoopbackPort}");
        caddy.Should().NotContain(":8000");
        caddy.Should().NotContain(":8001");
        caddy.Should().NotContain(":8080");
    }

    [Test]
    public void Bootstrap_And_Deploy_Should_Stay_Isolated_And_Require_Env()
    {
        var bootstrap = File.ReadAllText(Locate("podman", "bootstrap.sh"));
        var deploy = File.ReadAllText(Locate("podman", "deploy.sh"));
        var smoke = File.ReadAllText(Locate("podman", "smoke.sh"));
        var caddyInstall = File.ReadAllText(Locate("podman", "install-caddy-checkengine.sh"));
        var staging = File.ReadAllText(Locate("podman", "STAGING.md"));

        bootstrap.Should().Contain(".env");
        bootstrap.Should().Contain("compose.yml");
        bootstrap.Should().Contain("Install sample data");
        bootstrap.Should().Contain("Check Engine");
        deploy.Should().Contain("up -d");
        deploy.Should().Contain("checkengine_web");
        smoke.Should().Contain($"127.0.0.1:${{WEB_PORT:-{CheckEngineOciStagingPolicy.LoopbackPort}}}");
        caddyInstall.Should().Contain("/etc/caddy/Caddyfile");
        caddyInstall.Should().Contain(CheckEngineOciStagingPolicy.PublicHost);
        staging.Should().Contain(CheckEngineOciStagingPolicy.VmIp);
        staging.Should().Contain(CheckEngineOciStagingPolicy.RepoRootOnOci);
        staging.Should().Contain("sample data");
    }

    private static string Locate(params string[] relativePath)
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             dir is not null;
             dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, .. relativePath]);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }
}
