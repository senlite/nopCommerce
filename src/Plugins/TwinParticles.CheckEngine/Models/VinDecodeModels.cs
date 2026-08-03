namespace TwinParticles.CheckEngine.Models;

public sealed class VinDecodeRequestModel
{
    public string Vin { get; set; } = string.Empty;
}

public sealed class VinDecodeRateLimitedModel
{
    public string ReasonCode { get; set; } = "vin.rate_limited";

    public int RetryAfterSeconds { get; set; }
}
