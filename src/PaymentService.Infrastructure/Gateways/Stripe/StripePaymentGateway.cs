using Microsoft.Extensions.Options;
using System.Net;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Interfaces;
using PaymentService.Domain.ValueObjects;
using PaymentService.Infrastructure.Options;
using Stripe;
using DomainPaymentMethod = PaymentService.Domain.Entities.PaymentMethod;

namespace PaymentService.Infrastructure.Gateways.Stripe;

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly PaymentIntentService _paymentIntentService;
    private readonly RefundService _refundService;

    public StripePaymentGateway(IOptions<StripeOptions> options)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            throw new InvalidOperationException("Stripe API key is not configured.");
        }

        var stripeClient = new StripeClient(options.Value.ApiKey);
        _paymentIntentService = new PaymentIntentService(stripeClient);
        _refundService = new RefundService(stripeClient);
    }

    public string Name => "Stripe";

    public async Task<GatewayPaymentResult> AuthorizeAsync(
        Payment payment,
        DomainPaymentMethod paymentMethod,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentIntent = await _paymentIntentService.CreateAsync(
                new PaymentIntentCreateOptions
                {
                    Amount = ToMinorUnits(payment.Amount),
                    Currency = payment.Amount.Currency.ToLowerInvariant(),
                    PaymentMethod = paymentMethod.GatewayToken,
                    CaptureMethod = "manual",
                    Confirm = true,
                    OffSession = true,
                    Metadata = BuildMetadata(payment)
                },
                cancellationToken: cancellationToken);

            return MapPaymentIntentResult(paymentIntent, successStatuses: ["requires_capture", "succeeded"]);
        }
        catch (StripeException exception) when (IsTransient(exception))
        {
            throw new HttpRequestException("Transient Stripe authorization failure.", exception);
        }
        catch (StripeException exception)
        {
            return GatewayPaymentResult.Failure(MapStripeError(exception));
        }
    }

    public async Task<GatewayPaymentResult> CaptureAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payment.GatewayPaymentId))
        {
            return GatewayPaymentResult.Failure("Stripe payment intent id is missing.");
        }

        try
        {
            var paymentIntent = await _paymentIntentService.CaptureAsync(
                payment.GatewayPaymentId,
                cancellationToken: cancellationToken);

            return MapPaymentIntentResult(paymentIntent, successStatuses: ["succeeded"]);
        }
        catch (StripeException exception) when (IsTransient(exception))
        {
            throw new HttpRequestException("Transient Stripe capture failure.", exception);
        }
        catch (StripeException exception)
        {
            return GatewayPaymentResult.Failure(MapStripeError(exception));
        }
    }

    public async Task<GatewayPaymentResult> RefundAsync(
        Payment payment,
        Money amount,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payment.GatewayPaymentId))
        {
            return GatewayPaymentResult.Failure("Stripe payment intent id is missing.");
        }

        try
        {
            var refund = await _refundService.CreateAsync(
                new RefundCreateOptions
                {
                    PaymentIntent = payment.GatewayPaymentId,
                    Amount = ToMinorUnits(amount),
                    Metadata = new Dictionary<string, string>
                    {
                        ["payment_id"] = payment.Id.ToString(),
                        ["customer_id"] = payment.CustomerId.ToString()
                    }
                },
                cancellationToken: cancellationToken);

            return refund.Status is "succeeded" or "pending"
                ? GatewayPaymentResult.Success(refund.Id)
                : GatewayPaymentResult.Failure($"Stripe refund ended with status '{refund.Status}'.");
        }
        catch (StripeException exception) when (IsTransient(exception))
        {
            throw new HttpRequestException("Transient Stripe refund failure.", exception);
        }
        catch (StripeException exception)
        {
            return GatewayPaymentResult.Failure(MapStripeError(exception));
        }
    }

    private static GatewayPaymentResult MapPaymentIntentResult(
        PaymentIntent paymentIntent,
        IReadOnlyCollection<string> successStatuses)
    {
        if (successStatuses.Contains(paymentIntent.Status, StringComparer.OrdinalIgnoreCase))
        {
            return GatewayPaymentResult.Success(paymentIntent.Id);
        }

        var failureReason = paymentIntent.LastPaymentError?.Message
            ?? $"Stripe payment intent ended with status '{paymentIntent.Status}'.";

        return GatewayPaymentResult.Failure(failureReason);
    }

    private static Dictionary<string, string> BuildMetadata(Payment payment) =>
        new()
        {
            ["payment_id"] = payment.Id.ToString(),
            ["customer_id"] = payment.CustomerId.ToString()
        };

    private static long ToMinorUnits(Money money) =>
        decimal.ToInt64(decimal.Round(money.Amount * 100m, 0, MidpointRounding.AwayFromZero));

    private static string MapStripeError(StripeException exception) =>
        exception.StripeError?.Message
        ?? exception.Message
        ?? "Stripe payment request failed.";

    private static bool IsTransient(StripeException exception) =>
        exception.HttpStatusCode is HttpStatusCode.TooManyRequests or
            HttpStatusCode.InternalServerError or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout;
}
