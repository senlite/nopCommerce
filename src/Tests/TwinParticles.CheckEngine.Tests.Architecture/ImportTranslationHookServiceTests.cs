using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Application.ImportPipeline.Stages;
using TwinParticles.CheckEngine.Application.L10n;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ImportTranslationHookServiceTests
{
    [Test]
    public void Apply_Should_Save_Translation_Candidate_With_Glossary_Quality_Score()
    {
        var port = new RecordingAiCompletionPort();
        var store = new InMemoryGenerationRepository();
        var service = new AiContentCandidateService(store);
        var glossary = new AutomotiveGlossaryService(new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["water pump"] = "مضخة مياه"
        });
        var hook = new ImportTranslationHookService(glossary, port, contentCandidateService: service);

        var rows = new List<ImportPipelineRowState>
        {
            new()
            {
                RowNumber = 2,
                Fields = new Dictionary<string, string?> { ["name"] = "water pump" }
            }
        };

        hook.Apply(rows, enabled: true);

        rows[0].Fields.Should().ContainKey("translationCandidate");
        rows[0].Fields["translationGlossaryValid"].Should().Be("true");
        store.Items.Should().ContainSingle(x => x.EntityType == AiGenerationEntityType.Translation);
        store.Items[0].QualityScore.Should().Be(1m);
        store.Items[0].IsPublished.Should().BeFalse();
    }

    [Test]
    public void Apply_Should_Flag_Glossary_Miss_As_Pending_Review()
    {
        var port = new RecordingAiCompletionPort("wrong arabic");
        var store = new InMemoryGenerationRepository();
        var service = new AiContentCandidateService(store);
        var hook = new ImportTranslationHookService(new AutomotiveGlossaryService(), port, contentCandidateService: service);

        var rows = new List<ImportPipelineRowState>
        {
            new()
            {
                RowNumber = 3,
                Fields = new Dictionary<string, string?> { ["name"] = "water pump" }
            }
        };

        hook.Apply(rows, enabled: true);

        rows[0].Fields["translated"].Should().Be("pending_review");
        rows[0].Fields["translationGlossaryValid"].Should().Be("false");
        store.Items[0].QualityScore.Should().Be(0.5m);
    }

    private sealed class RecordingAiCompletionPort : IAiCompletionPort
    {
        private readonly string _text;

        public RecordingAiCompletionPort(string text = "مضخة مياه")
        {
            _text = text;
        }

        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AiCompletionResult
            {
                Success = true,
                Text = _text,
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
