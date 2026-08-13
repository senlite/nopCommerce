namespace TwinParticles.CheckEngine.Domain.ImportPipeline;

public sealed class ImportRow
{
    public int Id { get; set; }

    public int BatchId { get; set; }

    public int RowNumber { get; set; }

    public string RawPayload { get; set; } = string.Empty;

    public string? NormalizedOem { get; set; }

    public int? MatchedOemNumberId { get; set; }

    public int? ProposedProductId { get; set; }

    public string? ProposedFitmentJson { get; set; }

    public decimal? Confidence { get; set; }

    public string ReviewStatus { get; set; } = "Pending";

    public string? ReviewNote { get; set; }

    public string? PipelineStateJson { get; set; }

    public string? CompletedStagesCsv { get; set; }

    public string? LastStageError { get; set; }
}
