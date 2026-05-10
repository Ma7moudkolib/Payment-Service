using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Abstractions;
using PaymentService.Application.DTOs;
using PaymentService.Application.Validators;

namespace PaymentService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPaymentService, Services.PaymentService>();
        services.AddScoped<IValidator<ProcessPaymentRequest>, ProcessPaymentRequestValidator>();
        services.AddScoped<IValidator<RefundPaymentRequest>, RefundPaymentRequestValidator>();

        return services;
    }
}
