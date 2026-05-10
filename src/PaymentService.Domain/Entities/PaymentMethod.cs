namespace PaymentService.Domain.Entities;

public sealed class PaymentMethod
{
    private PaymentMethod()
    {
    }

    public PaymentMethod(
        Guid customerId,
        string gateway,
        string gatewayToken,
        string? displayName = null,
        string? lastFourDigits = null,
        DateTimeOffset? expiresAtUtc = null)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer id is required.", nameof(customerId));
        }

        if (string.IsNullOrWhiteSpace(gateway))
        {
            throw new ArgumentException("Gateway is required.", nameof(gateway));
        }

        if (string.IsNullOrWhiteSpace(gatewayToken))
        {
            throw new ArgumentException("Gateway token is required.", nameof(gatewayToken));
        }

        Id = Guid.NewGuid();
        CustomerId = customerId;
        Gateway = gateway.Trim();
        GatewayToken = gatewayToken.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        LastFourDigits = string.IsNullOrWhiteSpace(lastFourDigits) ? null : lastFourDigits.Trim();
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Gateway { get; private set; } = string.Empty;

    public string GatewayToken { get; private set; } = string.Empty;

    public string? DisplayName { get; private set; }

    public string? LastFourDigits { get; private set; }

    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public bool IsExpired(DateTimeOffset atUtc) =>
        ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= atUtc;
}
