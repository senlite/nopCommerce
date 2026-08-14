namespace TwinParticles.CheckEngine.Domain.Erp;

public sealed class ErpInboundWebhookValidationResult
{
    public bool IsValid { get; init; }

    public string? ReasonCode { get; init; }
}

/// <summary>
/// Validates signed ERPNext webhook callbacks before enqueueing pull jobs (H1.31 inbound scaffold).
/// </summary>
public interface IErpInboundWebhookValidator
{
    ErpInboundWebhookValidationResult Validate(string rawBody, string? signatureHeader);
}
