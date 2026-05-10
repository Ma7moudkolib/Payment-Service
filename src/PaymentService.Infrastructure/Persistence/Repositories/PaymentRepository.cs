using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Abstractions;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PaymentRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _dbContext.Payments
            .Include(payment => payment.Transactions)
            .FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default) =>
        await _dbContext.Payments.AddAsync(payment, cancellationToken);
}
