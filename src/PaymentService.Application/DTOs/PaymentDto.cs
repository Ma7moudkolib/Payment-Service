using PaymentService.Domain.Enums;

namespace PaymentService.Application.DTOs;

public sealed record PaymentDto(
    Guid Id,
    Guid CustomerId,
    Guid? PaymentMethodId,
    MoneyDto Amount,
    string Gateway,
    PaymentStatus Status,
    string? GatewayPaymentId,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyCollection<PaymentTransactionDto> Transactions);
