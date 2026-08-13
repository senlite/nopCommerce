using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Search;

public sealed class AutocompleteResult
{
    public IReadOnlyList<AutocompleteSuggestion> Vehicles { get; init; } = [];

    public IReadOnlyList<AutocompleteSuggestion> Oems { get; init; } = [];

    public IReadOnlyList<AutocompleteSuggestion> Products { get; init; } = [];
}
