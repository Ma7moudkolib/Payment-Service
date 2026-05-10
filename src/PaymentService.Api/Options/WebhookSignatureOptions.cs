namespace PaymentService.Api.Options;

public sealed class WebhookSignatureOptions
{
    public const string SectionName = "WebhookSignatures";

    public string StripeSecret { get; set; } = string.Empty;

    public string PayPalSecret { get; set; } = string.Empty;
}
