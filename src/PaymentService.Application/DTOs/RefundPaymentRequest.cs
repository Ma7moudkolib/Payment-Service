namespace PaymentService.Application.DTOs;

public sealed record RefundPaymentRequest(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string Gateway,
    string RequestKey);
