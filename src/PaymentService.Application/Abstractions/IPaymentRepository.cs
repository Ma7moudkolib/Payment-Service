using PaymentService.Domain.Entities;

namespace PaymentService.Application.Abstractions;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
