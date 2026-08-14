namespace TwinParticles.CheckEngine.Domain.Vehicle;

public sealed class VinPattern
{
    public int Id { get; set; }

    public int MakeId { get; set; }

    public string Pattern { get; set; } = string.Empty;

    public int Priority { get; set; }

    public string ModelCode { get; set; } = string.Empty;

    public string GenerationCode { get; set; } = string.Empty;

    public string? EngineCode { get; set; }

    public string? TrimSlug { get; set; }

    public decimal Confidence { get; set; }

    public string Provenance { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
