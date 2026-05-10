namespace PaymentService.Infrastructure.Messaging;

public sealed record OutboxMessageEnvelope(
    Guid MessageId,
    string MessageType,
    string Payload,
    DateTimeOffset OccurredAtUtc);
