namespace TwinParticles.CheckEngine.Application.Fitment;

/// <summary>
/// Encodes AI fitment inference metadata in <see cref="Domain.Fitment.FitmentClaimProvenance.SourceReference"/>.
/// </summary>
public static class FitmentAiReference
{
    private const char Separator = '|';

    public static string Format(string promptHash, string? rationale)
    {
        var hash = promptHash?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rationale))
            return hash;

        return $"{hash}{Separator}{rationale.Trim()}";
    }

    public static (string PromptHash, string? Rationale) Parse(string? sourceReference)
    {
        if (string.IsNullOrWhiteSpace(sourceReference))
            return (string.Empty, null);

        var separatorIndex = sourceReference.IndexOf(Separator);
        if (separatorIndex < 0)
            return (sourceReference, null);

        var hash = sourceReference[..separatorIndex];
        var rationale = sourceReference[(separatorIndex + 1)..].Trim();
        return (hash, string.IsNullOrWhiteSpace(rationale) ? null : rationale);
    }
}
