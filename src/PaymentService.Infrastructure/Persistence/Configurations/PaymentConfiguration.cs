using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(payment => payment.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(payment => payment.PaymentMethodId)
            .HasColumnName("payment_method_id");

        builder.OwnsOne(payment => payment.Amount, money =>
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

        builder.Property(payment => payment.Gateway)
            .HasColumnName("gateway")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(payment => payment.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(25)
            .HasDefaultValue(PaymentStatus.Pending)
            .IsRequired();

        builder.Property(payment => payment.GatewayPaymentId)
            .HasColumnName("gateway_payment_id")
            .HasMaxLength(200);

        builder.Property(payment => payment.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(1000);

        builder.Property(payment => payment.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(payment => payment.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasMany(payment => payment.Transactions)
            .WithOne()
            .HasForeignKey(transaction => transaction.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(payment => payment.Transactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(payment => payment.CustomerId);
        builder.HasIndex(payment => payment.GatewayPaymentId);
    }
}
