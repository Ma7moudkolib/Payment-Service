using PaymentService.Domain.Enums;

namespace PaymentService.Application.DTOs;

public sealed record PaymentTransactionDto(
    Guid Id,
    PaymentStatus Status,
    MoneyDto Amount,
    string Gateway,
    string? GatewayTransactionId,
    string? FailureReason,
    DateTimeOffset OccurredAtUtc);
