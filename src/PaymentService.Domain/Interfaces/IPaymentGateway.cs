using PaymentService.Domain.Entities;
using PaymentService.Domain.ValueObjects;

namespace PaymentService.Domain.Interfaces;

public interface IPaymentGateway
{
    string Name { get; }

    Task<GatewayPaymentResult> AuthorizeAsync(
        Payment payment,
        PaymentMethod paymentMethod,
        CancellationToken cancellationToken = default);

    Task<GatewayPaymentResult> CaptureAsync(
        Payment payment,
        CancellationToken cancellationToken = default);

    Task<GatewayPaymentResult> RefundAsync(
        Payment payment,
        Money amount,
        CancellationToken cancellationToken = default);
}
