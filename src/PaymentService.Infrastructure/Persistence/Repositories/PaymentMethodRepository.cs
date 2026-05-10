using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Abstractions;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Persistence.Repositories;

public sealed class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PaymentMethodRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaymentMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _dbContext.PaymentMethods
            .FirstOrDefaultAsync(paymentMethod => paymentMethod.Id == id, cancellationToken);
}
