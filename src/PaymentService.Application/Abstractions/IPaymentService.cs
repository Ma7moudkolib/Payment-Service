using PaymentService.Application.DTOs;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Abstractions;

public interface IPaymentService
{
    Task<PaymentStatusDto> GetPaymentStatusAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<PaymentDto> ProcessPaymentAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentDto> RefundPaymentAsync(
        RefundPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentDto> UpdatePaymentStatusAsync(
        Guid paymentId,
        PaymentStatus status,
        string gatewayTransactionId,
        string? failureReason = null,
        CancellationToken cancellationToken = default);
}
