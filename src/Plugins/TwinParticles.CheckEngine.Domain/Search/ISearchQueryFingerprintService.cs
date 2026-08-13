namespace TwinParticles.CheckEngine.Domain.Search;

public interface ISearchQueryFingerprintService
{
    string Create(string normalizedQuery);
}
