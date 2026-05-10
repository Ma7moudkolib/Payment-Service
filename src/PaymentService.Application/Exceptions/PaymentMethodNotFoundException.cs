namespace PaymentService.Application.Exceptions;

public sealed class PaymentMethodNotFoundException : Exception
{
    public PaymentMethodNotFoundException(Guid paymentMethodId)
        : base($"Payment method '{paymentMethodId}' was not found.")
    {
        PaymentMethodId = paymentMethodId;
    }

    public Guid PaymentMethodId { get; }
}
