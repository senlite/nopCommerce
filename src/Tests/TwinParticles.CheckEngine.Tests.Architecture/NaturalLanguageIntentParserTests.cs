using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class NaturalLanguageIntentParserTests
{
    [Test]
    public async Task ParseAsync_Should_Fallback_To_Heuristic_When_Ai_Disabled()
    {
        var parser = new NaturalLanguageIntentParser();

        var intent = await parser.ParseAsync("2016 BMW 320i water pump", "en", CancellationToken.None);

        intent.ModelYear.Should().Be(2016);
        intent.PartTerms.Should().Contain("water");
        intent.PartTerms.Should().Contain("pump");
    }

    [Test]
    public async Task ParseAsync_Should_Parse_Structured_Json_From_Llm()
    {
        var parser = new NaturalLanguageIntentParser(new JsonPort());

        var intent = await parser.ParseAsync("water pump for my 2016 320i", "en", CancellationToken.None);

        intent.ParsedFromNaturalLanguage.Should().BeTrue();
        intent.Make.Should().Be("BMW");
        intent.Model.Should().Be("320i");
        intent.ModelYear.Should().Be(2016);
        intent.PartTerms.Should().Contain("water pump");
    }

    private sealed class JsonPort : IAiCompletionPort
    {
        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
        {
            return Task.FromResult(new AiCompletionResult
            {
                Success = true,
                Text = """
                    {
                      "partTerms": ["water pump"],
                      "make": "BMW",
                      "model": "320i",
                      "modelYear": 2016,
                      "oemNumber": null,
                      "keywordFallback": "water pump BMW 320i"
                    }
                    """,
                ProviderName = "test",
                PromptHash = "abc"
            });
        }
    }
}
