using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Persistence.Configurations;

public sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("payment_methods");

        builder.HasKey(paymentMethod => paymentMethod.Id);

        builder.Property(paymentMethod => paymentMethod.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(paymentMethod => paymentMethod.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(paymentMethod => paymentMethod.Gateway)
            .HasColumnName("gateway")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(paymentMethod => paymentMethod.GatewayToken)
            .HasColumnName("gateway_token")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(paymentMethod => paymentMethod.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(150);

        builder.Property(paymentMethod => paymentMethod.LastFourDigits)
            .HasColumnName("last_four_digits")
            .HasMaxLength(4);

        builder.Property(paymentMethod => paymentMethod.ExpiresAtUtc)
            .HasColumnName("expires_at_utc");

        builder.Property(paymentMethod => paymentMethod.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(paymentMethod => new
        {
            paymentMethod.CustomerId,
            paymentMethod.Gateway
        });
    }
}
