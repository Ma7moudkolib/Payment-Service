using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;
using PaymentService.Domain.ValueObjects;

namespace PaymentService.Domain.Entities;

public sealed class Payment
{
    private readonly List<PaymentTransaction> _transactions = [];

    private Payment()
    {
    }

    public Payment(Guid customerId, Money amount, string gateway, Guid? paymentMethodId = null)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer id is required.", nameof(customerId));
        }

        if (amount.IsZero)
        {
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(gateway))
        {
            throw new ArgumentException("Gateway is required.", nameof(gateway));
        }

        Id = Guid.NewGuid();
        CustomerId = customerId;
        Amount = amount;
        Gateway = gateway.Trim();
        PaymentMethodId = paymentMethodId;
        Status = PaymentStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid? PaymentMethodId { get; private set; }

    public Money Amount { get; private set; } = default!;

    public string Gateway { get; private set; } = string.Empty;

    public PaymentStatus Status { get; private set; }

    public string? GatewayPaymentId { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<PaymentTransaction> Transactions => _transactions.AsReadOnly();

    public void MarkAuthorized(string gatewayPaymentId)
    {
        EnsureTransitionAllowed(PaymentStatus.Authorized);
        GatewayPaymentId = RequireGatewayPaymentId(gatewayPaymentId);
        FailureReason = null;
        TransitionTo(PaymentStatus.Authorized, GatewayPaymentId);
    }

    public void MarkCaptured(string gatewayPaymentId)
    {
        EnsureTransitionAllowed(PaymentStatus.Captured);
        GatewayPaymentId = RequireGatewayPaymentId(gatewayPaymentId);
        FailureReason = null;
        TransitionTo(PaymentStatus.Captured, GatewayPaymentId);
    }

    public void MarkFailed(string failureReason)
    {
        EnsureTransitionAllowed(PaymentStatus.Failed);

        if (string.IsNullOrWhiteSpace(failureReason))
        {
            throw new ArgumentException("Failure reason is required.", nameof(failureReason));
        }

        FailureReason = failureReason.Trim();
        TransitionTo(PaymentStatus.Failed, GatewayPaymentId, FailureReason);
    }

    public void MarkRefunded(string gatewayRefundId)
    {
        EnsureTransitionAllowed(PaymentStatus.Refunded);
        var transactionId = RequireGatewayPaymentId(gatewayRefundId);
        FailureReason = null;
        TransitionTo(PaymentStatus.Refunded, transactionId);
    }

    private void TransitionTo(
        PaymentStatus status,
        string? gatewayTransactionId = null,
        string? failureReason = null)
    {
        Status = status;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        _transactions.Add(new PaymentTransaction(
            Id,
            status,
            Amount,
            Gateway,
            gatewayTransactionId,
            failureReason));
    }

    private void EnsureTransitionAllowed(PaymentStatus nextStatus)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var allowedStatuses) ||
            !allowedStatuses.Contains(nextStatus))
        {
            throw new DomainException(
                $"Payment cannot transition from '{Status}' to '{nextStatus}'.");
        }
    }

    private static string RequireGatewayPaymentId(string gatewayPaymentId)
    {
        if (string.IsNullOrWhiteSpace(gatewayPaymentId))
        {
            throw new ArgumentException("Gateway payment id is required.", nameof(gatewayPaymentId));
        }

        return gatewayPaymentId.Trim();
    }

    private static readonly IReadOnlyDictionary<PaymentStatus, PaymentStatus[]> AllowedTransitions =
        new Dictionary<PaymentStatus, PaymentStatus[]>
        {
            [PaymentStatus.Pending] =
            [
                PaymentStatus.Authorized,
                PaymentStatus.Captured,
                PaymentStatus.Failed
            ],
            [PaymentStatus.Authorized] =
            [
                PaymentStatus.Captured,
                PaymentStatus.Failed
            ],
            [PaymentStatus.Captured] =
            [
                PaymentStatus.Refunded
            ],
            [PaymentStatus.Failed] = [],
            [PaymentStatus.Refunded] = []
        };
}
