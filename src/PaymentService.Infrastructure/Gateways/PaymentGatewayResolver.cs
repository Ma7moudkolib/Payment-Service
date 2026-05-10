using PaymentService.Application.Abstractions;
using PaymentService.Domain.Interfaces;

namespace PaymentService.Infrastructure.Gateways;

public sealed class PaymentGatewayResolver : IPaymentGatewayResolver
{
    private readonly IReadOnlyDictionary<string, IPaymentGateway> _gateways;

    public PaymentGatewayResolver(IEnumerable<IPaymentGateway> gateways)
    {
        _gateways = gateways.ToDictionary(
            gateway => gateway.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    public IPaymentGateway Resolve(string gateway)
    {
        if (_gateways.TryGetValue(gateway, out var paymentGateway))
        {
            return paymentGateway;
        }

        throw new InvalidOperationException($"Payment gateway '{gateway}' is not registered.");
    }
}
