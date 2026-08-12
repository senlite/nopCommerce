namespace TwinParticles.CheckEngine.Domain.Erp;

public sealed class ErpConnectionOptions
{
    public static ErpConnectionOptions Current { get; set; } = new();

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;
}
