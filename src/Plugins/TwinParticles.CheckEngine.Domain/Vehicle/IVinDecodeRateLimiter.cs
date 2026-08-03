namespace TwinParticles.CheckEngine.Domain.Vehicle;

public interface IVinDecodeRateLimiter
{
    bool TryAcquire(string key, out int retryAfterSeconds);
}
