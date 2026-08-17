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
public class ImportAiEnrichmentHookServiceTests
{
    [Test]
    public void Apply_Should_Save_Description_And_Specification_Candidates()
    {
        var port = new RecordingAiCompletionPort();
        var store = new InMemoryGenerationRepository();
        var service = new AiContentCandidateService(store);
        var hook = new ImportAiEnrichmentHookService(port, contentCandidateService: service);

        var rows = new List<ImportPipelineRowState>
        {
            new()
            {
                RowNumber = 4,
                Fields = new Dictionary<string, string?>
                {
                    ["name"] = "BMW Oil Filter",
                    ["oem"] = "11-51-7-586-925"
                }
            }
        };

        hook.Apply(rows, enabled: true);

        rows[0].Fields.Should().ContainKey("aiDescriptionCandidate");
        rows[0].Fields.Should().ContainKey("aiSpecificationCandidate");
        port.Calls.Should().HaveCount(2);
        store.Items.Should().Contain(x => x.EntityType == AiGenerationEntityType.ProductDescription);
        store.Items.Should().Contain(x => x.EntityType == AiGenerationEntityType.Specification);
    }

    private sealed class RecordingAiCompletionPort : IAiCompletionPort
    {
        public List<(string FeatureKey, string PromptKey)> Calls { get; } = [];

        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
        {
            Calls.Add((request.FeatureKey, request.PromptKey));

            var text = request.PromptKey == AiFeatureKeys.ImportSpecification
                ? """[{"key":"Material","value":"Steel"}]"""
                : "Concise description";

            return Task.FromResult(new AiCompletionResult
            {
                Success = true,
                Text = text,
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

        public Task<IReadOnlyList<AiGenerationCandidate>> GetPendingQueueAsync(int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AiGenerationCandidate>>(Items);

        public Task MarkReviewedAsync(int id, bool approved, string reviewer, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RebindImportRowEntityAsync(int importRowNumber, int productId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
