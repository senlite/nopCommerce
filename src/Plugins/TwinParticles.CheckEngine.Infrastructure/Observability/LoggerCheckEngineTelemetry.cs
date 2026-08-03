using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TwinParticles.CheckEngine.Domain.Observability;

namespace TwinParticles.CheckEngine.Infrastructure.Observability;

public sealed class LoggerCheckEngineTelemetry : ICheckEngineTelemetry
{
    private readonly ILogger<LoggerCheckEngineTelemetry> _logger;

    public LoggerCheckEngineTelemetry(ILogger<LoggerCheckEngineTelemetry> logger)
    {
        _logger = logger;
    }

    public void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties)
    {
        _logger.LogInformation("CheckEngine telemetry event {EventName}: {@Properties}", eventName, properties);
    }
}
