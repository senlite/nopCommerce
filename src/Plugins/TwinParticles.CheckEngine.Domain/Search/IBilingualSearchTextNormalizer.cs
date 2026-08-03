namespace TwinParticles.CheckEngine.Domain.Search;

public interface IBilingualSearchTextNormalizer
{
    string Normalize(string text, string locale);
}
