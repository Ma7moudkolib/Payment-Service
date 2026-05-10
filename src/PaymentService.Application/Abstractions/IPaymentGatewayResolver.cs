using PaymentService.Domain.Interfaces;

namespace PaymentService.Application.Abstractions;

public interface IPaymentGatewayResolver
{
    IPaymentGateway Resolve(string gateway);
}
