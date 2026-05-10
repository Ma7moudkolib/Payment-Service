namespace PaymentService.Infrastructure.Options;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int BatchSize { get; set; } = 20;

    public int PollingIntervalSeconds { get; set; } = 5;
}
