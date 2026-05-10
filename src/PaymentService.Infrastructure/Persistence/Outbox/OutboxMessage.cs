namespace PaymentService.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public OutboxMessage(string type, string payload)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Message type is required.", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Message payload is required.", nameof(payload));
        }

        Id = Guid.NewGuid();
        Type = type.Trim();
        Payload = payload;
        OccurredAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    public bool IsProcessed => ProcessedAtUtc.HasValue;

    public void MarkProcessed(DateTimeOffset processedAtUtc)
    {
        ProcessedAtUtc = processedAtUtc;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        LastError = string.IsNullOrWhiteSpace(error) ? "Unknown publishing error." : error.Trim();
    }
}
