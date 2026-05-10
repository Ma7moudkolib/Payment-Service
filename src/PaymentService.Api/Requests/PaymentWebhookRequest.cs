using PaymentService.Domain.Enums;

namespace PaymentService.Api.Requests;

public sealed record PaymentWebhookRequest(
    Guid PaymentId,
    PaymentStatus Status,
    string GatewayTransactionId,
    string? FailureReason);
