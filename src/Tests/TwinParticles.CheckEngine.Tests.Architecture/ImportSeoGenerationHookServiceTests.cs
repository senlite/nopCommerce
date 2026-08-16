using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Application.ImportPipeline.Stages;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ImportSeoGenerationHookServiceTests
{
    [Test]
    public void Apply_Should_Save_Seo_Candidate_Without_Auto_Publish()
    {
        var port = new RecordingAiCompletionPort();
        var store = new InMemoryGenerationRepository();
        var service = new AiContentCandidateService(store);
        var hook = new ImportSeoGenerationHookService(port, contentCandidateService: service);

        var rows = new List<ImportPipelineRowState>
        {
            new()
            {
                RowNumber = 5,
                Fields = new Dictionary<string, string?> { ["name"] = "BMW Oil Filter" }
            }
        };

        hook.Apply(rows, enabled: true);

        rows[0].Fields.Should().ContainKey("seoCandidate");
        rows[0].Fields["seoGenerated"].Should().Be("true");
        store.Items.Should().ContainSingle(x => x.EntityType == AiGenerationEntityType.SeoMetadata);
        store.Items[0].ReviewStatus.Should().Be("pending");
        store.Items[0].IsPublished.Should().BeFalse();
    }

    [Test]
    public void Apply_Should_Skip_When_Disabled_Or_Feature_Off()
    {
        var store = new InMemoryGenerationRepository();
        var service = new AiContentCandidateService(store);
        var hook = new ImportSeoGenerationHookService(
            new RecordingAiCompletionPort(),
            featureToggle: new OffToggle(),
            contentCandidateService: service);

        var rows = new List<ImportPipelineRowState>
        {
            new() { RowNumber = 1, Fields = new Dictionary<string, string?> { ["name"] = "Filter" } }
        };

        hook.Apply(rows, enabled: false);
        store.Items.Should().BeEmpty();

        hook.Apply(rows, enabled: true);
        store.Items.Should().BeEmpty();
    }

    [Test]
    public void Apply_Should_Skip_Row_When_Completion_Fails()
    {
        var store = new InMemoryGenerationRepository();
        var hook = new ImportSeoGenerationHookService(
            new FailingPort(),
            contentCandidateService: new AiContentCandidateService(store));

        var rows = new List<ImportPipelineRowState>
        {
            new() { RowNumber = 1, Fields = new Dictionary<string, string?> { ["name"] = "Filter" } }
        };

        hook.Apply(rows, enabled: true);

        store.Items.Should().BeEmpty();
        rows[0].Fields.Should().NotContainKey("seoCandidate");
    }

    private sealed class OffToggle : IAiFeatureToggle
    {
        public bool IsEnabled(string featureKey) => false;
    }

    private sealed class FailingPort : IAiCompletionPort
    {
        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new AiCompletionResult { Success = false, ErrorCode = "ai.completion_failed" });
    }

    private sealed class RecordingAiCompletionPort : IAiCompletionPort
    {
        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AiCompletionResult
            {
                Success = true,
                Text = """{"metaTitle":"BMW Oil Filter","metaDescription":"OEM oil filter for BMW engines."}""",
                PromptHash = "hash"
            });
        }
    }

    private sealed class InMemoryGenerationRepository : IAiGenerationRepository
    {
        public List<AiGenerationCandidate> Items { get; } = [];

        public Task<int> InsertAsync(AiGenerationCandidate candidate, CancellationToken cancellationToken)
        {
            candidate.Id = Items.Count + 1;
            Items.Add(candidate);
            return Task.FromResult(candidate.Id);
        }

        public Task<AiGenerationCandidate?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult<AiGenerationCandidate?>(null);

        public Task<IReadOnlyList<AiGenerationCandidate>> GetPendingByEntityAsync(
            AiGenerationEntityType entityType,
            int entityId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AiGenerationCandidate>>([]);

        public Task MarkReviewedAsync(int id, bool approved, string reviewer, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RebindImportRowEntityAsync(int importRowNumber, int productId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
