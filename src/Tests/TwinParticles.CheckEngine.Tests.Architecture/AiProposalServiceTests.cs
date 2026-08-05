using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AiProposalServiceTests
{
    [Test]
    public async Task CreateProposalAsync_Should_Never_Auto_Publish()
    {
        var service = new AiProposalService(new SuccessPort());

        var proposal = await service.CreateProposalAsync(
            "fitment.inference",
            "fitment.prompt",
            "Infer fitment",
            CancellationToken.None);

        proposal.IsPublished.Should().BeFalse();
        proposal.Content.Should().Be("candidate text");
        proposal.ErrorCode.Should().BeNull();
    }

    [Test]
    public async Task CreateProposalAsync_Should_Remain_Unpublished_When_Disabled()
    {
        var service = new AiProposalService(new NullAiCompletionPort(), new AlwaysOffToggle());

        var proposal = await service.CreateProposalAsync(
            "content.description",
            "content.prompt",
            "Write description",
            CancellationToken.None);

        proposal.IsPublished.Should().BeFalse();
        proposal.ErrorCode.Should().Be("ai.disabled");
    }

    private sealed class SuccessPort : IAiCompletionPort
    {
        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
        {
            return Task.FromResult(new AiCompletionResult
            {
                Success = true,
                Text = "candidate text",
                ProviderName = "test",
                PromptHash = "abc",
                TokenUsage = 10
            });
        }
    }

    private sealed class AlwaysOffToggle : IAiFeatureToggle
    {
        public bool IsEnabled(string featureKey) => false;
    }
}
