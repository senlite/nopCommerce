using System;
using System.Collections.Generic;
using System.Linq;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Ai;

public static class AssistantCitationFormatter
{
    public static AssistantCitation FromHit(SearchHit hit)
    {
        var parts = new List<string> { $"ProductId={hit.ProductId}", $"Name={hit.Name}" };
        if (!string.IsNullOrWhiteSpace(hit.Brand))
            parts.Add($"Brand={hit.Brand}");
        if (hit.Price.HasValue)
            parts.Add($"Price={hit.Price.Value:0.##}");

        return new AssistantCitation
        {
            ProductId = hit.ProductId,
            Name = hit.Name,
            Brand = hit.Brand,
            Price = hit.Price,
            SeName = hit.SeName,
            DisplayText = string.Join("; ", parts)
        };
    }

    public static string BuildContextBlock(IReadOnlyList<AssistantCitation> citations) =>
        string.Join('\n', citations.Select((citation, index) => $"{index + 1}. {citation.DisplayText}"));
}
