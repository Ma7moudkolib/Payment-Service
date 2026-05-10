using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Caching.Distributed;
using PaymentService.Application.Abstractions;
using PaymentService.Application.DTOs;
using PaymentService.Application.Exceptions;
using PaymentService.Application.Events;
using PaymentService.Application.Mappings;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.ValueObjects;

namespace PaymentService.Application.Services;

public sealed class PaymentService : IPaymentService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly DistributedCacheEntryOptions CompletedCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
    };
    private static readonly DistributedCacheEntryOptions ProcessingCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IPaymentGatewayResolver _gatewayResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventOutbox _integrationEventOutbox;
    private readonly IDistributedCache _cache;
    private readonly IValidator<ProcessPaymentRequest> _processPaymentValidator;
    private readonly IValidator<RefundPaymentRequest> _refundPaymentValidator;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPaymentMethodRepository paymentMethodRepository,
        IPaymentGatewayResolver gatewayResolver,
        IUnitOfWork unitOfWork,
        IIntegrationEventOutbox integrationEventOutbox,
        IDistributedCache cache,
        IValidator<ProcessPaymentRequest> processPaymentValidator,
        IValidator<RefundPaymentRequest> refundPaymentValidator)
    {
        _paymentRepository = paymentRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _gatewayResolver = gatewayResolver;
        _unitOfWork = unitOfWork;
        _integrationEventOutbox = integrationEventOutbox;
        _cache = cache;
        _processPaymentValidator = processPaymentValidator;
        _refundPaymentValidator = refundPaymentValidator;
    }

    public async Task<PaymentDto> ProcessPaymentAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_processPaymentValidator, request, cancellationToken);

        return await ExecuteIdempotentAsync(
            operationName: "process-payment",
            request.RequestKey,
            async () =>
            {
                var paymentMethod = await _paymentMethodRepository
                    .GetByIdAsync(request.PaymentMethodId, cancellationToken)
                    ?? throw new PaymentMethodNotFoundException(request.PaymentMethodId);

                var payment = new Payment(
                    request.CustomerId,
                    request.ToMoney(),
                    request.Gateway,
                    request.PaymentMethodId);

                var gateway = _gatewayResolver.Resolve(request.Gateway);
                var result = await gateway.AuthorizeAsync(payment, paymentMethod, cancellationToken);

                if (result.IsSuccessful)
                {
                    payment.MarkAuthorized(result.GatewayPaymentId!);
                }
                else
                {
                    payment.MarkFailed(result.FailureReason ?? "Payment authorization failed.");
                }

                await _paymentRepository.AddAsync(payment, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return payment.ToDto();
            },
            cancellationToken);
    }

    public async Task<PaymentStatusDto> GetPaymentStatusAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(paymentId);

        return new PaymentStatusDto(
            payment.Id,
            payment.Status,
            payment.Gateway,
            payment.GatewayPaymentId,
            payment.FailureReason,
            payment.UpdatedAtUtc);
    }

    public async Task<PaymentDto> RefundPaymentAsync(
        RefundPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_refundPaymentValidator, request, cancellationToken);

        return await ExecuteIdempotentAsync(
            operationName: "refund-payment",
            request.RequestKey,
            async () =>
            {
                var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken)
                    ?? throw new PaymentNotFoundException(request.PaymentId);

                var refundAmount = request.ToMoney();
                EnsureRefundCanBeProcessed(payment, refundAmount, request.Gateway);

                var gateway = _gatewayResolver.Resolve(request.Gateway);
                var result = await gateway.RefundAsync(payment, refundAmount, cancellationToken);

                if (result.IsSuccessful)
                {
                    payment.MarkRefunded(result.GatewayPaymentId!);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return payment.ToDto();
                }

                throw new PaymentProcessingException(
                    result.FailureReason ?? "Payment refund failed.");
            },
            cancellationToken);
    }

    public async Task<PaymentDto> UpdatePaymentStatusAsync(
        Guid paymentId,
        PaymentStatus status,
        string gatewayTransactionId,
        string? failureReason = null,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(paymentId);

        switch (status)
        {
            case PaymentStatus.Authorized:
                payment.MarkAuthorized(gatewayTransactionId);
                break;
            case PaymentStatus.Captured:
                payment.MarkCaptured(gatewayTransactionId);
                await AddPaymentSucceededEventAsync(payment, cancellationToken);
                break;
            case PaymentStatus.Failed:
                payment.MarkFailed(failureReason ?? "Payment failed by gateway webhook.");
                break;
            case PaymentStatus.Refunded:
                payment.MarkRefunded(gatewayTransactionId);
                break;
            case PaymentStatus.Pending:
            default:
                throw new PaymentProcessingException($"Webhook status '{status}' cannot be applied.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return payment.ToDto();
    }

    private async Task AddPaymentSucceededEventAsync(Payment payment, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payment.GatewayPaymentId))
        {
            throw new PaymentProcessingException("Gateway payment id is required for succeeded payment event.");
        }

        await _integrationEventOutbox.AddAsync(
            new PaymentSucceededEvent(
                payment.Id,
                payment.CustomerId,
                payment.Amount.Amount,
                payment.Amount.Currency,
                payment.Gateway,
                payment.GatewayPaymentId,
                payment.UpdatedAtUtc),
            cancellationToken);
    }

    private async Task<TResponse> ExecuteIdempotentAsync<TResponse>(
        string operationName,
        string requestKey,
        Func<Task<TResponse>> action,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildIdempotencyCacheKey(operationName, requestKey);
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedResult = JsonSerializer.Deserialize<IdempotencyCacheEntry<TResponse>>(cached, JsonOptions);
            if (cachedResult is { State: IdempotencyState.Completed, Response: not null })
            {
                return cachedResult.Response;
            }

            throw new PaymentProcessingException(
                "A request with the same idempotency key is already being processed.");
        }

        await StoreIdempotencyEntryAsync(
            cacheKey,
            IdempotencyCacheEntry<TResponse>.Processing(),
            ProcessingCacheOptions,
            cancellationToken);

        try
        {
            var response = await action();

            await StoreIdempotencyEntryAsync(
                cacheKey,
                IdempotencyCacheEntry<TResponse>.Completed(response),
                CompletedCacheOptions,
                cancellationToken);

            return response;
        }
        catch
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            throw;
        }
    }

    private static async Task ValidateAsync<TRequest>(
        IValidator<TRequest> validator,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid)
        {
            return;
        }

        var errors = validationResult.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        throw new ApplicationValidationException(errors);
    }

    private static void EnsureRefundCanBeProcessed(Payment payment, Money refundAmount, string gateway)
    {
        if (payment.Status != PaymentStatus.Captured)
        {
            throw new PaymentProcessingException("Only captured payments can be refunded.");
        }

        if (!string.Equals(payment.Gateway, gateway, StringComparison.OrdinalIgnoreCase))
        {
            throw new PaymentProcessingException("Refund gateway must match the original payment gateway.");
        }

        if (payment.Amount.Currency != refundAmount.Currency ||
            payment.Amount.Amount != refundAmount.Amount)
        {
            throw new PaymentProcessingException("Only full refunds are supported by the current domain model.");
        }
    }

    private static string BuildIdempotencyCacheKey(string operationName, string requestKey) =>
        $"payments:idempotency:{operationName}:{requestKey.Trim()}";

    private async Task StoreIdempotencyEntryAsync<TResponse>(
        string cacheKey,
        IdempotencyCacheEntry<TResponse> entry,
        DistributedCacheEntryOptions options,
        CancellationToken cancellationToken)
    {
        var serialized = JsonSerializer.Serialize(entry, JsonOptions);
        await _cache.SetStringAsync(cacheKey, serialized, options, cancellationToken);
    }

    private sealed record IdempotencyCacheEntry<TResponse>(
        IdempotencyState State,
        TResponse? Response)
    {
        public static IdempotencyCacheEntry<TResponse> Processing() =>
            new(IdempotencyState.Processing, default);

        public static IdempotencyCacheEntry<TResponse> Completed(TResponse response) =>
            new(IdempotencyState.Completed, response);
    }

    private enum IdempotencyState
    {
        Processing,
        Completed
    }
}
