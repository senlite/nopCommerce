using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Controllers;
using TwinParticles.CheckEngine.Application.Erp;
using TwinParticles.CheckEngine.Infrastructure.Erp;

namespace TwinParticles.CheckEngine.Controllers;

/// <summary>
/// Public ERPNext webhook receiver (H1.31 inbound scaffold). Field mapping into nopCommerce entities
/// remains contract-bound; this endpoint only validates signatures and enqueues durable pull jobs.
/// </summary>
public sealed class ErpWebhookController : BasePublicController
{
    private readonly ErpInboundWebhookService _service;

    public ErpWebhookController(ErpInboundWebhookService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            rawBody = await reader.ReadToEndAsync(cancellationToken);

        Request.Headers.TryGetValue(HmacErpInboundWebhookValidator.SignatureHeaderName, out var signatureValues);
        var signature = signatureValues.Count > 0 ? signatureValues[0] : null;

        var result = await _service.ReceiveAsync(rawBody, signature, cancellationToken);
        if (!result.Accepted)
            return BadRequest(new { reasonCode = result.ReasonCode });

        return Json(new { accepted = true, jobId = result.JobId, eventId = result.EventId });
    }
}
