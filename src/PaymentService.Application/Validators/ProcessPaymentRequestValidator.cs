using FluentValidation;
using PaymentService.Application.DTOs;

namespace PaymentService.Application.Validators;

public sealed class ProcessPaymentRequestValidator : AbstractValidator<ProcessPaymentRequest>
{
    public ProcessPaymentRequestValidator()
    {
        RuleFor(request => request.CustomerId)
            .NotEmpty();

        RuleFor(request => request.PaymentMethodId)
            .NotEmpty();

        RuleFor(request => request.Amount)
            .GreaterThan(0);

        RuleFor(request => request.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$");

        RuleFor(request => request.Gateway)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(request => request.RequestKey)
            .NotEmpty()
            .MaximumLength(128);
    }
}
