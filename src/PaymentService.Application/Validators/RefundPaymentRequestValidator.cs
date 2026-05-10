using FluentValidation;
using PaymentService.Application.DTOs;

namespace PaymentService.Application.Validators;

public sealed class RefundPaymentRequestValidator : AbstractValidator<RefundPaymentRequest>
{
    public RefundPaymentRequestValidator()
    {
        RuleFor(request => request.PaymentId)
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
