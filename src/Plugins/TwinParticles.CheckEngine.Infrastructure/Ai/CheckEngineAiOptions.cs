using System;
using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class CheckEngineAiOptions
{
    public static CheckEngineAiOptions Current { get; set; } = new();

    public IReadOnlyCollection<string> EnabledFeatures { get; set; } = Array.Empty<string>();

    public int DailyTokenCeiling { get; set; } = 100_000;

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-4o-mini";
}
