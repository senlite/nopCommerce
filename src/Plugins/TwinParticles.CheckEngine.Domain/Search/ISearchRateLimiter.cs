namespace TwinParticles.CheckEngine.Domain.Search;

public interface ISearchRateLimiter
{
    bool TryAcquire(string key, out int retryAfterSeconds);
}
