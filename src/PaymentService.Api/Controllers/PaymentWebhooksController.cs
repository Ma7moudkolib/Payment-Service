using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PaymentService.Api.Options;
using PaymentService.Api.Requests;
using PaymentService.Api.Security;
using PaymentService.Application.Abstractions;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("webhooks/payments")]
public sealed class PaymentWebhooksController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPaymentService _paymentService;
    private readonly IOptions<WebhookSignatureOptions> _signatureOptions;

    public PaymentWebhooksController(
        IPaymentService paymentService,
        IOptions<WebhookSignatureOptions> signatureOptions)
    {
        _paymentService = paymentService;
        _signatureOptions = signatureOptions;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> Stripe(CancellationToken cancellationToken) =>
        await HandleWebhookAsync(
            signatureHeaderName: "Stripe-Signature",
            secret: _signatureOptions.Value.StripeSecret,
            cancellationToken);

    [HttpPost("paypal")]
    public async Task<IActionResult> PayPal(CancellationToken cancellationToken) =>
        await HandleWebhookAsync(
            signatureHeaderName: "PayPal-Signature",
            secret: _signatureOptions.Value.PayPalSecret,
            cancellationToken);

    private async Task<IActionResult> HandleWebhookAsync(
        string signatureHeaderName,
        string secret,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        var signature = Request.Headers[signatureHeaderName].FirstOrDefault();
        if (signature is null || !WebhookSignatureVerifier.IsValid(payload, signature, secret))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid webhook signature.",
                type: "https://httpstatuses.com/401");
        }

        var webhook = JsonSerializer.Deserialize<PaymentWebhookRequest>(payload, JsonOptions);
        if (webhook is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid webhook payload.",
                type: "https://httpstatuses.com/400");
        }

        await _paymentService.UpdatePaymentStatusAsync(
            webhook.PaymentId,
            webhook.Status,
            webhook.GatewayTransactionId,
            webhook.FailureReason,
            cancellationToken);

        return Accepted();
    }
}
