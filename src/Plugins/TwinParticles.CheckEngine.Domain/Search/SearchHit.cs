namespace TwinParticles.CheckEngine.Domain.Search;

public sealed class SearchHit
{
    public int ProductId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int? CategoryId { get; init; }

    public string? Brand { get; init; }

    public decimal Score { get; set; }

    public bool FitsActiveContext { get; set; }
}
