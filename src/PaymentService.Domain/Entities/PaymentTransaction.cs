using PaymentService.Domain.Enums;
using PaymentService.Domain.ValueObjects;

namespace PaymentService.Domain.Entities;

public sealed class PaymentTransaction
{
    private PaymentTransaction()
    {
    }

    public PaymentTransaction(
        Guid paymentId,
        PaymentStatus status,
        Money amount,
        string gateway,
        string? gatewayTransactionId,
        string? failureReason = null)
    {
        if (paymentId == Guid.Empty)
        {
            throw new ArgumentException("Payment id is required.", nameof(paymentId));
        }

        if (string.IsNullOrWhiteSpace(gateway))
        {
            throw new ArgumentException("Gateway is required.", nameof(gateway));
        }

        Id = Guid.NewGuid();
        PaymentId = paymentId;
        Status = status;
        Amount = amount;
        Gateway = gateway.Trim();
        GatewayTransactionId = string.IsNullOrWhiteSpace(gatewayTransactionId)
            ? null
            : gatewayTransactionId.Trim();
        FailureReason = string.IsNullOrWhiteSpace(failureReason)
            ? null
            : failureReason.Trim();
        OccurredAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid PaymentId { get; private set; }

    public PaymentStatus Status { get; private set; }

    public Money Amount { get; private set; } = default!;

    public string Gateway { get; private set; } = string.Empty;

    public string? GatewayTransactionId { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }
}
