namespace PaymentService.Application.Events;

public sealed record PaymentSucceededEvent(
    Guid PaymentId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string Gateway,
    string GatewayPaymentId,
    DateTimeOffset SucceededAtUtc);
