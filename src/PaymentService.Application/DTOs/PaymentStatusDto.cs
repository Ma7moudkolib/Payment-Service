using PaymentService.Domain.Enums;

namespace PaymentService.Application.DTOs;

public sealed record PaymentStatusDto(
    Guid PaymentId,
    PaymentStatus Status,
    string Gateway,
    string? GatewayPaymentId,
    string? FailureReason,
    DateTimeOffset UpdatedAtUtc);
