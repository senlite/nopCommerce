using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Application.Common.Observability;

public interface ICheckEngineTelemetry
{
    void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties);
}
