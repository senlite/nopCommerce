using System.IO;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Configuration;
using TwinParticles.CheckEngine.Infrastructure.Erp;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ErpInboundWebhookTests
{
    [Test]
    public void Validator_Should_Accept_Valid_Hmac_And_Reject_Tampered_Bodies()
    {
        const string secret = "erp-webhook-secret";
        var body = """{"entityType":2,"localId":"ITEM-001","eventId":"evt-1","payload":"{}"}""";
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body)));

        var validator = new HmacErpInboundWebhookValidator(Options.Create(new CheckEngineSettings
        {
            Erp = new CheckEngineSettings.ErpOptions { InboundWebhookSecret = secret }
        }));

        validator.Validate(body, signature).IsValid.Should().BeTrue();
        validator.Validate(body + " ", signature).IsValid.Should().BeFalse();
        validator.Validate(body, null).ReasonCode.Should().Be("erp.webhook.missing_signature");
    }

    [Test]
    public void Route_Provider_Should_Register_Public_Erp_Webhook()
    {
        var routes = ReadPluginFile("Infrastructure", "RouteProvider.cs");
        routes.Should().Contain("check-engine/erp/webhook");
        routes.Should().Contain("ErpWebhook");
    }

    private static string ReadPluginFile(params string[] relativePath)
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             dir is not null;
             dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }
}
