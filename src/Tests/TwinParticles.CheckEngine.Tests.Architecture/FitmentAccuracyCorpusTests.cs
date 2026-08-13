using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// Runs the curated fitment accuracy corpus (src/Tests/corpus/fitment).
/// The Must set must pass at 100% on a release candidate (AC-35.3); a single wrong
/// verdict here is a release blocker rather than a warning.
/// </summary>
[TestFixture]
public class FitmentAccuracyCorpusTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static CorpusDocument LoadCorpus()
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Tests", "corpus", "fitment", "fitment-corpus.json");
            if (File.Exists(candidate))
                return JsonSerializer.Deserialize<CorpusDocument>(File.ReadAllText(candidate), JsonOptions)
                       ?? throw new InvalidOperationException("fitment corpus could not be parsed");
        }

        throw new FileNotFoundException("Unable to locate src/Tests/corpus/fitment/fitment-corpus.json");
    }

    private static IEnumerable<TestCaseData> CorpusCases()
    {
        foreach (var corpusCase in LoadCorpus().Cases)
            yield return new TestCaseData(corpusCase).SetName($"Corpus_{corpusCase.Id}_{corpusCase.Name}");
    }

    [Test]
    public void Corpus_Should_Meet_Horizon_1_Coverage_Requirements()
    {
        var corpus = LoadCorpus();
        var safetyCritical = corpus.Cases
            .Count(c => c.Claims.Any(claim => claim.SafetyClass == nameof(SafetyClass.SafetyCritical)));
        var standard = corpus.Cases
            .Count(c => c.Claims.Any(claim => claim.SafetyClass == nameof(SafetyClass.Standard)));

        using var scope = new AssertionScope();
        corpus.Cases.Should().HaveCountGreaterThanOrEqualTo(200,
            "Horizon 1 requires a corpus of at least 200 cases");
        safetyCritical.Should().BeGreaterThan(0, "the corpus must span safety-critical parts");
        standard.Should().BeGreaterThan(0, "the corpus must span standard parts");
        corpus.Cases.Select(c => c.Id).Should().OnlyHaveUniqueItems();
        corpus.Cases.Should().OnlyContain(c => c.Tier == "Must",
            "every case is currently release-blocking; add an explicit tier before relaxing this");
    }

    [Test]
    public void Corpus_Should_Cover_Every_Evaluation_Outcome()
    {
        var outcomes = LoadCorpus().Cases.Select(c => c.Expected.Outcome).Distinct().ToList();

        outcomes.Should().Contain([
            nameof(FitmentStatus.Fits),
            nameof(FitmentStatus.DoesNotFit),
            nameof(FitmentStatus.Unknown),
            nameof(FitmentStatus.NeedsDisambiguation)
        ], "FR-302 defines four evaluation outcomes and each must be exercised");
    }

    [TestCaseSource(nameof(CorpusCases))]
    public async Task Corpus_Case_Should_Produce_Expected_Verdict(CorpusCase corpusCase)
    {
        var claims = corpusCase.Claims.Select((claim, index) => claim.ToClaim(index + 1)).ToArray();
        var service = new FitmentEvaluationService(new CorpusReadRepository(claims), new NoCache());

        var result = await service.EvaluateAsync(corpusCase.Context.ToEvaluationContext(), CancellationToken.None);

        using var scope = new AssertionScope();
        result.Outcome.ToString().Should().Be(corpusCase.Expected.Outcome, corpusCase.Description);

        if (corpusCase.Expected.ReasonCode is not null)
            result.ReasonCode.Should().Be(corpusCase.Expected.ReasonCode, corpusCase.Description);
    }

    public sealed class CorpusDocument
    {
        public string Version { get; init; } = string.Empty;
        public decimal FitsConfidenceThreshold { get; init; }
        public List<CorpusCase> Cases { get; init; } = [];
    }

    public sealed class CorpusCase
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Tier { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public List<string> Tags { get; init; } = [];
        public List<CorpusClaim> Claims { get; init; } = [];
        public CorpusContext Context { get; init; } = new();
        public CorpusExpectation Expected { get; init; } = new();

        public override string ToString() => $"{Id} {Name}";
    }

    public sealed class CorpusClaim
    {
        public string Status { get; init; } = nameof(FitmentStatus.Fits);
        public decimal Confidence { get; init; }
        public bool IsPublished { get; init; }
        public bool IsActive { get; init; }
        public string SafetyClass { get; init; } = nameof(Domain.Fitment.SafetyClass.Standard);
        public string SourceKind { get; init; } = nameof(FitmentSourceKind.CuratorManual);
        public CorpusQualifier Qualifier { get; init; } = new();

        public FitmentClaim ToClaim(int id) => new()
        {
            Id = id,
            ProductId = 1,
            VehicleConfigurationId = 1,
            Status = Enum.Parse<FitmentStatus>(Status),
            Confidence = Confidence,
            IsPublished = IsPublished,
            IsActive = IsActive,
            SafetyClass = Enum.Parse<SafetyClass>(SafetyClass),
            Provenance = new FitmentClaimProvenance
            {
                SourceKind = Enum.Parse<FitmentSourceKind>(SourceKind),
                SourceReference = "corpus",
                CreatedBy = "corpus",
                CreatedUtc = DateTimeOffset.UnixEpoch
            },
            Qualifier = new FitmentClaimQualifier
            {
                ProductionFromYear = Qualifier.ProductionFromYear,
                ProductionToYear = Qualifier.ProductionToYear,
                SteeringSide = Qualifier.SteeringSide,
                MarketRegion = Qualifier.MarketRegion,
                DriveType = Qualifier.DriveType,
                TransmissionType = Qualifier.TransmissionType
            }
        };
    }

    public sealed class CorpusQualifier
    {
        public int? ProductionFromYear { get; init; }
        public int? ProductionToYear { get; init; }
        public string? SteeringSide { get; init; }
        public string? MarketRegion { get; init; }
        public string? DriveType { get; init; }
        public string? TransmissionType { get; init; }
    }

    public sealed class CorpusContext
    {
        public int? ProductionYear { get; init; }
        public string? SteeringSide { get; init; }
        public string? MarketRegion { get; init; }
        public string? DriveType { get; init; }
        public string? TransmissionType { get; init; }

        public FitmentEvaluationContext ToEvaluationContext() => new()
        {
            ProductId = 1,
            VehicleConfigurationId = 1,
            ProductionYear = ProductionYear,
            SteeringSide = SteeringSide,
            MarketRegion = MarketRegion,
            DriveType = DriveType,
            TransmissionType = TransmissionType
        };
    }

    public sealed class CorpusExpectation
    {
        public string Outcome { get; init; } = string.Empty;
        public string? ReasonCode { get; init; }
    }

    private sealed class CorpusReadRepository : IFitmentClaimReadRepository
    {
        private readonly IReadOnlyList<FitmentClaim> _claims;

        public CorpusReadRepository(IReadOnlyList<FitmentClaim> claims) => _claims = claims;

        public Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.FromResult(_claims);

        public Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FitmentClaim>>([]);
    }

    private sealed class NoCache : IFitmentCache
    {
        public Task<FitmentEvaluationResult?> GetAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.FromResult<FitmentEvaluationResult?>(null);

        public Task SetAsync(int productId, int vehicleConfigurationId, FitmentEvaluationResult result, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task InvalidateAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
