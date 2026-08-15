using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Search;

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
    public async Task AskAsync_Should_Return_Disabled_When_Port_Missing()
    {
        var service = new CustomerAssistantService();

        var response = await service.AskAsync("hello", null, CancellationToken.None);

        response.ErrorCode.Should().Be("ai.disabled");
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
