using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Application.Erp;

public sealed class ErpInboundWebhookService
{
    private readonly ErpSyncService _syncService;
    private readonly IErpInboundWebhookValidator _validator;

    public ErpInboundWebhookService(ErpSyncService syncService, IErpInboundWebhookValidator validator)
    {
        _syncService = syncService;
        _validator = validator;
    }

    public async Task<ErpInboundWebhookResult> ReceiveAsync(
        string rawBody,
        string? signatureHeader,
        CancellationToken cancellationToken)
    {
        var validation = _validator.Validate(rawBody, signatureHeader);
        if (!validation.IsValid)
        {
            return new ErpInboundWebhookResult
            {
                Accepted = false,
                ReasonCode = validation.ReasonCode ?? "erp.webhook.invalid"
            };
        }

        ErpInboundWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ErpInboundWebhookPayload>(
                rawBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return new ErpInboundWebhookResult
            {
                Accepted = false,
                ReasonCode = "erp.webhook.invalid_payload"
            };
        }

        if (payload is null ||
            string.IsNullOrWhiteSpace(payload.LocalId) ||
            string.IsNullOrWhiteSpace(payload.EventId))
        {
            return new ErpInboundWebhookResult
            {
                Accepted = false,
                ReasonCode = "erp.webhook.invalid_payload"
            };
        }

        var entityType = payload.EntityType;
        if (!Enum.IsDefined(typeof(ErpSyncEntityType), entityType))
        {
            return new ErpInboundWebhookResult
            {
                Accepted = false,
                ReasonCode = "erp.webhook.unsupported_entity"
            };
        }

        var jobId = await _syncService.QueueSyncAsync(
            entityType,
            ErpSyncDirection.PullFromErp,
            payload.LocalId.Trim(),
            payload.Payload ?? rawBody,
            cancellationToken);

        return new ErpInboundWebhookResult
        {
            Accepted = true,
            JobId = jobId,
            EventId = payload.EventId.Trim()
        };
    }
}

public sealed class ErpInboundWebhookPayload
{
    public ErpSyncEntityType EntityType { get; set; }

    public string LocalId { get; set; } = string.Empty;

    public string EventId { get; set; } = string.Empty;

    public string? Payload { get; set; }
}

public sealed class ErpInboundWebhookResult
{
    public bool Accepted { get; init; }

    public Guid? JobId { get; init; }

    public string? EventId { get; init; }

    public string? ReasonCode { get; init; }
}
