using System.IO;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Infrastructure.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class NopAiContentApplicatorConventionsTests
{
    [Test]
    public void Infrastructure_Should_Register_Content_Applicator_And_Prompt_Store()
    {
        var source = ReadInfrastructureFile("DependencyInjection", "ServiceCollectionExtensions.cs");

        source.Should().Contain("AddScoped<IAiContentApplicator, NopAiContentApplicator>");
        source.Should().Contain("AddSingleton<IAiPromptStore, EmbeddedAiPromptStore>");
        source.Should().Contain("AddSingleton<MemoryAiCompletionCache>");
        source.Should().Contain("LayeredAiCompletionCache");
        source.Should().Contain("AzureOpenAiEmbeddingPort");
        source.Should().Contain("CachingAiCompletionPort");
    }

    [Test]
    public void NopAiContentApplicator_Should_Implement_Domain_Port()
    {
        typeof(NopAiContentApplicator).Should().Implement<IAiContentApplicator>();
    }

    private static string ReadInfrastructureFile(params string[] relativePath)
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             dir is not null;
             dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine.Infrastructure", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }
}
