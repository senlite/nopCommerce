using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Infrastructure.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AiPromptResolverTests
{
    [Test]
    public void Format_Should_Substitute_Placeholders_From_Embedded_Store()
    {
        var resolver = new AiPromptResolver(new EmbeddedAiPromptStore());

        var prompt = resolver.Format(AiFeatureKeys.ImportEnrichment, new Dictionary<string, string?>
        {
            ["name"] = "Water pump",
            ["oem"] = "11517586925"
        });

        prompt.Should().Contain("Water pump");
        prompt.Should().Contain("11517586925");
        prompt.Should().NotContain("{name}");
    }

    [Test]
    public void ResolveDefinition_Should_Return_Versioned_Prompt()
    {
        var resolver = new AiPromptResolver(new EmbeddedAiPromptStore());

        var definition = resolver.ResolveDefinition(AiFeatureKeys.SearchNaturalLanguage);

        definition.Should().NotBeNull();
        definition!.Version.Should().Be("1");
        definition.Body.Should().Contain("{query}");
    }
}
