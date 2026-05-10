namespace PaymentService.Application.Exceptions;

public sealed class PaymentProcessingException : Exception
{
    public PaymentProcessingException(string message)
        : base(message)
    {
    }
}
