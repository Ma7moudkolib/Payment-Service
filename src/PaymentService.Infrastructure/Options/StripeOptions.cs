namespace PaymentService.Infrastructure.Options;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string ApiKey { get; set; } = string.Empty;
}
