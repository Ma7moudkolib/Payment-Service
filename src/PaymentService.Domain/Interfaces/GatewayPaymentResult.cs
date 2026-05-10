namespace PaymentService.Domain.Interfaces;

public sealed record GatewayPaymentResult(
    bool IsSuccessful,
    string? GatewayPaymentId,
    string? FailureReason)
{
    public static GatewayPaymentResult Success(string gatewayPaymentId) =>
        new(true, gatewayPaymentId, null);

    public static GatewayPaymentResult Failure(string failureReason) =>
        new(false, null, failureReason);
}
