using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Persistence.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(transaction => transaction.PaymentId)
            .HasColumnName("payment_id")
            .IsRequired();

        builder.Property(transaction => transaction.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(25)
            .IsRequired();

        builder.OwnsOne(transaction => transaction.Amount, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(value => value.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(transaction => transaction.Gateway)
            .HasColumnName("gateway")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(transaction => transaction.GatewayTransactionId)
            .HasColumnName("gateway_transaction_id")
            .HasMaxLength(200);

        builder.Property(transaction => transaction.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(1000);

        builder.Property(transaction => transaction.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.HasIndex(transaction => transaction.PaymentId);
        builder.HasIndex(transaction => transaction.GatewayTransactionId);
    }
}
