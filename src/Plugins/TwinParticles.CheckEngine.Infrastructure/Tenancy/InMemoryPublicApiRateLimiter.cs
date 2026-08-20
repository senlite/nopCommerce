using System;
using System.Collections.Concurrent;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Infrastructure.Tenancy;

public sealed class InMemoryPublicApiRateLimiter : IPublicApiRateLimiter
{
    private const int MaxRequestsPerWindow = 60;
    private static readonly TimeSpan WindowSize = TimeSpan.FromMinutes(1);

    private readonly ICheckEngineClock _clock;
    private readonly ConcurrentDictionary<string, WindowState> _states = new(StringComparer.Ordinal);

    public InMemoryPublicApiRateLimiter(ICheckEngineClock clock)
    {
        _clock = clock;
    }

    public bool TryAcquire(string key, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;
        var now = _clock.UtcNow;
        var state = _states.GetOrAdd(key, _ => new WindowState(now));
        lock (state.SyncRoot)
        {
            if (now - state.WindowStartUtc >= WindowSize)
            {
                state.WindowStartUtc = now;
                state.Count = 0;
            }

            if (state.Count >= MaxRequestsPerWindow)
            {
                retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((state.WindowStartUtc + WindowSize - now).TotalSeconds));
                return false;
            }

            state.Count++;
            return true;
        }
    }

    private sealed class WindowState
    {
        public WindowState(DateTimeOffset windowStartUtc) => WindowStartUtc = windowStartUtc;

        public object SyncRoot { get; } = new();

        public DateTimeOffset WindowStartUtc { get; set; }

        public int Count { get; set; }
    }
}
