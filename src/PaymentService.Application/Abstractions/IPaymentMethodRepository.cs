using PaymentService.Domain.Entities;

namespace PaymentService.Application.Abstractions;

public interface IPaymentMethodRepository
{
    Task<PaymentMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
