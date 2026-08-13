namespace TwinParticles.CheckEngine.Domain.Search;

public sealed class SearchHit
{
    public int ProductId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string? Brand { get; set; }

    public decimal? Price { get; set; }

    public decimal Score { get; set; }

    public bool FitsActiveContext { get; set; }
}
