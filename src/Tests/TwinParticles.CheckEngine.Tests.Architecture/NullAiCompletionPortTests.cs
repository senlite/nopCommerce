using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class NullAiCompletionPortTests
{
    [Test]
    public async Task CompleteAsync_Should_Return_Disabled()
    {
        var port = new NullAiCompletionPort();

        var result = await port.CompleteAsync(new AiCompletionRequest
        {
            PromptKey = "test",
            Prompt = "hello"
        }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ai.disabled");
        result.ProviderName.Should().Be("null");
        result.Text.Should().BeEmpty();
    }
}
