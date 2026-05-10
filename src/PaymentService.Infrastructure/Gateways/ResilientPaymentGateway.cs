using Microsoft.Extensions.Logging;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Interfaces;
using PaymentService.Domain.ValueObjects;
using Polly;

namespace PaymentService.Infrastructure.Gateways;

public sealed class ResilientPaymentGateway : IPaymentGateway
{
    private readonly IPaymentGateway _inner;
    private readonly ILogger<ResilientPaymentGateway> _logger;
    private readonly IAsyncPolicy<GatewayPaymentResult> _policy;

    public ResilientPaymentGateway(
        IPaymentGateway inner,
        ILogger<ResilientPaymentGateway> logger)
    {
        _inner = inner;
        _logger = logger;
        _policy = Policy<GatewayPaymentResult>
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)),
                onRetry: (outcome, delay, attempt, _) =>
                {
                    _logger.LogWarning(
                        outcome.Exception,
                        "Retrying payment gateway {PaymentGateway} call. Attempt {Attempt}; next retry in {Delay}.",
                        Name,
                        attempt,
                        delay);
                });
    }

    public string Name => _inner.Name;

    public Task<GatewayPaymentResult> AuthorizeAsync(
        Payment payment,
        PaymentMethod paymentMethod,
        CancellationToken cancellationToken = default) =>
        _policy.ExecuteAsync(
            token => _inner.AuthorizeAsync(payment, paymentMethod, token),
            cancellationToken);

    public Task<GatewayPaymentResult> CaptureAsync(
        Payment payment,
        CancellationToken cancellationToken = default) =>
        _policy.ExecuteAsync(
            token => _inner.CaptureAsync(payment, token),
            cancellationToken);

    public Task<GatewayPaymentResult> RefundAsync(
        Payment payment,
        Money amount,
        CancellationToken cancellationToken = default) =>
        _policy.ExecuteAsync(
            token => _inner.RefundAsync(payment, amount, token),
            cancellationToken);
}
