using PaymentService.Application.DTOs;
using PaymentService.Domain.Entities;
using PaymentService.Domain.ValueObjects;

namespace PaymentService.Application.Mappings;

public static class PaymentMappings
{
    public static Money ToMoney(this MoneyDto dto) => new(dto.Amount, dto.Currency);

    public static Money ToMoney(this ProcessPaymentRequest request) =>
        new(request.Amount, request.Currency);

    public static Money ToMoney(this RefundPaymentRequest request) =>
        new(request.Amount, request.Currency);

    public static PaymentDto ToDto(this Payment payment) =>
        new(
            payment.Id,
            payment.CustomerId,
            payment.PaymentMethodId,
            payment.Amount.ToDto(),
            payment.Gateway,
            payment.Status,
            payment.GatewayPaymentId,
            payment.FailureReason,
            payment.CreatedAtUtc,
            payment.UpdatedAtUtc,
            payment.Transactions
                .OrderBy(transaction => transaction.OccurredAtUtc)
                .Select(transaction => transaction.ToDto())
                .ToArray());

    private static MoneyDto ToDto(this Money money) => new(money.Amount, money.Currency);

    private static PaymentTransactionDto ToDto(this PaymentTransaction transaction) =>
        new(
            transaction.Id,
            transaction.Status,
            transaction.Amount.ToDto(),
            transaction.Gateway,
            transaction.GatewayTransactionId,
            transaction.FailureReason,
            transaction.OccurredAtUtc);
}
