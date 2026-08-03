using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Observability;

public interface ICheckEngineTelemetry
{
    void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties);
}
