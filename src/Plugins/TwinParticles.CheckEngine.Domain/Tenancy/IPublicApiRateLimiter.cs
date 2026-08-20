namespace TwinParticles.CheckEngine.Domain.Tenancy;

public interface IPublicApiRateLimiter
{
    bool TryAcquire(string key, out int retryAfterSeconds);
}
