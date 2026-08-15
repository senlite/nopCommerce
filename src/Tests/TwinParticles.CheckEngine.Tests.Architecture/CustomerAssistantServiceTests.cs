using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Infrastructure.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class CustomerAssistantServiceTests
{
    [Test]
    public async Task AskAsync_Should_Return_Grounded_Answer_From_Catalog()
    {
        var service = new CustomerAssistantService(new GroundedPort(), new StubSearchRepository());

        var response = await service.AskAsync("Do you have a water pump?", null, CancellationToken.None);

        response.Grounded.Should().BeTrue();
        response.Answer.Should().Contain("ProductId=42");
        response.Citations.Should().NotBeEmpty();
    }

    [Test]
    public async Task AskAsync_Should_Redact_Full_Vin_Before_Calling_Provider()
    {
        var port = new RecordingPort();
        var service = new CustomerAssistantService(port, new StubSearchRepository());

        await service.AskAsync("Does WBA3A5C53DF350429 need a water pump?", null, CancellationToken.None);

        port.LastPrompt.Should().NotContain("WBA3A5C53DF350429");
        port.LastPrompt.Should().Contain("VIN …0429");
    }

    [Test]
    public async Task AskAsync_Should_Use_Prompt_Store_When_Resolver_Present()
    {
        var port = new RecordingPort();
        var resolver = new AiPromptResolver(new EmbeddedAiPromptStore());
        var service = new CustomerAssistantService(port, new StubSearchRepository(), resolver);

        await service.AskAsync("Do you have a water pump?", null, CancellationToken.None);

        port.LastPrompt.Should().Contain("Context:");
        port.LastPrompt.Should().Contain("ProductId=42");
        port.LastPrompt.Should().Contain("Do not invent OEM");
    }

    [Test]
    public async Task AskAsync_Should_Return_Disabled_When_Port_Missing()
    {
        var service = new CustomerAssistantService();

        var response = await service.AskAsync("hello", null, CancellationToken.None);

        response.ErrorCode.Should().Be("ai.disabled");
    }

    private sealed class RecordingPort : IAiCompletionPort
    {
        public string LastPrompt { get; private set; } = string.Empty;

        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
        {
            LastPrompt = request.Prompt;
            return Task.FromResult(new AiCompletionResult
            {
                Success = true,
                Text = "I do not invent part numbers.",
                ProviderName = "test",
                PromptHash = "abc"
            });
        }
    }

    private sealed class GroundedPort : IAiCompletionPort
    {
        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
        {
            return Task.FromResult(new AiCompletionResult
            {
                Success = true,
                Text = "Yes, ProductId=42 is a water pump.",
                ProviderName = "test",
                PromptHash = "abc"
            });
        }
    }

    private sealed class StubSearchRepository : IProductSearchReadRepository
    {
        public Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SearchHit>>([
                new SearchHit { ProductId = 42, Name = "Water Pump", Score = 1 }
            ]);
        }

        public Task<IReadOnlyList<SearchHit>> SearchByVehicleTreeAsync(SearchQuery query, CancellationToken cancellationToken) =>
            SearchKeywordAsync(query, cancellationToken);

        public Task<IReadOnlyList<SearchHit>> SearchByOemIdAsync(int oemNumberId, SearchQuery query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SearchHit>>([]);

        public Task<IReadOnlyList<SearchHit>> SearchByCategoryAsync(SearchQuery query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SearchHit>>([]);
    }
}
