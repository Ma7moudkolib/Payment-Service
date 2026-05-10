namespace PaymentService.Application.DTOs;

public sealed record ProcessPaymentRequest(
    Guid CustomerId,
    Guid PaymentMethodId,
    decimal Amount,
    string Currency,
    string Gateway,
    string RequestKey);
