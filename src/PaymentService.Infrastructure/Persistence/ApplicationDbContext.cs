using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Abstractions;
using PaymentService.Domain.Entities;
using PaymentService.Infrastructure.Persistence.Outbox;

namespace PaymentService.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
